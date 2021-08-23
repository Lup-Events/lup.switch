using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace Lup.Switch
{
    public static class CsvLoader
    {
        public static ICollection<T> LoadAll<T>(String fileName)
        {
            if (null == fileName)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            
            using (var reader = new StreamReader(fileName))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<T>().ToList();
            }
        }
    }
}