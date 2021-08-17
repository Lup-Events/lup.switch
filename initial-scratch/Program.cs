using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lup.TwilioSwitch.Meraki;
using Twilio;
using Twilio.Rest.Supersim.V1;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;

namespace Lup.TwilioSwitch
{
    class Program
    {
        private const String ConfigurationFile = "config.json";

        private static Configuration Config;
        private static MerakiClient Meraki;
        private static String MerakiNetworkId;
        private static ICollection<SimResource> TwilioSims;
        private static VideoCapture Camera;

        static void Main(string[] args)
        {
            // Load configuration
            LoadConfiguration();

            // Start Twilio
            if (StartTwilio()) return;

            // Start Meraki
            if (StartMeraki()) return;

            // Load list of all SIMs
            FetchTwilioSims();

            // Start camera
            if (StartCamera()) return;

            var barcodeReader = new BarcodeReader();
            var mode = ModeType.Activate;
            var lastBarcodes = new List<String>();
            var status = "Looking for barcode...";
            var frame = new Mat();
            String enrollSim = null;
            String enrollDevice = null;

            WriteConsoleStatus("Opening working window... ");
            using var window = new Window("Twilio Switch");
            WriteConsoleSuccess("done.\n");
            while (true)
            {
                // Handle any key presses
                var key = Cv2.WaitKey(1000 / 10);
                switch (key)
                {
                    // Activate
                    case 97: // 'a'
                    case 65: // 'A'
                        mode = ModeType.Activate;
                        lastBarcodes.Clear(); // Reset debounce
                        WriteConsoleStatus("Now in activation mode.\n");
                        break;

                    // Deactivate
                    case 100: // 'd'
                    case 68: // 'D'
                        mode = ModeType.Deactivate;
                        lastBarcodes.Clear(); // Reset debounce
                        WriteConsoleStatus("Now in deactivation mode.\n");
                        break;

                    // Enroll
                    case 101: // 'e'
                    case 69: // 'E'
                        mode = ModeType.Enroll;
                        WriteConsoleStatus("Now in enroll mode.\n");
                        break;

                    // Deactivate all
                    case 120: // 'x'
                    case 88: // 'X'
                        mode = ModeType.DeactivateAll;
                        WriteConsoleStatus("Now in deactivate-all mode.\n");
                        status = "WARNING this will cause all SIMs to cease working immediately. Press ENTER to continue.";
                        break;
                    case 13: // ENTER
                        if (mode == ModeType.DeactivateAll)
                        {
                            status = "All SIMs deactivated.";
                            // TODO
                        }

                        break;

                    // Quit
                    case 113: // 'q'
                    case 81: // 'Q'
                        return;

                    case -1: // No key pressed
                        break;
                    default:
                        WriteConsoleStatus($"Unexpected key '{key.ToString()}' pressed.\n");
                        break;
                }

                // Read frame and shortcut loop if no frame read
                if (!Camera.Read(frame) || frame.Empty())
                {
                    break;
                }

                // Overlay frame
                switch (mode)
                {
                    case ModeType.Activate:
                        frame.PutTextShadow("Activate SIM.", Scalar.DarkGreen, 10, 40, 2);
                        break;
                    case ModeType.Deactivate:
                        frame.PutTextShadow("Deactivate SIM.", Scalar.DarkRed, 10, 40, 2);
                        break;
                    case ModeType.Enroll:
                        frame.PutTextShadow("Enroll device with SIM.", Scalar.Chocolate, 10, 40, 2);
                        break;
                    case ModeType.DeactivateAll:
                        frame.PutTextShadow("Deactivate all.", Scalar.Red, 10, 40, 2);
                        break;
                }

                frame.PutTextShadow(status, Scalar.Blue, 10, 80, 1);
                frame.PutTextShadow("Press 'A' to activate SIMs, 'D' to deactivate SIMs, 'E' to enroll a device, 'X' to deactivate all SIMs or 'Q' to quit.", Scalar.Black, 10, frame.Height - 10, 1);

                // Draw frame
                window.ShowImage(frame);

                // Read barcodes
                using var b = BitmapConverter.ToBitmap(frame);
                var barcodes = barcodeReader.DecodeMultiple(b)?.Select(a => a.Text).ToList();
                if (barcodes == null) // If no barcodes detected
                {
                    continue;
                }

                // Debounce
                if (lastBarcodes.Any(a => barcodes.Contains(a)))
                {
                    continue;
                }

                var sb = new StringBuilder();

                // Iterate through each barcode
                foreach (var barcode in barcodes)
                {
                    SimResource sim = null;
                    if (mode == ModeType.Activate || mode == ModeType.Deactivate)
                    {
                        WriteConsoleStatus($"Considering '{barcode}'... ");
                        // Attempt to lookup SIMs
                        try
                        {
                            sim = TwilioSims.SingleOrDefault(a => String.Compare(a.UniqueName, barcode, true) == 0);
                        }
                        catch (InvalidOperationException)
                        {
                            WriteConsoleWarning($"ambiguous.\n");
                            continue;
                        }

                        if (null == sim)
                        {
                            WriteConsoleWarning($"not found.\n");
                            continue;
                        }
                    }

                    // Apply action
                    switch (mode)
                    {
                        case ModeType.Activate:
                            ActivateTwilioSim(sim.Sid);
                            WriteConsoleSuccess($"activated.\n");
                            sb.Append($"{barcode} activated. ");

                            Console.Beep();
                            Thread.Sleep(500);
                            break;
                        case ModeType.Deactivate:
                            DeactivateTwilioSim(sim.Sid);
                            WriteConsoleSuccess($"deactivated.\n");
                            sb.Append($"{barcode} deactivated. ");

                            Console.Beep();
                            Thread.Sleep(200);
                            Console.Beep();
                            Thread.Sleep(500);
                            break;
                        case ModeType.Enroll:
                            if (barcode.Length == 12) // Apple serial
                            {
                                enrollDevice = barcode;
                            }
                            else if (barcode.Length == 20 && barcode.StartsWith("8988307")) // Twilio SuperSIM
                            {
                                enrollSim = barcode;
                            }
                            else
                            {
                                sb.Append($"Unknown barcode '{barcode}'. ");
                                continue;
                            }

                            if (enrollDevice == null)
                            {
                                sb.Append($"Matched SIM {enrollSim}, looking for device...");
                            }
                            else if (enrollSim == null)
                            {
                                sb.Append($"Matched device {enrollDevice}, looking for SIM...");
                            }
                            else
                            {
                                // TODO
                                sb.Append($"Enrolled {enrollDevice} > {enrollSim}");
                                enrollDevice = null;
                                enrollSim = null;
                            }

                            break;
                    }
                }

                // Update debounce list
                lastBarcodes = barcodes;
                status = sb.ToString();
            }

            WriteConsoleSuccess("Done.\n");
        }

