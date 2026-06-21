using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

                // 1. Пробуем загрузить основной файл
                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath, Encoding.UTF8);
                    var root = JsonConvert.DeserializeObject<StorageConfigRoot>(json);

                    if (root?.Storages != null && root.Storages.Count > 0)
                    {
                        return root.Storages;
                    }
                    Console.WriteLine("[StorageConfigService] Конфиг пуст");
                }
                else
                {
                    Console.WriteLine($"[StorageConfigService] Файл не найден: {fullPath}");
                }

                // 2. Пробуем восстановить из бекапа
                string backupPath = fullPath + ".backup";
                if (File.Exists(backupPath))
                {
                    Console.WriteLine($"[StorageConfigService] Восстанавливаем из бекапа: {backupPath}");

                    string json = File.ReadAllText(backupPath, Encoding.UTF8);
                    var root = JsonConvert.DeserializeObject<StorageConfigRoot>(json);

                    if (root?.Storages != null && root.Storages.Count > 0)
                    {
                        // Восстанавливаем основной файл из бекапа
                        File.Copy(backupPath, fullPath, true);
                        Console.WriteLine("[StorageConfigService] Конфиг восстановлен из бекапа");
                        return root.Storages;
                    }
                }

                // 3. Бекапа нет — возвращаем пустой список
                Console.WriteLine("[StorageConfigService] Бекап не найден, возвращаем пустой список");
                return new List<StorageConfig>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StorageConfigService] Ошибка загрузки: {ex.Message}");

                // При ошибке тоже пробуем бекап
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configPath);
                string backupPath = fullPath + ".backup";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        string json = File.ReadAllText(backupPath, Encoding.UTF8);
                        var root = JsonConvert.DeserializeObject<StorageConfigRoot>(json);
                        if (root?.Storages != null && root.Storages.Count > 0)
                        {
                            File.Copy(backupPath, fullPath, true);
                            return root.Storages;
                        }
                    }
                    catch { }
                }

                return new List<StorageConfig>();
            }
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
        // ===== НОВЫЕ МЕТОДЫ =====

        public void AddStorage(StorageConfig storage)
        {
            var storages = LoadAll();
            storage.Id = Guid.NewGuid().ToString(); // генерируем уникальный ID
            storages.Add(storage);
            SaveAll(storages);
        }

        public void UpdateStorage(StorageConfig storage)
        {
            var storages = LoadAll();
            int index = storages.FindIndex(s => s.Id == storage.Id);
            if (index >= 0)
            {
                storages[index] = storage;
                SaveAll(storages);
            }
        }

        public void DeleteStorage(string id, bool deleteFile = true)
        {
            var storages = LoadAll();
            var storageToDelete = storages.FirstOrDefault(s => s.Id == id);

            if (storageToDelete == null)
            {
                Console.WriteLine($"[StorageConfigService] Хранилище с Id={id} не найдено");
                return;
            }

            // Удаляем файл данных
            if (deleteFile && !string.IsNullOrEmpty(storageToDelete.FilePath))
            {
                DeleteStorageFile(storageToDelete.FilePath);
            }

            // Удаляем запись из конфигурации
            storages.RemoveAll(s => s.Id == id);
            SaveAll(storages);

            Console.WriteLine($"[StorageConfigService] Удалено хранилище {storageToDelete.DisplayName}");
        }


        // Создание пустого файла данных для хранилища
        public void CreateDataFileIfNotExists(string filePath)
        {
            // Защита от null или пустого пути
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("[StorageConfigService] Путь к файлу не указан");
                return;
            }

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            string directory = Path.GetDirectoryName(fullPath);

            // Проверка и создание директории
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"[StorageConfigService] Создана директория: {directory}");
            }

            // Создание файла, если его нет
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, "[]", Encoding.UTF8);
                Console.WriteLine($"[StorageConfigService] Создан файл: {fullPath}");
            }
        }

        public void DeleteStorageFile(string filePath)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Console.WriteLine($"[StorageConfigService] Файл удален: {fullPath}");
            }
            else
            {
                Console.WriteLine($"[StorageConfigService] Файл не найден: {fullPath}");
            }
        }
    }

    // Класс-обертка для корневого объекта JSON
    public class StorageConfigRoot
    {
        public List<StorageConfig> Storages { get; set; }
    }
}
