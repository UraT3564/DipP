using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
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
                string json = File.ReadAllText(configFilePath);
                configs = JsonConvert.DeserializeObject<List<TemplateConfig>>(json);

                foreach (var config in configs)
                {
                    if (string.IsNullOrEmpty(config.Id))
                        config.Id = Guid.NewGuid().ToString(); // на всякий случай
                }

                Console.WriteLine($"[ConfigService] Загружено шаблонов: {configs.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigService] Ошибка: {ex.Message}");
            }

            return configs;
        }
    }
    }