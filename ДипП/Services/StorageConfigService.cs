using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ДипП.Models;

namespace ДипП.Services
{
    public class StorageConfigService
    {
        private readonly string _configPath = Path.Combine("Res", "storage_config.json");

        public List<StorageConfig> LoadAll()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configPath);

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"[StorageConfigService] Файл не найден: {fullPath}");
                    return CreateDefaultStorage();
                }

                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                var root = JsonConvert.DeserializeObject<StorageConfigRoot>(json);

                if (root?.Storages == null || root.Storages.Count == 0)
                {
                    Console.WriteLine("[StorageConfigService] Конфиг пуст, создаю хранилище по умолчанию");
                    return CreateDefaultStorage();
                }

                return root.Storages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StorageConfigService] Ошибка загрузки: {ex.Message}");
                return CreateDefaultStorage();
            }
        }

        private List<StorageConfig> CreateDefaultStorage()
        {
            var defaultStorage = new StorageConfig
            {
                Id = "main_events",
                DisplayName = "Основное хранилище (мероприятия)",
                FilePath = "Res/Data/storage.json",
                Type = "event",
                Fields = new List<StorageField>
                {
                    new StorageField { Key = "EventName", Display = "Название мероприятия", Type = "string", Required = true },
                    new StorageField { Key = "EventDate", Display = "Дата", Type = "date", Required = true },
                    new StorageField { Key = "EventLink", Display = "Ссылка", Type = "link", Required = false },
                    new StorageField { Key = "Participants", Display = "Участники (всего)", Type = "number", Required = false },
                    new StorageField { Key = "TotalParticipants", Display = "Участники (всего)", Type = "number", Required = false },
                    new StorageField { Key = "Location", Display = "Место", Type = "string", Required = false },
                    new StorageField { Key = "Responsible", Display = "Ответственный", Type = "string", Required = false },
                    new StorageField { Key = "Children0to14", Display = "Дети 0-14 лет", Type = "number", Required = false },
                    new StorageField { Key = "Children14to35", Display = "Дети 14-35 лет", Type = "number", Required = false },
                    new StorageField { Key = "AdultsOver35", Display = "Взрослые (35+)", Type = "number", Required = false }
                }
            };

            return new List<StorageConfig> { defaultStorage };
        }

        public void SaveAll(List<StorageConfig> storages)
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configPath);
                var root = new StorageConfigRoot { Storages = storages };
                string json = JsonConvert.SerializeObject(root, Formatting.Indented);
                File.WriteAllText(fullPath, json, Encoding.UTF8);
                Console.WriteLine($"[StorageConfigService] Сохранено хранилищ: {storages.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StorageConfigService] Ошибка сохранения: {ex.Message}");
                throw;
            }
        }
    }
}