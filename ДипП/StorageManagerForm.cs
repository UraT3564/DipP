using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public partial class StorageManagerForm : Form
    {
        private StorageConfigService _configService;
        private DataRepository _dataRepository;
        private ListBox lbStorages;
        private DataGridView dgvFields;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnClose;
        private Label lblStorageInfo;
        private StorageConfig _selectedStorage;

        public StorageManagerForm(StorageConfigService configService, DataRepository dataRepository)
        {
            _configService = configService;
            _dataRepository = dataRepository;
            InitializeComponent();
            SetupForm();
            LoadStorages();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
            this.MaximizeBox = false;
            this.Text = "Управление хранилищами";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            //this.MinimumSize = new Size(700, 500);
        }

        private void SetupForm()
        {
            int margin = 15;
            int currentY = 15;

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Список хранилищ данных",
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(margin, currentY),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);
            currentY += 30;

            // Список хранилищ
            lbStorages = new ListBox
            {
                Location = new Point(margin, currentY),
                Size = new Size(200, 300),
                Font = new Font("Consolas", 10)
            };
            lbStorages.SelectedIndexChanged += LbStorages_SelectedIndexChanged;
            this.Controls.Add(lbStorages);

            // Информационная панель справа
            GroupBox grpInfo = new GroupBox
            {
                Text = "Информация о хранилище",
                Location = new Point(margin + 220, currentY),
                Size = new Size(440, 300)
            };
            this.Controls.Add(grpInfo);

            lblStorageInfo = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(420, 100),
                Font = new Font("Consolas", 9)
            };
            grpInfo.Controls.Add(lblStorageInfo);

            // Поля хранилища
            Label lblFields = new Label
            {
                Text = "Поля хранилища:",
                Location = new Point(10, 130),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            grpInfo.Controls.Add(lblFields);

            dgvFields = new DataGridView
            {
                Location = new Point(10, 155),
                Size = new Size(420, 130),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            dgvFields.Columns.Add("Key", "Ключ");
            dgvFields.Columns.Add("Display", "Отображаемое имя");
            dgvFields.Columns.Add("Type", "Тип");
            dgvFields.Columns.Add("Required", "Обязательное");
            grpInfo.Controls.Add(dgvFields);

            currentY += 320;

            // Кнопки управления
            int buttonWidth = 140;
            int buttonHeight = 35;
            int spacing = 10;

            btnAdd = new Button
            {
                Text = "➕ Добавить",
                Location = new Point(margin, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            btnEdit = new Button
            {
                Text = "✏️ Редактировать",
                Location = new Point(margin + buttonWidth + spacing, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnEdit.Click += BtnEdit_Click;
            this.Controls.Add(btnEdit);

            btnDelete = new Button
            {
                Text = "🗑️ Удалить",
                Location = new Point(margin + (buttonWidth + spacing) * 2, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(margin + (buttonWidth + spacing) * 3, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            currentY += 105;
            this.Size = new Size(700, currentY);
            this.MinimumSize = new Size(700, currentY);
        }

        private void LoadStorages()
        {
            var storages = _configService.LoadAll();
            lbStorages.Items.Clear();

            foreach (var storage in storages)
            {
                string display = $"{storage.DisplayName} ({storage.Type})";
                lbStorages.Items.Add(new StorageItem { Config = storage, DisplayText = display });
            }

            if (lbStorages.Items.Count > 0)
                lbStorages.SelectedIndex = 0;
        }

        private void LbStorages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbStorages.SelectedItem == null)
            {
                _selectedStorage = null;
                lblStorageInfo.Text = "";
                dgvFields.Rows.Clear();
                return;
            }

            var item = lbStorages.SelectedItem as StorageItem;
            _selectedStorage = item?.Config;

            if (_selectedStorage != null)
            {
                lblStorageInfo.Text = $"ID: {_selectedStorage.Id}\n" +
                                      $"Название: {_selectedStorage.DisplayName}\n" +
                                      $"Путь: {_selectedStorage.FilePath}\n" +
                                      $"Тип: {_selectedStorage.Type}\n" +
                                      $"Полей: {_selectedStorage.Fields?.Count ?? 0}";

                dgvFields.Rows.Clear();
                if (_selectedStorage.Fields != null)
                {
                    foreach (var field in _selectedStorage.Fields)
                    {
                        dgvFields.Rows.Add(field.Key, field.Display, field.Type, field.Required ? "Да" : "Нет");
                    }
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var editForm = new StorageEditForm(_configService);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadStorages();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedStorage == null)
            {
                MessageBox.Show("Выберите хранилище для редактирования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var editForm = new StorageEditForm(_configService, _selectedStorage);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadStorages();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedStorage == null)
            {
                MessageBox.Show("Выберите хранилище для удаления", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить хранилище \"{_selectedStorage.DisplayName}\"?\n\n" +
                "ВНИМАНИЕ: Файл данных НЕ будет удален. Вы сможете подключить его позже.\n" +
                "Удалить запись о хранилище?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _configService.DeleteStorage(_selectedStorage.Id);
                LoadStorages();
                MessageBox.Show("Хранилище удалено из конфигурации", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Вспомогательный класс для хранения в ListBox
        private class StorageItem
        {
            public StorageConfig Config { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() => DisplayText;
        }
    }
}