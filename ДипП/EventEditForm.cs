using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ДипП.Models;

namespace ДипП
{
    public partial class EventEditForm : Form
    {
        private DataEntity _event;
        private DataRepository _repository;
        private Dictionary<string, Control> _fieldControls;
        private StorageConfig _storageConfig;
        private bool _isEdit;

        // Конструктор для добавления — анализируем все существующие события
        public EventEditForm(DataRepository repository, StorageConfig storageConfig, DataEntity existingEvent = null)
        {
            _repository = repository;
            _storageConfig = storageConfig;
            _event = existingEvent ?? new DataEntity
            {
                Id = Guid.NewGuid().ToString(),
                Type = storageConfig.Type,
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Fields = new Dictionary<string, object>()
            };
            _isEdit = existingEvent != null;
            _fieldControls = new Dictionary<string, Control>();

            SetupForm();
            LoadEventData();
        }

        public DataEntity GetEvent() => _event;

        private void SetupForm()
        {
            bool isEdit = _event.Fields.Count > 0;
            this.Text = isEdit ? "Редактирование записи" : "Добавление записи";
            this.Size = new Size(500, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.AutoScroll = true;

            int y = 15;
            int labelWidth = 140;
            int controlWidth = 270;
            int leftMargin = 15;

            // Поле даты (обязательное)
            Label lblDate = new Label
            {
                Text = "Дата (YYYY-MM-DD):",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            TextBox txtDate = new TextBox
            {
                Name = "Date",
                Location = new Point(leftMargin + labelWidth + 5, y),
                Size = new Size(controlWidth, 25),
                Text = _event.Date
            };
            this.Controls.Add(lblDate);
            this.Controls.Add(txtDate);
            _fieldControls["Date"] = txtDate;
            y += 35;

            // Получаем все возможные ключи из всех event-записей в хранилище
            var allEvents = _repository.GetByType("event");
            var allKeys = new HashSet<string>();

            foreach (var ev in allEvents)
            {
                foreach (var key in ev.Fields.Keys)
                {
                    allKeys.Add(key);
                }
            }

            // Если ключей нет (пустое хранилище) — используем стандартный набор
            if (allKeys.Count == 0)
            {
                allKeys.Add("EventName");
                allKeys.Add("Participants");
                allKeys.Add("Location");
                allKeys.Add("Responsible");
                allKeys.Add("EventLink");
                allKeys.Add("Children0to14");
                allKeys.Add("Children14to35");
                allKeys.Add("AdultsOver35");
            }

            // Сортируем ключи для удобства
            //var orderedKeys = allKeys.OrderBy(k => k).ToList();

            // Создаем поля для каждого ключа
            foreach (var fieldConfig in _storageConfig.Fields)
            {
                Label lbl = new Label
                {
                    Text = fieldConfig.Display + ":",
                    Location = new Point(leftMargin, y),
                    Size = new Size(labelWidth, 25),
                    TextAlign = ContentAlignment.MiddleRight
                };

                TextBox txt = new TextBox
                {
                    Name = fieldConfig.Key,
                    Location = new Point(leftMargin + labelWidth + 5, y),
                    Size = new Size(controlWidth, 25)
                };

                this.Controls.Add(lbl);
                this.Controls.Add(txt);
                _fieldControls[fieldConfig.Key] = txt;
                y += 35;
            }

            y += 20;

            // Кнопки
            Button btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(leftMargin + 100, y),
                Size = new Size(120, 35),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(leftMargin + 240, y),
                Size = new Size(120, 35),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            this.Height = y + 100;
        }

        private void LoadEventData()
        {
            if (_event.Fields.Count == 0) return;

            foreach (var field in _fieldControls)
            {
                if (field.Key == "Date")
                {
                    ((TextBox)field.Value).Text = _event.Date;
                }
                else if (_event.Fields.ContainsKey(field.Key))
                {
                    ((TextBox)field.Value).Text = _event.Fields[field.Key]?.ToString() ?? "";
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Проверка обязательных полей
            if (_fieldControls.ContainsKey("EventName"))
            {
                var nameControl = _fieldControls["EventName"] as TextBox;
                if (string.IsNullOrWhiteSpace(nameControl?.Text))
                {
                    MessageBox.Show("Введите название мероприятия", "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Сохраняем дату
            string selectedDate = "";
            if (_fieldControls.ContainsKey("Date") && _fieldControls["Date"] is TextBox txtDate)
            {
                if (DateTime.TryParse(txtDate.Text, out DateTime parsedDate))
                {
                    selectedDate = parsedDate.ToString("yyyy-MM-dd");
                    _event.Date = selectedDate;
                    _event.Fields["EventDate"] = parsedDate.ToString("dd.MM.yyyy");
                }
                else
                {
                    selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                    _event.Date = selectedDate;
                    _event.Fields["EventDate"] = DateTime.Now.ToString("dd.MM.yyyy");
                }
            }

            // Проверка на дубликат (при добавлении нового события)
            if (!_isEdit)
            {
                string eventName = "";
                if (_fieldControls.ContainsKey("EventName") && _fieldControls["EventName"] is TextBox txtName)
                    eventName = txtName.Text;

                var existingEvents = _repository.GetByType(_storageConfig.Type);
                var duplicate = existingEvents.FirstOrDefault(ev =>
                    ev.Date == selectedDate &&
                    ev.Fields.ContainsKey("EventName") &&
                    ev.Fields["EventName"]?.ToString() == eventName);

                if (duplicate != null)
                {
                    MessageBox.Show(
                        $"В этот день уже есть событие с таким названием:\n\n" +
                        $"Дата: {selectedDate}\n" +
                        $"Название: {eventName}\n\n" +
                        $"Выберите действие:\n" +
                        $"• Если хотите изменить существующее — нажмите 'Отмена' и выберите его для редактирования\n" +
                        $"• Если хотите добавить новое — измените название или дату",
                        "Обнаружен дубликат",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            // Сохраняем остальные поля...
            foreach (var field in _fieldControls)
            {
                if (field.Key == "Date") continue;
                if (field.Value is TextBox txt)
                {
                    string value = txt.Text;
                    _event.Fields[field.Key] = string.IsNullOrEmpty(value) ? "" : value;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}