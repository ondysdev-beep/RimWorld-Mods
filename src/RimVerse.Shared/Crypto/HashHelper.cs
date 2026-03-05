using System.Security.Cryptography;
using System.Text;

namespace RimVerse.Shared.Crypto
{
    public static class HashHelper
    {
        public static string ComputeSha256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string ComputeModpackHash(string[] modPackageIds)
        {
            var sorted = new string[modPackageIds.Length];
            modPackageIds.CopyTo(sorted, 0);
            System.Array.Sort(sorted, System.StringComparer.OrdinalIgnoreCase);
            var combined = string.Join("|", sorted);
            return ComputeSha256(combined);
        }
    }
}
