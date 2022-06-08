using StructuredFieldValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NSign.Http
{
    /// <summary>
    /// Represents the value of a structured field.
    /// </summary>
    public readonly ref struct StructuredFieldValue
    {
        /// <summary>
        /// Initializes a new instance of <see cref="StructuredFieldValue"/>.
        /// </summary>
        /// <param name="list">
        /// An <see cref="IReadOnlyList{T}"/> of <see cref="ParsedItem"/> that represents the list of structured values
        /// of a structured field of type list.
        /// </param>
        public StructuredFieldValue(IReadOnlyList<ParsedItem> list)
        {
            Type = StructuredFieldType.List;
            List = list;
            Dictionary = null;
            Item = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StructuredFieldValue"/>.
        /// </summary>
        /// <param name="dictionary">
        /// An <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and <see cref="ParsedItem"/> that
        /// represents the dictionary of structured values of a structured field of type dictionary.
        /// </param>
        public StructuredFieldValue(IReadOnlyDictionary<string, ParsedItem> dictionary)
        {
            Type = StructuredFieldType.Dictionary;
            List = null;
            Dictionary = dictionary;
            Item = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StructuredFieldValue"/>.
        /// </summary>
        /// <param name="item">
        /// A <see cref="ParsedItem"/> value that represents the value of a structured field of type item.
        /// </param>
        public StructuredFieldValue(ParsedItem item)
        {
            Type = StructuredFieldType.Item;
            List = null;
            Dictionary = null;
            Item = item;
        }

        /// <summary>
        /// Gets the <see cref="StructuredFieldType"/> value that defines the type of the structured field.
        /// </summary>
        public StructuredFieldType Type { get; }

        /// <summary>
        /// Gets a nullable <see cref="IReadOnlyList{T}"/> of <see cref="ParsedItem"/> that represents the list of
        /// structured values of the structured field.
        /// </summary>
        public IReadOnlyList<ParsedItem>? List { get; }

        /// <summary>
        /// Gets a nullable <see cref="IReadOnlyDictionary{TKey, TValue}"/> of <see cref="string"/> and
        /// <see cref="ParsedItem"/> that represents the dictionary of structured values of the structured field.
        /// </summary>
        public IReadOnlyDictionary<string, ParsedItem>? Dictionary { get; }

        /// <summary>
        /// Gets a nullable <see cref="ParsedItem"/> value that represents the value of the structured field.
        /// </summary>
        public ParsedItem? Item { get; }

        /// <summary>
        /// Serializes this structured field's value to a string.
        /// </summary>
        /// <returns>
        /// A string representing the value of the structured field.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// The field type is unknown and cannot be serialized.
        /// </exception>
        public string Serialize()
        {
            switch (Type)
            {
                case StructuredFieldType.List:
                    Debug.Assert(null != List, "The list must not be null.");
                    return List.SerializeAsString(innerList: false);

                case StructuredFieldType.Dictionary:
                    Debug.Assert(null != Dictionary, "The dictionary must not be null.");
                    return Dictionary.SerializeAsString();

                case StructuredFieldType.Item:
                    Debug.Assert(null != Item, "The item must not be null.");
                    return Item.SerializeAsString();

                default:
                    throw new NotSupportedException("Cannot serialize a field of type Unknown type.");
            }
        }
    }
}
