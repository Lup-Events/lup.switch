using System;
using CsvHelper.Configuration.Attributes;

namespace Lup.Switch
{
    public class Carrier
    {
        [Name("Carrier")]
        public String Name { get; set; }
        
        [Name("ICCID Prefix")]
        public String IccidPrefix { get; set; }

        [Name("Approved")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public Boolean IsApproved { get; set; }
        
        [Name("Configuration Profile")]
        public String ConfigurationProfile { get; set; }
    }
}