using System;
using CsvHelper.Configuration.Attributes;

namespace Lup.Switch
{
    public class Device
    {
        [Name("Connected")]
        public DateTime LastConnectedAt { get; set; }

        [Name("Full OS")]
        public String OSVersion { get; set; }
        
        [Name("Model")]
        public String Model { get; set; }
        
        [Name("Serial")]
        public String Serial { get; set; }
        
        [Name("Name")]
        public String Name { get; set; }
        
        [Name("Tags")]
        public String Tags { get; set; }
        
        [Name("ICCID")]
        public String Iccid { get; set; }
        
        [Name("Supervised?")]
        [BooleanTrueValues("Yes")]
        [BooleanFalseValues("No")]
        public Boolean IsSupervised { get; set; }
        
        [Name("Managed?")]
        [BooleanTrueValues("Yes")]
        [BooleanFalseValues("No")]
        public Boolean IsManaged { get; set; }
    }
}