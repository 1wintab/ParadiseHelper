using System;
using System.Security.Cryptography;

namespace ParadiseHelper.SteamLogAccounts.SteamAuth
{
    /// <summary>
    /// Utility class responsible for generating the current 5-character Steam Guard 2FA code.
    /// This implementation is based on the standard TOTP (Time-based One-Time Password) algorithm,
    /// tailored to Steam's specific requirements (SHA1, 30-second interval, and custom alphabet).
    /// </summary>
    public static class SteamGuardCodeGenerator
    {
        /// <summary>
        /// Generates the current 5-character Steam Guard code from the account's shared secret.
        /// The process involves TOTP calculation followed by conversion to Steam's custom alphabet.
        /// </summary>
        /// <param name="sharedSecret">The Base64 encoded shared secret key for the Steam account.</param>
        /// <returns>A 5-character string representing the current Steam Guard code.</returns>
        public static string GenerateCode(string sharedSecret)
        {
            // 1. Convert the Base64 secret string into raw bytes.
            byte[] sharedSecretBytes = Convert.FromBase64String(sharedSecret);

            // Calculate the current time step (Unix seconds divided by the 30-second interval).
            long timeStep = GetSteamTime() / 30;
            // Convert time step to a byte array, which serves as the message for HMAC.
            byte[] timeArray = BitConverter.GetBytes(timeStep);

            // Adjust byte order (endianness) to ensure the time array is in Big-Endian format 
            // for correct HMAC-SHA1 hash calculation, as required by the TOTP standard.
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeArray);

            // 2. Compute the hash (HMAC-SHA1) using the shared secret and the time interval.
            byte[] hash;
            using (var hmac = new HMACSHA1(sharedSecretBytes))
            {
                // Compute the hash of the time step using the shared secret key.
                hash = hmac.ComputeHash(timeArray);
            }

            // 3. Perform dynamic truncation (RFC 4226) to get a 4-byte segment from the hash.

            // The last 4 bits of the hash determine the starting offset (0 to 15).
            int offset = hash[hash.Length - 1] & 0x0F;

            // Convert the 4 bytes at the offset into a single 32-bit integer.
            // The most significant bit (MSB) of the 4-byte segment is masked out (0x7F) 
            // to ensure the result is treated as a positive 31-bit integer.
            int codeInt = (hash[offset] & 0x7F) << 24
                        | (hash[offset + 1] & 0xFF) << 16
                        | (hash[offset + 2] & 0xFF) << 8
                        | hash[offset + 3] & 0xFF;

            // 4. Convert the integer into the final 5-character Steam Guard code using a custom alphabet.
            // Steam's alphabet consists of 26 characters (A-Z, 2-9 excluding I, L, O, S, Z).
            const string chars = "23456789BCDFGHJKMNPQRTVWXY";
            // Initialize the 5-character code array.
            char[] code = new char[5];

            // Iteratively map the 31-bit integer to 5 characters using the custom alphabet.
            for (int i = 0; i < 5; ++i)
            {
                // Use the modulo operation to find the index in the alphabet for the current character.
                code[i] = chars[codeInt % chars.Length];
                // Divide the integer to prepare for the next character generation.
                codeInt /= chars.Length;
            }

            return new string(code);
        }

        /// <summary>
        /// Gets the current Unix time in seconds (Time since 1970-01-01 00:00:00 UTC).
        /// Steam Guard relies on accurate UTC time for synchronization.
        /// </summary>
        /// <returns>The current time as a long integer representing seconds since the Unix Epoch.</returns>
        private static long GetSteamTime()
        {
            // DateTimeOffset.UtcNow ensures the time is accurately calculated in UTC.
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}