using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NSign.Http
{
    /// <summary>
    /// Extensions for working with structured header values as defined in RFC 8941.
    /// </summary>
    public static class StructuredValuesExtensions
    {
        /// <summary>
        /// The regular expression to use to escape the back-slash and double-quote for serializing strings.
        /// </summary>
        private static readonly Regex StringEscaper = new Regex(@"[\\""]", RegexOptions.Compiled);

        /// <summary>
        /// Serializes the given object as a string (as per RFC 8941).
        /// </summary>
        /// <param name="value">
        /// The object to serialize.
        /// </param>
        /// <returns>
        /// A string representing the serialized object.
        /// </returns>
        public static string SerializeAsString(this object? value)
        {
            if (null == value)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is string stringValue)
            {
                return SerializeAsString(stringValue);
            }
            else if (value is bool boolValue)
            {
                return SerializeAsString(boolValue);
            }
            else if (value is ParsedItem item)
            {
                return SerializeAsString(item.Value);
            }
            else if (value is IEnumerable<ParsedItem> itemList)
            {
                return SerializeAsString(itemList);
            }
            else if (IsNumeric(value))
            {
                return value.ToString();
            }
            else if (IsByteSequence(value, out ReadOnlyMemory<byte> bytesValue))
            {
                return SerializeAsString(bytesValue);
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Serializes the given boolean value as a string (as per RFC 8941).
        /// </summary>
        /// <param name="value">
        /// The bool value to serialize.
        /// </param>
        /// <returns>
        /// A string representing the serialized value.
        /// </returns>
        public static string SerializeAsString(this bool value)
        {
            return $"?{(value ? '1' : '0')}";
        }

        /// <summary>
        /// Serializes the given string (as per RFC 8941).
        /// </summary>
        /// <param name="value">
        /// The string to serialize.
        /// </param>
        /// <returns>
        /// A string representing the serialized value.
        /// </returns>
        public static string SerializeAsString(this string value)
        {
            return $"\"{StringEscaper.Replace(value, "\\$0")}\"";
        }

        /// <summary>
        /// Serializes the given byte sequence (as per RFC 8941).
        /// </summary>
        /// <param name="value">
        /// A ReadOnlyMemory of byte to serialize.
        /// </param>
        /// <returns>
        /// A string representing the serialized value.
        /// </returns>
        public static string SerializeAsString(this ReadOnlyMemory<byte> value)
        {
            return $":{Convert.ToBase64String(value.Span)}:";
        }

        /// <summary>
        /// Serializes the given sequence of <see cref="ParsedItem"/> values (as per RFC 8941).
        /// </summary>
        /// <param name="itemList">
        /// An <see cref="IEnumerable{T}"/> of <see cref="ParsedItem"/> representing the items in the list to serialize.
        /// </param>
        /// <returns>
        /// A string that represents the list of items.
        /// </returns>
        public static string SerializeAsString(this IEnumerable<ParsedItem> itemList)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('(');
            int pos = 0;

            foreach (ParsedItem item in itemList)
            {
                if (pos++ > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(item.Value.SerializeAsString() + item.Parameters.SerializeAsParameters());
            }

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Serializes the given parameters as per RFC 8941.
        /// </summary>
        /// <param name="parameters">
        /// An IEnumerable of KeyValuePair if string and object representing the parameters to serialize.
        /// </param>
        /// <returns>
        /// A string representing the serialized parameters.
        /// </returns>
        public static string SerializeAsParameters(this IEnumerable<KeyValuePair<string, object>>? parameters)
        {
            if (null == parameters)
            {
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                if (parameter.Value is bool boolValue)
                {
                    // Per the RFC, if a boolean parameter is true, only the name must be serialized.
                    if (boolValue)
                    {
                        sb.Append($";{parameter.Key}");
                    }
                    else
                    {
                        sb.Append($";{parameter.Key}=?0");
                    }
                }
                else
                {
                    sb.Append($";{parameter.Key}={parameter.Value.SerializeAsString()}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tries to get a dictionary entry from a set of structured dictionary header values.
        /// </summary>
        /// <param name="values">
        /// The <see cref="IEnumerable{T}"/> of <see cref="String"/> value representing all the values for the header.
        /// </param>
        /// <param name="key">
        /// The key of the entry in the structured dictionary header to get the value for.
        /// </param>
        /// <param name="lastValue">
        /// On success, holds the last found value for the given key.
        /// </param>
        /// <returns>
        /// True if successful, or false otherwise.
        /// </returns>
        public static bool TryGetStructuredDictionaryValue(this IEnumerable<string> values, string key, out ParsedItem? lastValue)
        {
            lastValue = null;

            foreach (string value in values)
            {
                if (null == SfvParser.ParseDictionary(value, out IReadOnlyDictionary<string, ParsedItem> actualDict) &&
                    actualDict.TryGetValue(key, out ParsedItem valueForKey))
                {
                    lastValue = valueForKey;
                }
            }

            return lastValue.HasValue;
        }

        /// <summary>
        /// Tries to get a binary data value from the given <paramref name="item"/>.
        /// </summary>
        /// <param name="item">
        /// The <see cref="ParsedItem"/> from which to try to get a binary value.
        /// </param>
        /// <param name="data">
        /// If successful, holds the <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> that represents the binary
        /// value of the item.
        /// </param>
        /// <returns>
        /// True if successful, false otherwise.
        /// </returns>
        public static bool TryGetBinaryData(this ParsedItem item, out ReadOnlyMemory<byte> data)
        {
            return IsByteSequence(item.Value, out data);
        }

        #region Private Methods

        /// <summary>
        /// Checks if the given value is numeric.
        /// </summary>
        /// <param name="value">
        /// The object to check.
        /// </param>
        /// <returns>
        /// True if the value is numeric, or false otherwise.
        /// </returns>
        private static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte ||
                value is ushort || value is short ||
                value is uint || value is int ||
                value is ulong || value is long ||
                value is decimal || value is float || value is double;
        }

        /// <summary>
        /// Checks if the given value is a byte sequence.
        /// </summary>
        /// <param name="value">
        /// The object to check.
        /// </param>
        /// <param name="bytesValue">
        /// If the object is a byte sequence, holds the ReadOnlyMemory of byte representing the sequence, or null otherwise.
        /// </param>
        /// <returns>
        /// True if the value is a byte sequnce, or false otherwise.
        /// </returns>
        private static bool IsByteSequence(object value, out ReadOnlyMemory<byte> bytesValue)
        {
            if (value is byte[] bytesArray)
            {
                bytesValue = bytesArray;
                return true;
            }

            if (value is ReadOnlyMemory<byte> bytesMem)
            {
                bytesValue = bytesMem;
                return true;
            }

            bytesValue = null;

            return false;
        }

        #endregion
    }
}
