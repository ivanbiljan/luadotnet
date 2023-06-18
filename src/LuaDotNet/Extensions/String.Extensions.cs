using System.Text;

namespace LuaDotNet.Extensions
{
    /// <summary>
    ///     Provides extension methods for the <see cref="string" /> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Encodes the specified string using the specified character encoding.
        /// </summary>
        /// <param name="str">The string, which must not be <c>null</c>.</param>
        /// <param name="encoding">The encoding, which must not be <c>null</c>.</param>
        /// <returns>The encoded byte array.</returns>
        public static byte[] GetEncodedString(this string str, Encoding encoding)
        {
            var encodedBytes = new byte[encoding.GetByteCount(str)];
            encoding.GetBytes(str, 0, str.Length, encodedBytes, 0);

            return encodedBytes;
        }

        /// <summary>
        ///     Checks whether the specified string is null or consists purely of whitespace characters.
        /// </summary>
        /// <param name="str">The string, which must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the string is null or whitespace; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrWhitespace(this string str) => string.IsNullOrWhiteSpace(str);
    }
}