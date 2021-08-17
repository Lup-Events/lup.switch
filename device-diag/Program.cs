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

            WriteConsoleStatus("Loading device models... ");
            var deviceModels = CsvLoader.LoadAll<DeviceModel>(DeviceModelFile);
            WriteConsoleSuccess($"{deviceModels.Count} retrieved.\n");

            WriteConsoleStatus("Loading carriers... ");
            var carriers = CsvLoader.LoadAll<Carrier>(CarrierFile);
            WriteConsoleSuccess($"{carriers.Count} retrieved.\n");

            WriteConsoleStatus("Preparing output... ");
            using var outputFile = new StreamWriter(OutputFile, false)
            {
                AutoFlush = true
            };
            using var output = new CsvWriter(outputFile, CultureInfo.InvariantCulture);
            WriteConsoleSuccess($"done.\n");

            WriteConsoleStatus("Analysing devices... ");
            foreach (var device in devices)
            {
                // Check if stale, ignore if so
                if (device.LastConnectedAt < DateTime.Now.AddDays(-DeviceStaleTime))
                {
                    output.Write(device.Serial,IssueType.Stale, $"Hasn't been online for {(Int32)(DateTime.Now - device.LastConnectedAt).TotalDays} days.");
                    continue;
                }

                // Check device model
                var deviceModel = deviceModels.LastOrDefault(a => device.Model.StartsWith(a.Name));
                if (null == deviceModel)
                {
                    output.Write(device.Serial,IssueType.UnsupportedModel, $"Model '{device.Model}'");
                }
                else
                {
                    // Check OS version
                    var currentVersion = new Version(device.OSVersion);
                    var minVersion = new Version(deviceModel.MinOSVersion);

                    if (currentVersion < minVersion)
                    {
                        output.Write(device.Serial,IssueType.OutOfDateOperatingSystem,$"Current version {device.OSVersion} < {deviceModel.MinOSVersion} expected for {deviceModel.Name}.");
                    }
                }

                // Check enrolled
                if (!device.IsSupervised || !device.IsManaged)
                {
                    output.Write(device.Serial, IssueType.NotEnrolled, $"Supervised {device.IsSupervised}, managed {device.IsManaged}");
                }
                
                // Check device name
                if (device.Name != device.Serial)
                {
                    output.Write(device.Serial,IssueType.Name, $"Named '{device.Name}'.");
                }

                if (device.Iccid == "-")
                {
                    continue;
                }

                // Check carrier known
                var carrier = carriers.FirstOrDefault(a => device.Iccid.StartsWith(a.IccidPrefix));
                if (null == carrier)
                {
                    output.Write( device.Serial, IssueType.UnknownCarrier,$"ICCID '{device.Iccid}'");
                    continue;
                }
                else if (!carrier.IsApproved) // Check carrier approved
                {
                    output.Write(device.Serial,IssueType.UnapprovedCarrier, $"Unapproved carrier '{carrier.Name}'.");
                    continue;
                }

                // Add profile if missing
                if (!device.Tags.Contains(carrier.ConfigurationProfile))
                {
                    output.Write(device.Serial, IssueType.MissingCarrierProfile, $"Tags '{device.Tags}' does not contain '{carrier.ConfigurationProfile}'.");
                }

                // Check SIM known
                var sim = sims.FirstOrDefault(a => a.Iccid == device.Iccid);
                if (null == sim)
                {
                    output.Write(device.Serial, IssueType.IccidNotFound, null);
                    continue;
                }

                // Check SIM name
                if (sim.UniqueName != device.Serial)
                {
                    output.Write(device.Serial, IssueType.SimBadLabel, $"Currently named '{sim.UniqueName}' instead of device serial.");
                }
            }

            WriteConsoleSuccess($"done.\n");
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