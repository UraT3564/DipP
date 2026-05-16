using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ДипП.Models;

namespace ДипП.Services
{
    public class ConfigService
    {
        private readonly string _configsPath = "Res";

        public List<TemplateConfig> GetAllConfigs()
        {
            var configs = new List<TemplateConfig>();
            string configFilePath = Path.Combine(_configsPath, "config_report.json");

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"[ConfigService] Файл не найден: {configFilePath}");
                return configs;
            }

            try
            {
                // Проверка целостности
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFilePath);
                if (!HashHelper.VerifyFileIntegrity(fullPath))
                {
                    MessageBox.Show(
                        "Файл конфигурации шаблонов поврежден. Будет использована резервная копия.\n\n" +
                        "Проверьте целостность файла config_report.json",
                        "Нарушение целостности",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    // Пробуем восстановить из бэкапа
                    string backupPath = fullPath + ".backup";
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, fullPath, true);
                        HashHelper.SaveHashFile(fullPath);
                    }
                }

                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                configs = JsonConvert.DeserializeObject<List<TemplateConfig>>(json);

                // ... остальной код
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Ошибка: {ex.Message}");
            }

            return configs;
        }

        public void SaveAllConfigs(List<TemplateConfig> configs)
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configsPath, "config_report.json");
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Резервная копия
                if (File.Exists(fullPath))
                {
                    string backupPath = fullPath + ".backup";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Copy(fullPath, backupPath);
                }

                string json = JsonConvert.SerializeObject(configs, Formatting.Indented);
                File.WriteAllText(fullPath, json, Encoding.UTF8);

                // Обновляем хэш
                HashHelper.SaveHashFile(fullPath);

                Console.WriteLine($"[ConfigService] Сохранено шаблонов: {configs.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Ошибка сохранения: {ex.Message}");
                throw;
            }
        }
    }
}