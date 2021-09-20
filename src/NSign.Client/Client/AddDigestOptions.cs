﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NSign.Client
{
    /// <summary>
    /// Options class to control the creation/adding of 'Digest' headers for outgoing messages.
    /// </summary>
    public sealed class AddDigestOptions
    {
        /// <summary>
        /// Gets an ICollection of <see cref="Hash"/> values defining the hash algorithms to use for the 'Digest' header.
        /// </summary>
        public ICollection<Hash> Hashes { get; } = new Collection<Hash>();

        /// <summary>
        /// Instructs the <see cref="AddDigestHandler"/> to add the given <see cref="Hash"/> to the 'Digest' header.
        /// </summary>
        /// <param name="hash">
        /// The <see cref="Hash"/> value defining the hash algorithm to create a 'Digest' header value for.
        /// </param>
        /// <returns>
        /// The <see cref="AddDigestOptions"/> instance.
        /// </returns>
        public AddDigestOptions WithHash(Hash hash)
        {
            Hashes.Add(hash);

            return this;
        }

        /// <summary>
        /// Enum to define hash algorithms.
        /// </summary>
        public enum Hash
        {
            /// <summary>
            /// Unknown hash algorithm. Must not be configured.
            /// </summary>
            Unknown,

            /// <summary>
            /// Generate a SHA-256 hash of request content.
            /// </summary>
            Sha256,

            /// <summary>
            /// Generate a SHA-512 hash of request content.
            /// </summary>
            Sha512,
        }
    }
}
