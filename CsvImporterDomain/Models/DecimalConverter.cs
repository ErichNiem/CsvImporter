using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace CsvImporterDomain.Models
{
    public class DecimalConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0m;
            }

            // Remove any surrounding quotes and trim whitespace
            text = text.Trim().Trim('"', '\'').Trim();

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            var rawRow = row.Parser.RawRecord;
            var headerName = row.HeaderRecord?[row.CurrentIndex] ?? "unknown";
            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Could not convert '{text}' to decimal. Row: {rawRow}, Header: {headerName}");
        }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
                return null;

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}