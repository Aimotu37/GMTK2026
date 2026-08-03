using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class CsvParser
{
    public sealed class Row
    {
        private readonly Dictionary<string, string> _values;

        public string SourceName { get; }
        public int LineNumber { get; }

        internal Row(
            string sourceName,
            int lineNumber,
            Dictionary<string, string> values)
        {
            SourceName = sourceName;
            LineNumber = lineNumber;
            _values = values;
        }

        public string GetString(string columnName, bool allowEmpty = false)
        {
            if (!_values.TryGetValue(columnName, out var value))
            {
                throw Error(columnName, "Column does not exist.");
            }

            value = value.Trim();
            if (!allowEmpty && value.Length == 0)
            {
                throw Error(columnName, "Value cannot be empty.");
            }

            return value;
        }

        public int GetInt32(string columnName)
        {
            var value = GetString(columnName);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw Error(columnName, $"'{value}' is not a valid integer.");
        }

        public float GetSingle(string columnName)
        {
            var value = GetString(columnName);
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw Error(columnName, $"'{value}' is not a valid number.");
        }

        public bool GetBoolean(string columnName)
        {
            var value = GetString(columnName);
            if (bool.TryParse(value, out var result))
            {
                return result;
            }

            throw Error(columnName, $"'{value}' is not a valid boolean.");
        }

        public TEnum GetEnum<TEnum>(string columnName) where TEnum : struct, Enum
        {
            var value = GetString(columnName);
            var normalizedValue = NormalizeEnumName(value);

            foreach (var name in Enum.GetNames(typeof(TEnum)))
            {
                if (string.Equals(
                    NormalizeEnumName(name),
                    normalizedValue,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return (TEnum)Enum.Parse(typeof(TEnum), name);
                }
            }

            throw Error(columnName, $"'{value}' is not a valid {typeof(TEnum).Name} value.");
        }

        private FormatException Error(string columnName, string message)
        {
            return new FormatException($"{SourceName}:{LineNumber} [{columnName}] {message}");
        }
    }

    private sealed class Record
    {
        public int LineNumber { get; }
        public List<string> Fields { get; }

        public Record(int lineNumber, List<string> fields)
        {
            LineNumber = lineNumber;
            Fields = fields;
        }
    }

    public static IReadOnlyList<Row> Parse(string csvText, string sourceName = "CSV")
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            throw new FormatException($"{sourceName}: CSV content is empty.");
        }

        var records = ReadRecords(csvText, sourceName);
        if (records.Count == 0)
        {
            throw new FormatException($"{sourceName}: CSV content is empty.");
        }

        var headers = records[0].Fields;
        headers[0] = headers[0].TrimStart('\uFEFF');
        ValidateHeaders(headers, sourceName, records[0].LineNumber);

        var rows = new List<Row>();
        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            var record = records[recordIndex];
            if (IsBlank(record.Fields))
            {
                continue;
            }

            if (record.Fields.Count != headers.Count)
            {
                throw new FormatException(
                    $"{sourceName}:{record.LineNumber} Expected {headers.Count} columns, " +
                    $"but found {record.Fields.Count}.");
            }

            var values = new Dictionary<string, string>(headers.Count, StringComparer.Ordinal);
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                values.Add(headers[columnIndex], record.Fields[columnIndex]);
            }

            rows.Add(new Row(sourceName, record.LineNumber, values));
        }

        return rows.AsReadOnly();
    }

    private static List<Record> ReadRecords(string csvText, string sourceName)
    {
        var records = new List<Record>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var lineNumber = 1;
        var recordLineNumber = 1;

        for (var index = 0; index < csvText.Length; index++)
        {
            var character = csvText[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < csvText.Length && csvText[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes && character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (character == '\r' || character == '\n')
            {
                if (inQuotes)
                {
                    field.Append('\n');
                    if (character == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    lineNumber++;
                    continue;
                }

                fields.Add(field.ToString());
                field.Clear();
                records.Add(new Record(recordLineNumber, fields));
                fields = new List<string>();

                if (character == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                {
                    index++;
                }

                lineNumber++;
                recordLineNumber = lineNumber;
                continue;
            }

            field.Append(character);
        }

        if (inQuotes)
        {
            throw new FormatException($"{sourceName}:{recordLineNumber} Quoted field is not closed.");
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(new Record(recordLineNumber, fields));
        }

        return records;
    }

    private static void ValidateHeaders(List<string> headers, string sourceName, int lineNumber)
    {
        var uniqueHeaders = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < headers.Count; index++)
        {
            headers[index] = headers[index].Trim();
            if (headers[index].Length == 0)
            {
                throw new FormatException($"{sourceName}:{lineNumber} Header cannot be empty.");
            }

            if (!uniqueHeaders.Add(headers[index]))
            {
                throw new FormatException(
                    $"{sourceName}:{lineNumber} Duplicate header '{headers[index]}'.");
            }
        }
    }

    private static bool IsBlank(List<string> fields)
    {
        return fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0]);
    }

    private static string NormalizeEnumName(string value)
    {
        return value.Replace("_", string.Empty).Replace("-", string.Empty);
    }
}
