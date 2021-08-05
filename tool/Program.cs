using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Twilio;
using Twilio.Rest.Supersim.V1;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Twilio.Exceptions;
using ZXing;

namespace Lup.TwilioSwitch
{
    class Program
    {
        private const String ConfigurationFile = "config.json";
        private const Int32 GbelowCNote = 196; // Hz
        private const Int32 FNote      = 349; // Hz
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
                    TwilioClient.Init(configuration.TwilioAccountSid, configuration.TwilioAuthToken);
                    Console.WriteLine("Authenticated with Twilio.");
                    break;
                }
                catch (AuthenticationException ex)
                {
                    Console.WriteLine($"Twilio authentication failed. {ex.Message}.");
                    Console.WriteLine("What is your Twilio Account SID?");
                    configuration.TwilioAccountSid = Console.ReadLine();
                    if (String.IsNullOrEmpty(configuration.TwilioAccountSid))
                    {
                        return;
                    }

                    Console.WriteLine("What is your Twilio Auth Token?");
                    configuration.TwilioAuthToken = Console.ReadLine();
                    if (String.IsNullOrEmpty(configuration.TwilioAuthToken))
                    {
                        return;
                    }

                    configuration.Write(ConfigurationFile);
                }
            }

            // Start camera
            using var frame = new Mat();
            VideoCapture camera;
            while (true)
            {
                camera = new VideoCapture(configuration.CameraIndex);
                if (camera.IsOpened() && camera.Read(frame))
                {
                    Console.WriteLine("Camera activated.");
                    break;
                }

                camera.Dispose();

                Console.WriteLine("Camera not selected or not working. What camera index would you like to use?");
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
            using var window = new Window("Twilio Switch");
            while (true)
            {
                // Handle any key presses
                var key = Cv2.WaitKey(1000 / 10);
                switch (key)
                {
                    case 97: // 'a'
                    case 65: // 'A'
                        mode = ModeType.Activate;
                        break;
                    case 100: // 'd'
                    case 68: // 'D'
                        mode = ModeType.Deactivate;
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
                        Console.WriteLine($"Unexpected key '{key.ToString()}' pressed.");
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

                frame.PutText("Press 'A' to activate scanned SIMs, 'D' to deactivate scanned SIMs, 'X' to deactivate all SIMs or 'Q' to quit.", new Point(10, frame.Height - 10), HersheyFonts.HersheyPlain, 1, Scalar.Blue, 1, LineTypes.Link8, false);

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

                        // Attempt to lookup SIMs
                        SimResource sim;
                        try
                        {
                            sim = GetSimByUniqueName(result.Text);
                        }
                        catch (InvalidOperationException)
                        {
                            Console.Beep(GbelowCNote,NoteDuration);
                            continue;
                        }

                        // Apply action
                        switch (mode)
                        {
                            case ModeType.Activate:
                                SimActivate(sim.Sid);
                                
                                Console.Beep(FsharpNote, NoteDuration);
                                Console.WriteLine($"{sim.UniqueName} activated.");
                                break;
                            case ModeType.Deactivate:
                                SimDeactivate(sim.Sid);
                                Console.Beep(FNote, NoteDuration);
                                Console.WriteLine($"{sim.UniqueName} deactivated.");
                                break;
                        }
                    }

                    // Update debounce list
                    lastCodes = results.Select(a => a.Text).ToList();
                }
            }


            /*
            
            var sims = SimResource.Read();
            foreach (var sim in sims)
            {
                Console.WriteLine($"{sim.UniqueName} {sim.Status}");
                
            }
            
                 SoundPlayer player = new SoundPlayer();
            player.SoundLocation =Application.StartupPath+ "\\Sound\\5756.wav";
            player.Load ();
*/
            Console.WriteLine("Done");
        }

        private static void SimDeactivate(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Inactive, null, null, null, null);
        }

        private static void SimActivate(String sid)
        {
            SimResource.Update(sid, null, SimResource.StatusUpdateEnum.Active, null, null, null, null);
        }

        private static SimResource GetSimByUniqueName(String uniqueName)
        {
            if (null == uniqueName)
            {
                throw new ArgumentNullException(nameof(uniqueName));
            }

            var sims = SimResource.Read();
            return sims.Single(a => a.UniqueName == uniqueName);
        }
    }
}