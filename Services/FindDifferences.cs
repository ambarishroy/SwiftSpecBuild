using System.Security.Cryptography;

namespace SwiftSpecBuild.Services
{
    public class FindDifferences
    {
        public static string ComputeSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        public static bool AreDifferent(string file1Path, string file2Path)
        {
            var hash1 = ComputeSHA256(file1Path);
            var hash2 = ComputeSHA256(file2Path);
            return hash1 != hash2;
        }
    }
}
