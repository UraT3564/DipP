using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ДипП.Services
{
    public static class HashHelper
    {
        /// <summary>
        /// Вычисляет SHA256 хэш файла
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        /// <summary>
        /// Вычисляет SHA256 хэш строки
        /// </summary>
        public static string ComputeStringHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Сохраняет хэш файла в отдельный файл
        /// </summary>
        public static void SaveHashFile(string filePath)
        {
            string hash = ComputeFileHash(filePath);
            string hashFilePath = filePath + ".hash";
            File.WriteAllText(hashFilePath, hash, Encoding.UTF8);
        }

        /// <summary>
        /// Проверяет целостность файла по сохраненному хэшу
        /// </summary>
        public static bool VerifyFileIntegrity(string filePath)
        {
            string hashFilePath = filePath + ".hash";
            if (!File.Exists(hashFilePath))
                return true; // Если нет хэш-файла, считаем что всё в порядке (первый запуск)

            string currentHash = ComputeFileHash(filePath);
            string savedHash = File.ReadAllText(hashFilePath, Encoding.UTF8).Trim();

            return string.Equals(currentHash, savedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}