using System;
using System.IO;
using System.Text.Json;

namespace Lup.Switch
{
    public class Configuration
    {
        public String TwilioAccountSid { get; set; }
        public String TwilioAuthToken { get; set; }
        public String MerakiApiKey { get; set; }
        public String MerakiNetworkId { get; set; }

        public void Write(string file)
        {
            if (null == file)
            {
                throw new ArgumentNullException();
            }

            var raw = JsonSerializer.Serialize(this);
            File.WriteAllText(file, raw);
        }

        public static Configuration Read(string file)
        {
            var raw = File.ReadAllText(file);
            return JsonSerializer.Deserialize<Configuration>(raw);
        }
    }
}