using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlendShapeSearch
{
    internal static class FlatYaml
    {
        internal static string SerializeStrings(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var builder = new StringBuilder();
            foreach (var entry in entries)
            {
                builder.Append(Quote(entry.Key));
                builder.Append(": ");
                builder.Append(Quote(entry.Value));
                builder.Append('\n');
            }

            return builder.ToString();
        }

        internal static string SerializeFloats(IEnumerable<KeyValuePair<string, float>> entries)
        {
            var builder = new StringBuilder();
            foreach (var entry in entries)
            {
                builder.Append(Quote(entry.Key));
                builder.Append(": ");
                builder.Append(entry.Value.ToString("0.######", CultureInfo.InvariantCulture));
                builder.Append('\n');
            }

            return builder.ToString();
        }

        internal static Dictionary<string, string> Parse(string yaml)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(yaml))
            {
                return result;
            }

            var lines = yaml.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var rawLine in lines)
            {
                var line = StripComment(rawLine).Trim();
                if (line.Length == 0 || line.StartsWith("-", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = FindSeparator(line);
                if (separator <= 0)
                {
                    continue;
                }

                var key = Unquote(line.Substring(0, separator).Trim());
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                result[key] = Unquote(line.Substring(separator + 1).Trim());
            }

            return result;
        }

        internal static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
        }

        private static string Unquote(string value)
        {
            if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
            {
                return value;
            }

            var builder = new StringBuilder();
            for (var index = 1; index < value.Length - 1; index++)
            {
                var character = value[index];
                if (character != '\\' || index == value.Length - 2)
                {
                    builder.Append(character);
                    continue;
                }

                index++;
                switch (value[index])
                {
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case '\\': builder.Append('\\'); break;
                    case '"': builder.Append('"'); break;
                    default:
                        builder.Append(value[index]);
                        break;
                }
            }

            return builder.ToString();
        }

        private static int FindSeparator(string value)
        {
            var quoted = false;
            var escaped = false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\' && quoted)
                {
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (character == ':' && !quoted)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string StripComment(string value)
        {
            var quoted = false;
            var escaped = false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\' && quoted)
                {
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (character == '#' && !quoted)
                {
                    return value.Substring(0, index);
                }
            }

            return value;
        }
    }
}
