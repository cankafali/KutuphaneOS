using System;
using System.Security.Cryptography;

namespace KutuphaneMvc.Utils
{
    public static class PasswordHasher
    {
        // v1$iterations$Base64(salt)$Base64(key)
        public static string Hash(string password, int iterations = 100_000)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var key = pbkdf2.GetBytes(32);
                return $"v1${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
            }
        }

        public static bool Verify(string password, string encoded)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encoded))
                return false;

            try
            {
                var p = encoded.Trim().Split('$');
                if (p.Length != 4 || p[0] != "v1") return false;

                if (!int.TryParse(p[1], out var iter)) return false;

                var salt = Convert.FromBase64String(p[2]);
                var key = Convert.FromBase64String(p[3]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256))
                {
                    var attempt = pbkdf2.GetBytes(key.Length);
                    return FixedTimeEquals(key, attempt);
                }
            }
            catch
            {
                return false;
            }
        }

        // .NET Framework uyumlu sabit-zaman karşılaştırma
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}