        private static bool StartCamera()
        {
            while (true)
            {
                using var frame = new Mat();
                WriteConsoleStatus("Activating camera... ");
                Camera = new VideoCapture(Config.CameraIndex);
                if (Camera.IsOpened() && Camera.Read(frame))
                {
                    WriteConsoleSuccess("success.\n");
                    break;
                }

                Camera.Dispose();
                WriteConsoleWarning("camera not selected or not working.\n");

                WriteConsoleInput("What camera index would you like to use?\n");
                var v = Console.ReadLine();
                if (String.IsNullOrEmpty(v) || !Int32.TryParse(v, out var cameraIndex))
                {
                    return true;
                }

                Config.CameraIndex = cameraIndex;
                Config.Write(ConfigurationFile);
            }

            return false;
        }

        private static void FetchTwilioSims()
        {
            WriteConsoleStatus("Retrieving Twilio SIMs... ");
            TwilioSims = SimResource.Read().ToList(); // Without ToList() it doesn't return all results
            WriteConsoleSuccess($"{TwilioSims.Count().ToString()} retrieved.\n");
        }

        private static bool StartMeraki()
        {
            while (true)
            {
                try
                {
                    WriteConsoleStatus("Authenticating with Meraki... ");
                    if (null == Config.MerakiApiKey)
                    {
                        throw new Meraki.AuthenticationException();
                    }

                    Meraki = new MerakiClient(Config.MerakiApiKey);

                    var orgs = Meraki.RequestCollection<Organization>("organizations");
                    var org = orgs.First();

                    var nets = Meraki.RequestCollection<Network>($"organizations/{org.id}/networks");
                    var net = nets.First();
                    MerakiNetworkId = net.id;

                    WriteConsoleSuccess($"{org.name} > {net.name}\n");
                    break;
                }
                catch (Meraki.AuthenticationException ex)
                {
                    WriteConsoleError($"failed. {ex.Message}.\n");
                    WriteConsoleInput("What is your Meraki API Key?\n");
                    Config.MerakiApiKey = Console.ReadLine();
                    if (String.IsNullOrEmpty(Config.MerakiApiKey))
                    {
                        return true;
                    }

                    Config.Write(ConfigurationFile);
                }
            }

            return false;
        }

        private static bool StartTwilio()
        {
            while (true)
            {
                try
                {
                    WriteConsoleStatus("Authenticating with Twilio... ");
                    TwilioClient.Init(Config.TwilioAccountSid, Config.TwilioAuthToken);
                    WriteConsoleSuccess("success.\n");
                    break;
                }
                catch (AuthenticationException ex)
                {
                    WriteConsoleError($"failed. {ex.Message}.\n");
                    WriteConsoleInput("What is your Twilio Account SID?\n");
                    Config.TwilioAccountSid = Console.ReadLine();
                    if (String.IsNullOrEmpty(Config.TwilioAccountSid))
                    {
                        return true;
                    }

                    WriteConsoleInput("What is your Twilio Auth Token?\n");
                    Config.TwilioAuthToken = Console.ReadLine();
                    if (String.IsNullOrEmpty(Config.TwilioAuthToken))
                    {
                        return true;
                    }

                    Config.Write(ConfigurationFile);
                }
            }

            return false;
        }

        private static void LoadConfiguration()
        {
            WriteConsoleStatus("Reading configuration... ");
            try
            {
                Config = Configuration.Read(ConfigurationFile);
                WriteConsoleSuccess("success.\n");
            }
            catch (FileNotFoundException)
            {
                Config = new Configuration();
                WriteConsoleWarning("none found.\n");
            }
        }

        private static void DeactivateTwilioSim(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Inactive, null, null, null, null);
        }

        private static void ActivateTwilioSim(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Active, null, null, null, null);
        }

        public static void WriteConsoleStatus(String message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.DarkRed;
        }

        public static void WriteConsoleInput(String message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
        }

        public static void WriteConsoleError(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.Write(message);
        }

        public static void WriteConsoleWarning(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(message);
        }

        public static void WriteConsoleSuccess(String message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.Write(message);
        }
    }
}