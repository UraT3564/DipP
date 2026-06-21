using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public partial class TemplateManagerForm : Form
    {
        private ConfigService _configService;
        private StorageConfigService _storageConfigService;
        private List<TemplateConfig> _templates;
        private List<StorageConfig> _storages;

        private ListView lvTemplates;
        private Button btnCreate;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnClose;
        private Label lblStatus;

        public TemplateManagerForm()
        {
            InitializeComponent();
            _configService = new ConfigService();
            _storageConfigService = new StorageConfigService();

            LoadData();
            SetupForm();
        }

        private void InitializeComponent()
        {
			this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
			this.MaximizeBox = false;
			this.Text = "Управление шаблонами отчетов";
            this.Size = new Size(550, 410 );
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(500, 300);
        }

        private void LoadData()
        {
            try
            {
                _templates = _configService.GetAllConfigs();
                _storages = _storageConfigService.LoadAll();

                // Защита от null
                if (_templates == null)
                    _templates = new List<TemplateConfig>();
                if (_storages == null)
                    _storages = new List<StorageConfig>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                _templates = new List<TemplateConfig>();
                _storages = new List<StorageConfig>();
            }
        }

        private void SetupForm()
        {
            int margin = 12;
            int currentY = 10;

            this.Icon = new Icon(System.IO.Path.Combine(Application.StartupPath, "Res\\icons8.ico"));
            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Список доступных шаблонов отчетов:",
                Location = new Point(margin, currentY),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);
            currentY += 30;

            // ListView для списка шаблонов
            lvTemplates = new ListView
            {
                Location = new Point(margin, currentY),
                Size = new Size(this.ClientSize.Width - margin * 2, 250),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            lvTemplates.Columns.Add("Название", 250);
            lvTemplates.Columns.Add("Файл шаблона", 150);
            lvTemplates.Columns.Add("Хранилище", 120);
            lvTemplates.DoubleClick += (s, e) => EditSelectedTemplate();


            this.Controls.Add(lvTemplates);
            currentY += lvTemplates.Height + 15;

            // Кнопки
            int buttonWidth = 110;
            int buttonHeight = 35;
            int spacing = 10;

            btnCreate = new Button
            {
                Text = "➕ Создать",
                Location = new Point(margin, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnCreate.Click += BtnCreate_Click;

            btnEdit = new Button
            {
                Text = "✏️ Редактировать",
                Location = new Point(margin + buttonWidth + spacing, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnEdit.Click += (s, e) => EditSelectedTemplate();

            btnDelete = new Button
            {
                Text = "🗑️ Удалить",
                Location = new Point(margin + (buttonWidth + spacing) * 2, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(this.ClientSize.Width - margin - buttonWidth, currentY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnCreate);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnClose);
            currentY += buttonHeight + 10;

            // Статус
            lblStatus = new Label
            {
                Text = $"Всего шаблонов: {_templates.Count}",
                Location = new Point(margin, currentY),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblStatus);

            // Событие выбора в списке
            lvTemplates.SelectedIndexChanged += (s, e) =>
            {
                bool hasSelection = lvTemplates.SelectedItems.Count > 0;
                btnEdit.Enabled = hasSelection;
                btnDelete.Enabled = hasSelection;
            };
            RefreshTemplateList();
        }

        private void RefreshTemplateList()
        {
            lvTemplates.Items.Clear();

            foreach (TemplateConfig template in _templates)
            {
                string storageName = "—";
                if (!string.IsNullOrEmpty(template.StorageId))
                {
                    var storage = _storages.FirstOrDefault(s => s.Id == template.StorageId);
                    if (storage != null)
                        storageName = storage.DisplayName;
                    else
                        storageName = "[не найдено]";
                }

                var item = new ListViewItem(template.DisplayName);
                item.SubItems.Add(template.TemplateFile);
                item.SubItems.Add(storageName);
                item.Tag = template;
                lvTemplates.Items.Add(item);
            }

            this.lblStatus.Text = $"Всего шаблонов: {_templates.Count}";
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            var editor = new TemplateWizardForm(_configService, _storageConfigService);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                RefreshTemplateList();
            }
        }

        private void EditSelectedTemplate()
        {
            if (lvTemplates.SelectedItems.Count == 0) return;

            var template = lvTemplates.SelectedItems[0].Tag as TemplateConfig;
            if (template == null) return;

            var editor = new TemplateWizardForm(_configService, _storageConfigService, template);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                LoadData();
                RefreshTemplateList();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lvTemplates.SelectedItems.Count == 0) return;

            var template = lvTemplates.SelectedItems[0].Tag as TemplateConfig;
            if (template == null) return;

            var result = MessageBox.Show($"Удалить шаблон \"{template.DisplayName}\"?\n\nФайл шаблона не будет удален, только ссылка на него.",
                                         "Подтверждение удаления",
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _templates.Remove(template);
                _configService.SaveAllConfigs(_templates);
                LoadData();
                RefreshTemplateList();
                MessageBox.Show("Шаблон удален", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}