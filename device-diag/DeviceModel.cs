using System;
using CsvHelper.Configuration.Attributes;

namespace Lup.Switch
{
    public class DeviceModel
    {
        [Name("Name")]
        public String Name { get; set; }
        
        [Name("Min OS")]
        public String MinOSVersion { get; set; }
    }
}