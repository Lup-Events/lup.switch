using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Supersim.V1;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZXing;

namespace Lup.TwilioSwitch
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load configuration
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.local.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables();

            var configRoot = builder.Build();
            var configuration = configRoot.Get<Configuration>();
            
            // Start Twilio
            TwilioClient.Init(configuration.TwilioAccountSid, configuration.TwilioAuthToken);


            var reader = new BarcodeReader();
            using var capture = new VideoCapture(0);
            using var image = new Mat();
            using var window = new Window("Twilio Switch");
            while (true)
            {
                if (Cv2.WaitKey(1) > 0)
                {
                    break;
                }
                
                capture.Read(image);
                if (image.Empty())
                {
                    break;
                }

                window.ShowImage(image);

                using var b = BitmapConverter.ToBitmap(image);
                var result = reader.Decode(b);
                if (result != null)
                {
                    Console.WriteLine(result);
                }
            }


            /*
            
            var sims = SimResource.Read();
            foreach (var sim in sims)
            {
                Console.WriteLine($"{sim.UniqueName} {sim.Status}");
                
            }
*/
            Console.WriteLine("Done");
        }

        private static void DeactivateSim(String uniqueName)
        {
            
        }

        private static SimResource GetSimByUniqueName(String uniqueName)
        {
            if (null == uniqueName)
            {
                throw new ArgumentNullException(nameof(uniqueName));
            }
            
            var sims = SimResource.Read();
            return sims.SingleOrDefault(a => a.UniqueName == uniqueName);
        }
    }
}