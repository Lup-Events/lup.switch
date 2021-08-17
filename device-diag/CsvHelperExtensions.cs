using System;
using CsvHelper;

namespace Lup.Switch
{
    public static class CsvHelperExtensions
    {
        public static void Write(this CsvWriter target, String serial, IssueType issue, String notes)
        {
            target.WriteRecord(new Record()
            {
                Serial = serial,
                Issue = issue,
                Notes = notes,
                Link = $"https://n25.meraki.com/L%C3%BCp/n/KpDVibz/manage/pcc/list#q={serial}"
            });
            target.NextRecord();
        }
    }
}