using System;
using System.Linq;
using Twilio;
using Twilio.Rest.Supersim.V1;

namespace Lup.Switch
{
    class Program
    {
        private const String ConfigurationFile = "config.json";
        
        static void Main(string[] args)
        {
            //
            // The following deactivates all SuperSIMs!
            //
            
            
            WriteConsoleStatus("Reading configuration... ");
            var config = Configuration.Read(ConfigurationFile);
            WriteConsoleSuccess("success.\n");
                
            WriteConsoleStatus("Authenticating with Twilio... ");
            TwilioClient.Init(config.TwilioAccountSid, config.TwilioAuthToken);
            WriteConsoleSuccess("success.\n");
            
            WriteConsoleStatus("Loading SuperSIMs... ");
            var sims = SimResource.Read().ToList(); // Without ToList() it doesn't return all results
            WriteConsoleSuccess($"{sims.Count().ToString()} retrieved.\n");

            WriteConsoleStatus("Deactivating SIMs... ");
            var activeSims = sims.Where(a => a.Status == SimResource.StatusEnum.Active);
            foreach (var sim in activeSims)
            {
                WriteConsoleStatus($"  {sim.UniqueName}... ");
                 var sim2 = SimResource.Update(
                    status: SimResource.StatusUpdateEnum.Inactive,
                    pathSid: sim.Sid
                );
                 WriteConsoleSuccess("done.\n");
            }
            WriteConsoleSuccess($"{activeSims.Count()} done.\n");
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