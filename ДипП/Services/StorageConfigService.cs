using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ДипП.Models;

namespace ДипП.Services
{
    public class StorageConfigService
    {
        private readonly string _configPath = Path.Combine("Res", "storage_config.json");

        public List<StorageConfig> LoadAll()
{
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configPath);
            if (!File.Exists(fullPath)) return new List<StorageConfig>();

            string json = File.ReadAllText(fullPath, Encoding.UTF8);
            var root = JsonConvert.DeserializeObject<StorageConfigRoot>(json);
            return root?.Storages ?? new List<StorageConfig>();
        }
    }
}