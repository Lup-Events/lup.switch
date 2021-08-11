using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lup.TwilioSwitch.Meraki;
using Twilio;
using Twilio.Rest.Supersim.V1;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;
using AuthenticationException = Twilio.Exceptions.AuthenticationException;

namespace Lup.TwilioSwitch
{
    class Program
    {
        private const String ConfigurationFile = "config.json";
        private const Int32 GbelowCNote = 196; // Hz
        private const Int32 FNote = 349; // Hz
        private const Int32 FsharpNote = 370; // Hz
        private const Int32 NoteDuration = 500; // ms

        static void Main(string[] args)
        {
            // Load configuration
            Configuration configuration;
            try
            {
                configuration = Configuration.Read(ConfigurationFile);
            }
            catch (FileNotFoundException)
            {
                configuration = new Configuration();
            }

            // Start Twilio
            while (true)
            {
                try
                {
                    Status("Authenticating with Twilio... ");
                    TwilioClient.Init(configuration.TwilioAccountSid, configuration.TwilioAuthToken);
                    Success("success.\n");
                    break;
                }
                catch (AuthenticationException ex)
                {
                    Error($"failed. {ex.Message}.\n");
                    Input("What is your Twilio Account SID?\n");
                    configuration.TwilioAccountSid = Console.ReadLine();
                    if (String.IsNullOrEmpty(configuration.TwilioAccountSid))
                    {
                        return;
                    }

                    Input("What is your Twilio Auth Token?\n");
                    configuration.TwilioAuthToken = Console.ReadLine();
                    if (String.IsNullOrEmpty(configuration.TwilioAuthToken))
                    {
                        return;
                    }

                    configuration.Write(ConfigurationFile);
                }
            }

            // Start Meraki
            MerakiClient meraki;
            Organization org;
            Network net;
            while (true)
            {
                try
                {
                    Status("Authenticating with Meraki... ");
                    if (null == configuration.MerakiApiKey)
                    {
                        throw new Meraki.AuthenticationException();
                    }

                    meraki = new MerakiClient(configuration.MerakiApiKey);

                    var orgs = meraki.RequestCollection<Organization>("organizations");
                    org = orgs.First();

                    var nets = meraki.RequestCollection<Network>($"organizations/{org.id}/networks");
                    net = nets.First();

                    Success($"{org.name} > {net.name}\n");
                    break;
                }
                catch (Meraki.AuthenticationException ex)
                {
                    Error($"failed. {ex.Message}.\n");
                    Input("What is your Meraki API Key?\n");
                    configuration.MerakiApiKey = Console.ReadLine();
                    if (String.IsNullOrEmpty(configuration.MerakiApiKey))
                    {
                        return;
                    }

                    configuration.Write(ConfigurationFile);
                }
            }

            // Load list of all SIMs
            Status("Retrieving Twilio SIMs... ");
            var sims = SimResource.Read().ToList(); // Without ToList() it doesn't return all results
            Success($"{sims.Count().ToString()} retrieved.\n");

            // Start camera
            using var frame = new Mat();
            VideoCapture camera;
            while (true)
            {
                Status("Activating camera... ");
                camera = new VideoCapture(configuration.CameraIndex);
                if (camera.IsOpened() && camera.Read(frame))
                {
                    Success("success.\n");
                    break;
                }

                camera.Dispose();
                Warning("camera not selected or not working.\n");

                Input("What camera index would you like to use?\n");
                var v = Console.ReadLine();
                if (String.IsNullOrEmpty(v) || !Int32.TryParse(v, out var cameraIndex))
                {
                    return;
                }

                configuration.CameraIndex = cameraIndex;
                configuration.Write(ConfigurationFile);
            }

            var mode = ModeType.Activate;
            var lastCodes = new List<string>();
            var reader = new BarcodeReader();
            Status("Opening working window... ");
            using var window = new Window("Twilio Switch");
            Success("done.\n");
            while (true)
            {
                // Handle any key presses
                var key = Cv2.WaitKey(1000 / 10);
                switch (key)
                {
                    case 97: // 'a'
                    case 65: // 'A'
                        mode = ModeType.Activate;
                        lastCodes.Clear(); // Reset debounce
                        Status("Now in activation mode.\n");
                        break;
                    case 100: // 'd'
                    case 68: // 'D'
                        mode = ModeType.Deactivate;
                        lastCodes.Clear(); // Reset debounce
                        Status("Now in deactivation mode.\n");
                        break;
                    case 120: // 'x'
                    case 88: // 'X'
                        throw new NotImplementedException();
                        break;
                    case 113: // 'q'
                    case 81: // 'Q'
                        return;
                    case -1: // No key pressed
                        break;
                    default:
                        Status($"Unexpected key '{key.ToString()}' pressed.\n");
                        break;
                }

                // Read frame and shortcut loop if no frame read
                if (!camera.Read(frame) || frame.Empty())
                {
                    break;
                }

                // Overlay frame
                switch (mode)
                {
                    case ModeType.Activate:
                        frame.PutText("Scan barcode to activate SIM.", new Point(10, 40), HersheyFonts.HersheyPlain, 2, Scalar.Green, 2, LineTypes.Link8, false);
                        break;
                    case ModeType.Deactivate:
                        frame.PutText("Scan barcode to deactivate SIM.", new Point(10, 40), HersheyFonts.HersheyPlain, 2, Scalar.Red, 2, LineTypes.Link8, false);
                        break;
                }

                frame.PutText("Press 'A' to activate scanned SIMs, 'D' to deactivate scanned SIMs, 'E' to enroll a device, 'X' to deactivate all SIMs or 'Q' to quit.", new Point(10, frame.Height - 10), HersheyFonts.HersheyPlain, 1, Scalar.Blue, 1, LineTypes.Link8, false);

                // Draw frame
                window.ShowImage(frame);

                // Read barcodes
                using var b = BitmapConverter.ToBitmap(frame);
                var results = reader.DecodeMultiple(b);
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        // Debounce
                        if (lastCodes.Contains(result.Text))
                        {
                            continue;
                        }

                        Status($"Considering '{result.Text}'... ");
                        // Attempt to lookup SIMs
                        SimResource sim;
                        try
                        {
                            sim = sims.SingleOrDefault(a => String.Compare(a.UniqueName, result.Text, true) == 0);
                        }
                        catch (InvalidOperationException)
                        {
                            Warning($"ambiguous.\n");
                            continue;
                        }

                        if (null == sim)
                        {
                            Warning($"not found.\n");
                            continue;
                        }

                        // Apply action
                        switch (mode)
                        {
                            case ModeType.Activate:
                                SimActivate(sim.Sid);
                                Success($"activated.\n");
                                break;
                            case ModeType.Deactivate:
                                SimDeactivate(sim.Sid);
                                Success($"deactivated.\n");
                                break;
                        }
                    }

                    // Update debounce list
                    lastCodes = results.Select(a => a.Text).ToList();
                }
            }

            Success("Done.\n");
        }

        private static void SimDeactivate(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Inactive, null, null, null, null);
        }

        private static void SimActivate(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Active, null, null, null, null);
        }


        public static void Status(String message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.DarkRed;
        }

        public static void Input(String message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
        }

        public static void Record(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write(message);
        }

        public static void Error(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.Write(message);
        }

        public static void Warning(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(message);
        }

        public static void Success(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.Write(message);
        }
    }
}