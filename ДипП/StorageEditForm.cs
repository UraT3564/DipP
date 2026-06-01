using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public partial class StorageEditForm : Form
    {
        private StorageConfigService _configService;
        private StorageConfig _storage;
        private bool _isEditMode;
        private List<StorageField> _fields;

        // Элементы управления
        private TextBox txtDisplayName;
        private TextBox txtFilePath;
        private TextBox txtType;
        private DataGridView dgvFields;
        private Button btnSave;
        private Button btnCancel;

        public StorageEditForm(StorageConfigService configService, StorageConfig existingStorage = null)
        {
            _configService = configService;
            _storage = existingStorage ?? new StorageConfig
            {
                Id = Guid.NewGuid().ToString(),
                Fields = new List<StorageField>()
            };
            _isEditMode = existingStorage != null;
            _fields = _storage.Fields ?? new List<StorageField>();

            InitializeComponent();
            SetupForm();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = _isEditMode ? "Редактирование хранилища" : "Новое хранилище";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(650, 500);
        }

        private void SetupForm()
        {
            int margin = 15;
            int y = 15;
            int labelWidth = 150;
            int controlWidth = 480;

            // ===== Основные параметры =====
            GroupBox grpMain = new GroupBox
            {
                Text = "Основные параметры",
                Location = new Point(margin, y),
                Size = new Size(this.ClientSize.Width - margin * 2, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(grpMain);

            int gy = 25;

            // Отображаемое имя
            Label lblDisplayName = new Label
            {
                Text = "Отображаемое имя:",
                Location = new Point(10, gy),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            grpMain.Controls.Add(lblDisplayName);

            txtDisplayName = new TextBox
            {
                Location = new Point(labelWidth + 15, gy),
                Size = new Size(controlWidth, 25),
                Text = _storage.DisplayName
            };
            grpMain.Controls.Add(txtDisplayName);
            gy += 35;

            // Путь к файлу
            Label lblFilePath = new Label
            {
                Text = "Путь к файлу (авто):",
                Location = new Point(10, gy),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            grpMain.Controls.Add(lblFilePath);

            txtFilePath = new TextBox
            {
                Location = new Point(labelWidth + 15, gy),
                Size = new Size(controlWidth, 25),
                ReadOnly = true,
                BackColor = Color.LightGray,
                Text = _storage.FilePath ?? ""  // будет заполнено автоматически
            };
            grpMain.Controls.Add(txtFilePath);

            // Тип сущности
            Label lblType = new Label
            {
                Text = "Тип сущности:",
                Location = new Point(10, gy),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            grpMain.Controls.Add(lblType);

            txtType = new TextBox
            {
                Location = new Point(labelWidth + 15, gy),
                Size = new Size(controlWidth, 25),
                Text = _storage.Type ?? "event"
            };
            grpMain.Controls.Add(txtType);

            y = grpMain.Bottom + 15;

            // ===== Поля хранилища =====
            GroupBox grpFields = new GroupBox
            {
                Text = "Поля хранилища",
                Location = new Point(margin, y),
                Size = new Size(this.ClientSize.Width - margin * 2, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(grpFields);

            // Подсказка
            Label lblHint = new Label
            {
                Text = "Добавьте поля, которые будут отображаться в форме добавления/редактирования событий.",
                Location = new Point(10, 20),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font(this.Font.FontFamily, 8)
            };
            grpFields.Controls.Add(lblHint);

            // DataGridView для полей
            dgvFields = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(grpFields.Width - 20, 190),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Колонки
            dgvFields.Columns.Add("Key", "Ключ (идентификатор)");
            dgvFields.Columns.Add("Display", "Отображаемое имя");
            dgvFields.Columns.Add("Type", "Тип данных");
            dgvFields.Columns.Add("Required", "Обязательное");

            // Настройка колонки Type
            var typeColumn = dgvFields.Columns["Type"] as DataGridViewComboBoxColumn;
            if (typeColumn == null)
            {
                dgvFields.Columns.Remove("Type");
                DataGridViewComboBoxColumn cmbType = new DataGridViewComboBoxColumn
                {
                    Name = "Type",
                    HeaderText = "Тип данных",
                    Items = { "string", "number", "date", "link" }
                };
                dgvFields.Columns.Insert(2, cmbType);
            }

            // Настройка колонки Required
            var requiredColumn = dgvFields.Columns["Required"] as DataGridViewCheckBoxColumn;
            if (requiredColumn == null)
            {
                dgvFields.Columns.Remove("Required");
                DataGridViewCheckBoxColumn chkRequired = new DataGridViewCheckBoxColumn
                {
                    Name = "Required",
                    HeaderText = "Обязательное",
                    TrueValue = true,
                    FalseValue = false
                };
                dgvFields.Columns.Insert(3, chkRequired);
            }

            grpFields.Controls.Add(dgvFields);

            // Кнопки управления полями
            Button btnAddField = new Button
            {
                Text = "➕ Добавить поле",
                Location = new Point(10, 245),
                Size = new Size(120, 25),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnAddField.Click += BtnAddField_Click;
            grpFields.Controls.Add(btnAddField);

            Button btnRemoveField = new Button
            {
                Text = "🗑️ Удалить поле",
                Location = new Point(140, 245),
                Size = new Size(120, 25),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveField.Click += BtnRemoveField_Click;
            grpFields.Controls.Add(btnRemoveField);

            y = grpFields.Bottom + 15;

            // ===== Кнопки Сохранить / Отмена =====
            btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(this.ClientSize.Width - 210, y),
                Size = new Size(100, 35),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(this.ClientSize.Width - 100, y),
                Size = new Size(90, 35),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
        }

        private void LoadData()
        {
            txtDisplayName.Text = _storage.DisplayName ?? "";
            txtFilePath.Text = _storage.FilePath ?? "Res/Data/";
            txtType.Text = _storage.Type ?? "event";

            dgvFields.Rows.Clear();
            foreach (var field in _fields)
            {
                dgvFields.Rows.Add(field.Key, field.Display, field.Type, field.Required);
            }
        }

        private void BtnAddField_Click(object sender, EventArgs e)
        {
            dgvFields.Rows.Add("new_field", "Новое поле", "string", false);
        }

        private void BtnRemoveField_Click(object sender, EventArgs e)
        {
            if (dgvFields.SelectedRows.Count > 0)
            {
                dgvFields.Rows.RemoveAt(dgvFields.SelectedRows[0].Index);
            }
            else if (dgvFields.SelectedCells.Count > 0 && dgvFields.SelectedCells[0].RowIndex >= 0)
            {
                dgvFields.Rows.RemoveAt(dgvFields.SelectedCells[0].RowIndex);
            }
            else
            {
                MessageBox.Show("Выберите поле для удаления", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDisplayName.Text))
            {
                MessageBox.Show("Введите отображаемое имя хранилища", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtType.Text))
            {
                MessageBox.Show("Укажите тип сущности (например, event)", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // АВТОМАТИЧЕСКИ ГЕНЕРИРУЕМ ПУТЬ
            string safeFileName = txtDisplayName.Text
                .Replace(" ", "_")
                .Replace("ё", "е")
                .Replace("Ё", "Е")
                + ".json";
            string autoPath = Path.Combine("Res", "Data", safeFileName);
            txtFilePath.Text = autoPath;

            // Собираем поля
            var fields = new List<StorageField>();
            foreach (DataGridViewRow row in dgvFields.Rows)
            {
                if (row.IsNewRow) continue;

                string key = row.Cells["Key"]?.Value?.ToString();
                string display = row.Cells["Display"]?.Value?.ToString();
                string type = row.Cells["Type"]?.Value?.ToString();
                bool required = row.Cells["Required"]?.Value is true;

                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("Ключ поля не может быть пустым", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                fields.Add(new StorageField
                {
                    Key = key,
                    Display = display ?? key,
                    Type = type ?? "string",
                    Required = required
                });
            }

            // Сохраняем
            _storage.DisplayName = txtDisplayName.Text;
            _storage.FilePath = autoPath;
            _storage.Type = txtType.Text;
            _storage.Fields = fields;

            if (_isEditMode)
                _configService.UpdateStorage(_storage);
            else
                _configService.AddStorage(_storage);

            // Создаём папку и файл данных
            _configService.CreateDataFileIfNotExists(autoPath);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}