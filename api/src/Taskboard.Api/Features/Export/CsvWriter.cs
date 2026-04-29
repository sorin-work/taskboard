using System.Text;

namespace Taskboard.Api.Features.Export;

/// <summary>Minimal RFC 4180 writer: quotes fields that need it and doubles quotes.</summary>
public sealed class CsvWriter
{
    private readonly StringBuilder _builder = new();

    public CsvWriter Row(params string?[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                _builder.Append(',');
            }

            _builder.Append(Escape(fields[i] ?? string.Empty));
        }

        _builder.Append("\r\n");
        return this;
    }

    public override string ToString() => _builder.ToString();

    internal static string Escape(string value)
    {
        var needsQuotes = value.AsSpan().IndexOfAny(",\"\r\n") >= 0;
        return needsQuotes ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
    }
}
