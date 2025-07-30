using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;


namespace CTFHackTools
{
    public static class CTFTools
    {
        /// <summary>
        /// Gets the difference (offset) between two addresses.
        /// </summary>
        public static long GetOffset(string address1Hex, string address2Hex)
        {
            long addr1 = Convert.ToInt64(address1Hex, 16);
            long addr2 = Convert.ToInt64(address2Hex, 16);
            return addr2 - addr1;
        }

        /// <summary>
        /// Adds an integer offset to a given hex address (as string or long).
        /// Returns the result as a hex string (prefixed 0x).
        /// </summary>
        public static string AddOffset(string addressHex, int offset)
        {
            long addr = Convert.ToInt64(addressHex, 16);
            long result = addr + offset;
            return $"0x{result:X}";
        }

        /// <summary>
        /// Adds an integer offset to a given address (as long).
        /// </summary>
        public static long AddOffset(long address, int offset)
        {
            return address + offset;
        }

        /// <summary>
        /// Formats a long address as a hex string.
        /// </summary>
        public static string ToHex(long address)
        {
            return $"0x{address:X}";
        }


        /// <summary>
        /// Decodes a URL-encoded string.
        /// </summary>
        public static string UrlDecode(string input)
        {
            return WebUtility.UrlDecode(input);
        }

        /// <summary>
        /// Decodes a Base64-encoded string.
        /// </summary>
        public static string Base64Decode(string input)
        {
            var bytes = Convert.FromBase64String(input);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        

    public static string? TryAutoDecodeBase64(string input, out string? foundBase64)
    {
        // Regex: finds any "long enough" base64-like string (32+ chars, with = padding optional)
        var matches = Regex.Matches(input, @"([A-Za-z0-9+/]{20,}={0,2})");
        foreach (Match m in matches)
        {
            string candidate = m.Value;

            // Make sure it's a multiple of 4 for base64
            int mod = candidate.Length % 4;
            if (mod != 0)
            {
                candidate = candidate.PadRight(candidate.Length + (4 - mod), '=');
            }

            try
            {
                byte[] data = Convert.FromBase64String(candidate);
                // You can also check if it's printable/text:
                string decoded = System.Text.Encoding.UTF8.GetString(data);

                // Optionally, check if it looks like a flag (picoCTF, CTF{, etc)
                if (Regex.IsMatch(decoded, @"(picoCTF|CTF\{|\{flag)", RegexOptions.IgnoreCase))
                {
                    foundBase64 = candidate;
                    return decoded;
                }
                // Or just return the first successful decode
                foundBase64 = candidate;
                return decoded;
            }
            catch
            {
                // Not valid base64, skip
            }
        }
        foundBase64 = null;
        return null;
    }

}
}
