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
            this.Text = _isEdit ? "Редактирование записи" : "Добавление записи";
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

            // Поле даты (для _event.Date)
            Label lblDate = new Label
            {
                Text = "Дата (ГГГГ-ММ-ДД):",
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

            // Остальные поля из конфига
            foreach (var fieldConfig in _storageConfig.Fields)
            {
                string displayName = fieldConfig.Display;
                string fieldKey = fieldConfig.Key;

                Label lbl = new Label
                {
                    Text = displayName + ":",
                    Location = new Point(leftMargin, y),
                    Size = new Size(labelWidth, 25),
                    TextAlign = ContentAlignment.MiddleRight
                };
                this.Controls.Add(lbl);

                Control inputControl;

                if (fieldConfig.Type == "date")
                {
                    DateTimePicker dtp = new DateTimePicker
                    {
                        Name = fieldKey,
                        Location = new Point(leftMargin + labelWidth + 5, y),
                        Size = new Size(controlWidth, 25),
                        Format = DateTimePickerFormat.Custom,
                        CustomFormat = "dd.MM.yyyy"
                    };
                    inputControl = dtp;
                }
                else if (fieldConfig.Type == "link")
                {
                    TextBox txt = new TextBox
                    {
                        Name = fieldKey,
                        Location = new Point(leftMargin + labelWidth + 5, y),
                        Size = new Size(controlWidth, 25)
                    };
                    inputControl = txt;
                }
                else
                {
                    TextBox txt = new TextBox
                    {
                        Name = fieldKey,
                        Location = new Point(leftMargin + labelWidth + 5, y),
                        Size = new Size(controlWidth, 25)
                    };
                    inputControl = txt;
                }

                this.Controls.Add(inputControl);
                _fieldControls[fieldKey] = inputControl;
                y += 35;
            }

            // Кнопки
            y += 10;
            Button btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(leftMargin + 100, y),
                Size = new Size(120, 35),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(leftMargin + 240, y),
                Size = new Size(120, 35),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.Height = y + 100;
        }

        private void LoadEventData()
        {
            if (_event.Fields.Count == 0 && _event.Date != null)
            {
                // Только дата
                if (_fieldControls.ContainsKey("Date") && _fieldControls["Date"] is TextBox txtDate)
                    txtDate.Text = _event.Date;
                return;
            }

            foreach (var field in _fieldControls)
            {
                if (field.Key == "Date")
                {
                    if (field.Value is TextBox txtDate)
                        txtDate.Text = _event.Date;
                }
                else if (_event.Fields.ContainsKey(field.Key))
                {
                    object value = _event.Fields[field.Key]?.ToString() ?? "";

                    if (field.Value is TextBox txt)
                    {
                        txt.Text = value.ToString();
                    }
                    else if (field.Value is DateTimePicker dtp)
                    {
                        if (DateTime.TryParse(value.ToString(), out DateTime parsedDate))
                            dtp.Value = parsedDate;
                    }
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

            // Сохраняем дату из текстового поля Date
            string selectedDate = "";
            if (_fieldControls.ContainsKey("Date") && _fieldControls["Date"] is TextBox txtDate)
            {
                if (DateTime.TryParse(txtDate.Text, out DateTime parsedDate))
                {
                    selectedDate = parsedDate.ToString("yyyy-MM-dd");
                    _event.Date = selectedDate;
                }
                else
                {
                    selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                    _event.Date = selectedDate;
                }
            }

            // Проверка на дубликат
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
                        $"Измените название или дату",
                        "Обнаружен дубликат",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            // Сохраняем остальные поля
            foreach (var field in _fieldControls)
            {
                if (field.Key == "Date") continue;

                if (field.Value is TextBox txt)
                {
                    string value = txt.Text;
                    _event.Fields[field.Key] = string.IsNullOrEmpty(value) ? "" : value;
                }
                else if (field.Value is DateTimePicker dtp)
                {
                    _event.Fields[field.Key] = dtp.Value.ToString("dd.MM.yyyy");
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}