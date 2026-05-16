using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using ДипП.Models;

namespace ДипП.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath = Path.Combine("Res", "ProgrammSettings.json");

        public string GetOrganizationName()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    throw new FileNotFoundException($"Файл настроек не найден: {_settingsPath}");
                }

                string json = File.ReadAllText(_settingsPath, Encoding.UTF8);
                var data = JObject.Parse(json);
                return data["organizationName"]?.ToString() ?? "Организация не указана";
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка чтения настроек: {ex.Message}");
            }
        }

        public void SetOrganizationName(string name)
        {
            try
            {
                string json = $"{{\"organizationName\": \"{name}\"}}";
                File.WriteAllText(_settingsPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения настроек: {ex.Message}");
            }
        }
    }
}