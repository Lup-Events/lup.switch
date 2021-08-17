using System;
using System.IO;
using System.Linq;
using Twilio;
using Twilio.Rest.Wireless.V1;

namespace scratch
{
    class Program
    {
        static void Main(string[] args)
        {
            TwilioClient.Init("SKcd901bcca79134d68513b72b5d22ace6", "n6Ay9YMlU4FORMQD3rigZONuR6DEEuU9");
            
            var sims = SimResource.Read().ToList();
            var file = File.OpenWrite("twilio.csv");
            var writer = new StreamWriter(file);
            foreach (var sim in sims)
            {
                writer.WriteLine($"{sim.Iccid},{sim.UniqueName}");
            }
            writer.Flush();
            file.Flush();
            
            Console.WriteLine("Hello World!");
        }
    }
}