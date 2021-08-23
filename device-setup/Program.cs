using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Twilio;
using Twilio.Rest.Supersim.V1;

namespace Lup.Switch
{
    class Program
    {
        private const String ConfigurationFile = "config.json";
        private const String DeviceFile = "device.csv";
        private const String DeviceModelFile = "devicemodel.csv";
        private const String CarrierFile = "carrier.csv";
        private const String OutputFile = "output.csv";

        private const Int32 DeviceStaleTime = 365; // days

        static void Main(string[] args)
        {
            WriteConsoleStatus("Reading configuration... ");
            var config = Configuration.Read(ConfigurationFile);
            WriteConsoleSuccess("success.\n");

            WriteConsoleStatus("Authenticating with Twilio... ");
            TwilioClient.Init(config.TwilioAccountSid, config.TwilioAuthToken);
            WriteConsoleSuccess("success.\n");

            WriteConsoleStatus("Loading SuperSIMs... ");
            var sims = SimResource.Read().ToList(); // Without ToList() it doesn't return all results
            WriteConsoleSuccess($"{sims.Count().ToString()} retrieved.\n");

            WriteConsoleStatus("Loading devices... ");
            var devices = CsvLoader.LoadAll<Device>(DeviceFile);
            WriteConsoleSuccess($"{devices.Count} retrieved.\n");

            WriteConsoleStatus("Analysing devices... \n");
            var countSetup = 0;
            foreach (var device in devices)
            {
                if (!device.Iccid.StartsWith("8988307"))
                {
                    continue;
                }


                // Check SIM known
                var sim = sims.FirstOrDefault(a => a.Iccid == device.Iccid);
                if (null == sim)
                {
                    WriteConsoleStatus($"  {device.Serial} ");
                    WriteConsoleError($"SIM not found.\n");
                    continue;
                }

                // Check SIM name
                if (sim.UniqueName != device.Serial)
                {
                    WriteConsoleStatus($"  {device.Serial} incorrect name, updating... ");
                    SimResource.Update(sim.Sid, device.Serial, SimResource.StatusUpdateEnum.Active, "Devices 2105");
                    countSetup++;
                    WriteConsoleSuccess($"done.\n");
                }
            }

            WriteConsoleSuccess($"{countSetup} setup.\n");
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