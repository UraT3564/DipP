using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public class DataRepository
    {
        private List<DataEntity> _entities = new List<DataEntity>();
        private readonly string _dataPath;
        private bool _integrityWarningShown = false;

        public DataRepository(string customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
                _dataPath = customPath;
            else
                _dataPath = Path.Combine("Res", "Data", "storage.json");
        }

        public void Load()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dataPath);

                if (!File.Exists(fullPath))
                {
                    _entities = new List<DataEntity>();
                    Save(); // Создаем пустой файл с хэшем
                    return;
                }

                // Проверка целостности файла
                string hashPath = fullPath + ".hash";
                if (File.Exists(hashPath) && !HashHelper.VerifyFileIntegrity(fullPath) && !_integrityWarningShown)
                {
                    _integrityWarningShown = true;
                    var result = MessageBox.Show(
                        "Файл данных поврежден или был изменен вручную.\n\n" +
                        "• Нажмите 'Да' - продолжить работу с текущим файлом (хэш будет обновлен)\n" +
                        "• Нажмите 'Нет' - восстановить данные из резервной копии\n" +
                        "• Нажмите 'Отмена' - создать новый пустой файл",
                        "Нарушение целостности данных",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        RestoreFromBackup(fullPath);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        _entities = new List<DataEntity>();
                        Save();
                        return;
                    }
                    // Если Да — продолжаем, хэш обновится при сохранении
                }

                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                _entities = JsonConvert.DeserializeObject<List<DataEntity>>(json) ?? new List<DataEntity>();

                Console.WriteLine($"[DataRepository] Загружено сущностей: {_entities.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataRepository] Ошибка загрузки: {ex.Message}");
                _entities = new List<DataEntity>();
            }
        }

        public void Save()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dataPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Создаем резервную копию перед сохранением
                if (File.Exists(fullPath))
                {
                    string backupPath = fullPath + ".backup";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Copy(fullPath, backupPath);
                }

                string json = JsonConvert.SerializeObject(_entities, Formatting.Indented);
                File.WriteAllText(fullPath, json, Encoding.UTF8);

                // Обновляем хэш после сохранения
                HashHelper.SaveHashFile(fullPath);

                Console.WriteLine($"[DataRepository] Сохранено сущностей: {_entities.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataRepository] Ошибка сохранения: {ex.Message}");
                throw;
            }
        }

        private void RestoreFromBackup(string fullPath)
        {
            string backupPath = fullPath + ".backup";
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, fullPath, true);
                HashHelper.SaveHashFile(fullPath);
                Load(); // Перезагружаем
                MessageBox.Show("Данные восстановлены из резервной копии", "Восстановление",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Резервная копия не найдена. Будет создан новый пустой файл.",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _entities = new List<DataEntity>();
                Save();
            }
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
