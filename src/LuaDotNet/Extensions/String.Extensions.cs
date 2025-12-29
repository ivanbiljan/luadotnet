using System.Text;

namespace LuaDotNet.Extensions;

/// <summary>
///     Provides extension methods for the <see cref="string" /> type.
/// </summary>
public static class StringExtensions
{
    /// <param name="str">The string, which must not be <c>null</c>.</param>
    extension(string str)
    {
        /// <summary>
        ///     Encodes the specified string using the specified character encoding.
        /// </summary>
        /// <param name="encoding">The encoding, which must not be <c>null</c>.</param>
        /// <returns>The encoded byte array.</returns>
        public byte[] GetEncodedString(Encoding encoding)
        {
            var encodedBytes = new byte[encoding.GetByteCount(str)];
            encoding.GetBytes(str, 0, str.Length, encodedBytes, 0);

            return encodedBytes;
        }

        /// <summary>
        ///     Checks whether the specified string is null or consists purely of whitespace characters.
        /// </summary>
        /// <returns><c>true</c> if the string is null or whitespace; otherwise, <c>false</c>.</returns>
        public bool IsNullOrWhitespace()
        {
            return string.IsNullOrWhiteSpace(str);
        }
    }
}