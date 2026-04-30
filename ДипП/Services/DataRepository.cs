using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ДипП.Models;

namespace ДипП
{
    public class DataRepository
    {
        private List<DataEntity> _entities = new List<DataEntity>();
        private readonly string _dataPath;

        public DataRepository(string customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
                _dataPath = customPath;
            else
                _dataPath = Path.Combine("Res", "Data", "storage.json");
        }
        public void Load()
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dataPath);
            Console.WriteLine($"Загрузка данных из: {fullPath}");

            if (File.Exists(fullPath))
            {
                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                Console.WriteLine($"JSON прочитан, длина: {json.Length}");
                _entities = JsonConvert.DeserializeObject<List<DataEntity>>(json) ?? new List<DataEntity>();
                Console.WriteLine($"Загружено сущностей: {_entities.Count}");
            }
            else
            {
                Console.WriteLine($"Файл не найден: {fullPath}");
            }
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(_entities, Formatting.Indented);
            File.WriteAllText(_dataPath, json, Encoding.UTF8);
        }

        public List<DataEntity> GetByType(string type)
        {
            return _entities.Where(e => e.Type == type).ToList();
        }

        public List<DataEntity> GetByDateRange(DateTime start, DateTime end)
        {
            return _entities.Where(e =>
            {
                if (DateTime.TryParse(e.Date, out DateTime entityDate))
                    return entityDate >= start && entityDate <= end;
                return false;
            }).ToList();
        }

        public void Add(DataEntity entity)
        {
            _entities.Add(entity);
            Save();
        }

        public void Update(DataEntity entity)
        {
            var index = _entities.FindIndex(e => e.Id == entity.Id);
            if (index >= 0)
            {
                _entities[index] = entity;
                Save();
            }
        }

        public void Delete(string id)
        {
            _entities.RemoveAll(e => e.Id == id);
            Save();
        }
    }
}
