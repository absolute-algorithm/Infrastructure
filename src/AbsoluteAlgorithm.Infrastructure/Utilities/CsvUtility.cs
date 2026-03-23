using System.Globalization;
using System.Reflection;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for reading and writing CSV content.
/// </summary>
public static class CsvUtility
{
    /// <summary>
    /// Reads CSV content and maps the rows to a list of the specified type.
    /// </summary>
    /// <typeparam name="T">The target record type.</typeparam>
    /// <param name="csvContent">The CSV content to read.</param>
    /// <param name="hasHeaderRecord"><see langword="true"/> when the first row contains headers; otherwise, <see langword="false"/>.</param>
    /// <returns>A list of mapped records.</returns>
    public static List<T> ReadFromCsv<T>(string csvContent, bool hasHeaderRecord = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(csvContent);

        using var reader = new StringReader(csvContent);
        return ReadFromCsv<T>(reader, hasHeaderRecord);
    }

    /// <summary>
    /// Reads CSV data from a stream and maps the rows to a list of the specified type.
    /// </summary>
    /// <typeparam name="T">The target record type.</typeparam>
    /// <param name="stream">The stream that contains CSV data.</param>
    /// <param name="hasHeaderRecord"><see langword="true"/> when the first row contains headers; otherwise, <see langword="false"/>.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after reading; otherwise, <see langword="false"/>.</param>
    /// <returns>A list of mapped records.</returns>
    public static List<T> ReadFromCsv<T>(Stream stream, bool hasHeaderRecord = true, Encoding? encoding = null, bool leaveOpen = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: leaveOpen);
        return ReadFromCsv<T>(reader, hasHeaderRecord);
    }

    /// <summary>
    /// Writes the specified records to CSV content.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to write.</param>
    /// <param name="includeHeaderRecord"><see langword="true"/> to include property names as the header row; otherwise, <see langword="false"/>.</param>
    /// <returns>The generated CSV content.</returns>
    public static string WriteToCsv<T>(IEnumerable<T> records, bool includeHeaderRecord = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(records);

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        WriteToCsv(records, writer, includeHeaderRecord);

        return writer.ToString();
    }

    /// <summary>
    /// Writes the specified records to a CSV stream.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to write.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="includeHeaderRecord"><see langword="true"/> to include property names as the header row; otherwise, <see langword="false"/>.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after writing; otherwise, <see langword="false"/>.</param>
    public static void WriteToCsv<T>(IEnumerable<T> records, Stream stream, bool includeHeaderRecord = true, Encoding? encoding = null, bool leaveOpen = false) where T : class
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: leaveOpen);
        WriteToCsv(records, writer, includeHeaderRecord);
        writer.Flush();
    }

    private static List<T> ReadFromCsv<T>(TextReader reader, bool hasHeaderRecord) where T : class
    {
        var configuration = CreateConfiguration(hasHeaderRecord);
        using var csv = new CsvReader(reader, configuration);
        RegisterMap<T>(csv.Context);

        return csv.GetRecords<T>().ToList();
    }

    private static void WriteToCsv<T>(IEnumerable<T> records, TextWriter writer, bool includeHeaderRecord) where T : class
    {
        var configuration = CreateConfiguration(includeHeaderRecord);
        using var csv = new CsvWriter(writer, configuration);
        RegisterMap<T>(csv.Context);
        csv.WriteRecords(records);
    }

    private static CsvConfiguration CreateConfiguration(bool hasHeaderRecord)
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaderRecord,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? string.Empty
        };
    }

    private static void RegisterMap<T>(CsvContext context) where T : class
    {
        context.RegisterClassMap(new PropertyOrderMap<T>());
    }

    private sealed class PropertyOrderMap<T> : ClassMap<T> where T : class
    {
        public PropertyOrderMap()
        {
            var properties = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite)
                .OrderBy(property => property.MetadataToken)
                .ToArray();

            for (var index = 0; index < properties.Length; index++)
            {
                Map(typeof(T), properties[index]).Index(index).Name(properties[index].Name);
            }
        }
    }
}