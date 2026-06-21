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
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
            this.MaximizeBox = false;
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

            this.Icon = new Icon(System.IO.Path.Combine(Application.StartupPath, "Res\\icons8.ico"));
            // Поле даты (для _event.Date)
            Label lblDate = new Label
            {
                Text = "Дата:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblDate);

            DateTimePicker dtpDate = new DateTimePicker
            {
                Name = "Date",
                Location = new Point(leftMargin + labelWidth + 5, y),
                Size = new Size(controlWidth, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy"
            };
            this.Controls.Add(dtpDate);
            _fieldControls["Date"] = dtpDate;  // теперь контрол — DateTimePicker, а не TextBox
            y += 35;

            // Остальные поля из конфига
            foreach (var fieldConfig in _storageConfig.Fields)
            {
                string displayName = fieldConfig.Display;
                int fieldId = fieldConfig.Id;  // было Key

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
                        Name = Convert.ToString(fieldId),  // было Key
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
                        Name = Convert.ToString(fieldId),  // было Key
                        Location = new Point(leftMargin + labelWidth + 5, y),
                        Size = new Size(controlWidth, 25)
                    };
                    inputControl = txt;
                }
                else
                {
                    TextBox txt = new TextBox
                    {
                        Name = Convert.ToString(fieldId),  // было Key
                        Location = new Point(leftMargin + labelWidth + 5, y),
                        Size = new Size(controlWidth, 25)
                    };
                    inputControl = txt;
                }

                this.Controls.Add(inputControl);
                _fieldControls[Convert.ToString(fieldId)] = inputControl;  // было Key
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
            if (!_isEdit) return;

            // Загружаем дату
            if (_fieldControls.ContainsKey("Date") && _fieldControls["Date"] is DateTimePicker dtpDate)
            {
                if (_event.Fields.ContainsKey("EventDate") && DateTime.TryParse(_event.Fields["EventDate"]?.ToString(), out DateTime eventDate))
                    dtpDate.Value = eventDate;
                else if (!string.IsNullOrEmpty(_event.Date) && DateTime.TryParse(_event.Date, out DateTime dateFromDate))
                    dtpDate.Value = dateFromDate;
            }

            // Загружаем остальные поля по Id
            foreach (var field in _fieldControls)
            {
                string fieldId = field.Key;  // теперь это Id, а не Key
                if (fieldId == "Date") continue;

                if (field.Value is TextBox txt && _event.Fields.ContainsKey(fieldId))
                {
                    txt.Text = _event.Fields[fieldId]?.ToString() ?? "";
                }
                else if (field.Value is DateTimePicker dtp && _event.Fields.ContainsKey(fieldId))
                {
                    if (DateTime.TryParse(_event.Fields[fieldId]?.ToString(), out DateTime parsedDate))
                        dtp.Value = parsedDate;
                }
                else if (field.Value is ComboBox combo && _event.Fields.ContainsKey(fieldId))
                {
                    combo.SelectedItem = _event.Fields[fieldId]?.ToString();
                }
                else if (field.Value is CheckBox chk && _event.Fields.ContainsKey(fieldId))
                {
                    chk.Checked = _event.Fields[fieldId]?.ToString() == "true";
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Получаем дату
            DateTime selectedDate = DateTime.Now;
            if (_fieldControls.ContainsKey("Date") && _fieldControls["Date"] is DateTimePicker dtpDate)
            {
                selectedDate = dtpDate.Value;
                _event.Date = selectedDate.ToString("yyyy-MM-dd");
                _event.Fields["EventDate"] = selectedDate.ToString("dd.MM.yyyy");
            }

            // Проверка на дубликат
            if (!_isEdit)
            {
                string eventName = "";
                var nameField = _storageConfig.Fields.FirstOrDefault(f => f.Display == "Название мероприятия" || f.Required);
                if (nameField != null && _fieldControls.ContainsKey(nameField.Id.ToString()) && _fieldControls[nameField.Id.ToString()] is TextBox txtName)
                {
                    eventName = txtName.Text;
                }

                var existingEvents = _repository.GetByType(_storageConfig.Type);
                var duplicate = existingEvents.FirstOrDefault(ev =>
                    ev.Date == selectedDate.ToString("yyyy-MM-dd") &&
                    ev.Fields.ContainsKey(nameField?.Id.ToString() ?? "") &&
                    ev.Fields[nameField?.Id.ToString() ?? ""]?.ToString() == eventName);

                if (duplicate != null)
                {
                    MessageBox.Show("В этот день уже есть мероприятие с таким названием",
                        "Дубликат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Сохраняем остальные поля
            foreach (var field in _fieldControls)
            {
                string fieldKey = field.Key;
                if (fieldKey == "Date") continue;

                if (field.Value is TextBox txt)
                    _event.Fields[fieldKey] = txt.Text;
                else if (field.Value is DateTimePicker dtp)
                    _event.Fields[fieldKey] = dtp.Value.ToString("dd.MM.yyyy");
                else if (field.Value is ComboBox combo)
                    _event.Fields[fieldKey] = combo.SelectedItem?.ToString() ?? "";
                else if (field.Value is CheckBox chk)
                    _event.Fields[fieldKey] = chk.Checked ? "true" : "false";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}