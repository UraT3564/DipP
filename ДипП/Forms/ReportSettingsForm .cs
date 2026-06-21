using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public partial class ReportSettingsForm : Form
    {
        private List<TemplateConfig> _templates;
        private DataRepository _repository;
        private TemplateConfig _selectedTemplate;
        private StorageConfig _selectedStorage;
        private StorageConfigService _storageConfigService;
        private DateTime _selectedDate;
        private ReportCalculator _calculator = new ReportCalculator();

        public ReportSettingsForm(List<TemplateConfig> templates, DataRepository repository)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
            this.MaximizeBox = false;
            InitializeComponent();
            _templates = templates;
            _repository = repository;
            _storageConfigService = new StorageConfigService();
            SetupForm();
        }

        private void SetupForm()
        {
            this.Text = "Настройки отчета";
            this.Size = new Size(850, 650); // шире для правой панели
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 10);
            this.MinimumSize = new Size(850, 650);

            int currentY = 10;
            int leftMargin = 10;
            int rightPanelX = 730; // позиция для правой панели
            this.Icon = new Icon(System.IO.Path.Combine(Application.StartupPath, "Res\\icons8.ico"));

            // ===== ВЕРХНЯЯ ПАНЕЛЬ =====
            Label lblTemplate = new Label
            {
                Text = "Шаблон:",
                Location = new Point(leftMargin, currentY + 5),
                AutoSize = true
            };

            ComboBox cmbTemplate = new ComboBox
            {
                Name = "cmbTemplate",
                Location = new Point(100, currentY),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTemplate.DataSource = _templates;
            cmbTemplate.DisplayMember = "DisplayName";
            cmbTemplate.ValueMember = "Id";
            cmbTemplate.SelectedIndexChanged += (s, e) =>
            {
                _selectedTemplate = cmbTemplate.SelectedItem as TemplateConfig;
                LoadPropertiesData();
                LoadStorageForTemplate();
                LoadPreviewData();
            };

            this.Controls.Add(lblTemplate);
            this.Controls.Add(cmbTemplate);

            currentY += 35;

            // ===== КАЛЕНДАРЬ =====
            Label lblDate = new Label
            {
                Text = "Период:",
                Location = new Point(leftMargin, currentY + 5),
                AutoSize = true
            };

            MonthCalendar monthCalendar = new MonthCalendar
            {
                Name = "monthCalendar",
                Location = new Point(100, currentY),
                MaxSelectionCount = 1,
                ShowToday = true,
                ShowTodayCircle = true,
                CalendarDimensions = new Size(1, 1)
            };
            monthCalendar.DateSelected += (s, e) =>
            {
                _selectedDate = e.Start;
                LoadPreviewData();
            };

            this.Controls.Add(lblDate);
            this.Controls.Add(monthCalendar);

            currentY += monthCalendar.Height + 20;

            // ===== ЗАГОЛОВОК ПРЕДПРОСМОТРА =====
            Label lblPreview = new Label
            {
                Text = "Предпросмотр данных:",
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                AutoSize = true
            };
            this.Controls.Add(lblPreview);

            currentY += 25;

            // ===== ГРИД СЛЕВА =====
            DataGridView gridPreview = new DataGridView
            {
                Name = "gridPreview",
                Location = new Point(leftMargin, currentY),
                Size = new Size(700, 350), // ширина 700, оставляем место для правой панели
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            this.Controls.Add(gridPreview);

            // ===== ПРАВАЯ ПАНЕЛЬ =====
            // Панель с информацией о шаблоне
            GroupBox grpTemplateInfo = new GroupBox
            {
                Name = "grpTemplateInfo",
                Text = "Информация о шаблоне",
                Location = new Point(rightPanelX, currentY),
                Size = new Size(235, 350),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            ListBox lstProperties = new ListBox
            {
                Name = "lstProperties",
                Location = new Point(10, 25),
                Size = new Size(215, 315),
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 9),
                IntegralHeight = false
            };
            grpTemplateInfo.Controls.Add(lstProperties);
            this.Controls.Add(grpTemplateInfo);

            // Обновляем currentY для кнопок (после грида)
            currentY += gridPreview.Height + 20;

            // ===== КНОПКИ =====
            Button btnGenerate = new Button
            {
                Text = "Создать отчет",
                Location = new Point(640, currentY),
                Size = new Size(150, 40),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            btnGenerate.Click += BtnGenerate_Click;

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(810, currentY),
                Size = new Size(150, 40),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(btnGenerate);
            this.Controls.Add(btnCancel);

            // ===== УСТАНАВЛИВАЕМ НАЧАЛЬНЫЕ ЗНАЧЕНИЯ =====
            if (_templates != null && _templates.Count > 0)
            {
                _selectedTemplate = _templates[0]; // вызовет LoadPreviewData через событие
                LoadStorageForTemplate();
            }

            _selectedDate = DateTime.Now;
            monthCalendar.SetDate(_selectedDate);

            // Явно вызываем загрузку данных, если не сработало событие
            if (_templates != null && _templates.Count > 0)
            {
                LoadPreviewData();
                LoadPropertiesData();
            }
        }
        private void LoadPreviewData()
        {
            if (_selectedTemplate == null) return;

            var grid = this.Controls["gridPreview"] as DataGridView;
            if (grid == null)
            {
                MessageBox.Show("grid не найден");
                return;
            }

            try
            {
                // Получаем тип хранилища из шаблона
                string storageType = _selectedStorage?.Type ?? "event";

                var allEvents = _repository.GetByType(storageType);
                var events = allEvents.Where(e =>
                {
                    if (DateTime.TryParse(e.Date, out DateTime eventDate))
                    {
                        return eventDate.Year == _selectedDate.Year &&
                               eventDate.Month == _selectedDate.Month;
                    }
                    return false;
                }).ToList();

                var dt = new DataTable();

                // Добавляем колонки из маппинга таблицы (используем ключи маппинга)
                var displayColumns = _selectedTemplate.TableFieldMappings.Keys.ToList();
                foreach (var col in displayColumns)
                {
                    dt.Columns.Add(col);
                }

                foreach (var ev in events)
                {
                    var row = dt.NewRow();
                    foreach (var mapping in _selectedTemplate.TableFieldMappings)
                    {
                        string markerName = mapping.Key;
                        string storageField = mapping.Value.StorageFieldId;

                        if (!string.IsNullOrEmpty(storageField) && ev.Fields.ContainsKey(storageField))
                        {
                            string value = ev.Fields[storageField]?.ToString() ?? "";

                            // Применяем формат даты если нужно
                            if (mapping.Value.DateFormat != null && !string.IsNullOrEmpty(value))
                            {
                                value = DateHelper.FormatDateValue(value, mapping.Value.DateFormat);
                            }

                            row[markerName] = value;
                        }
                        else
                        {
                            row[markerName] = "";
                        }
                    }
                    dt.Rows.Add(row);
                }

                grid.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadPropertiesData()
        {
            var grpInfo = this.Controls["grpTemplateInfo"] as GroupBox;
            if (grpInfo == null) return;

            var lstProperties = grpInfo.Controls["lstProperties"] as ListBox;
            if (lstProperties == null) return;

            lstProperties.Items.Clear();

            if (_selectedTemplate == null) return;

            lstProperties.Items.Add($"📄 {_selectedTemplate.DisplayName}");
            lstProperties.Items.Add($"Файл: {_selectedTemplate.TemplateFile}");
            lstProperties.Items.Add("──────────────");

            // Информация о хранилище
            if (_selectedStorage != null)
            {
                lstProperties.Items.Add($"🗄️ Хранилище: {_selectedStorage.DisplayName}");
            }
            else
            {
                lstProperties.Items.Add($"⚠️ Хранилище не указано");
            }
            lstProperties.Items.Add("──────────────");

            // Поля в тексте
            int textFieldsCount = _selectedTemplate.TextFieldMappings?.Count ?? 0;
            lstProperties.Items.Add($"📝 Поля в тексте ({textFieldsCount}):");
            foreach (var field in _selectedTemplate.TextFieldMappings ?? new Dictionary<string, FieldMapping>())
            {
                string marker = field.Key;
                var mapping = field.Value;

                if (mapping.IsStatic)
                {
                    lstProperties.Items.Add($"   • {marker} → (статическое: {mapping.StaticValue})");
                }
                else
                {
                    string dateInfo = string.IsNullOrEmpty(mapping.DateFormat) ? "" : $" [{mapping.DateFormat}]";
                    lstProperties.Items.Add($"   • {marker} → {mapping.StorageFieldId}{dateInfo}");
                }
            }
            lstProperties.Items.Add("──────────────");

            // Поля в таблице
            int tableFieldsCount = _selectedTemplate.TableFieldMappings?.Count ?? 0;
            lstProperties.Items.Add($"📊 Поля в таблице ({tableFieldsCount}):");
            foreach (var field in _selectedTemplate.TableFieldMappings ?? new Dictionary<string, FieldMapping>())
            {
                string marker = field.Key;
                var mapping = field.Value;
                bool isAutoNumber = _selectedTemplate.AutoNumberMarker == marker;

                if (isAutoNumber)
                {
                    lstProperties.Items.Add($"   🔢 {marker} → (автонумерация)");
                }
                else
                {
                    string dateInfo = string.IsNullOrEmpty(mapping.DateFormat) ? "" : $" [{mapping.DateFormat}]";
                    lstProperties.Items.Add($"   • {marker} → {mapping.StorageFieldId}{dateInfo}");
                }
            }
        }


        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (_selectedTemplate == null)
            {
                MessageBox.Show("Выберите шаблон!");
                return;
            }

            // НЕ закрываем форму, просто сигнализируем о создании отчета
            OnReportGenerated(EventArgs.Empty);
        }

        // Событие для передачи данных в TestForm
        public event EventHandler ReportGenerated;

        protected virtual void OnReportGenerated(EventArgs e)
        {
            ReportGenerated?.Invoke(this, e);
        }
        

        // Свойства для доступа к выбранным значениям
        public TemplateConfig SelectedTemplate => _selectedTemplate;
        public DateTime SelectedDate => _selectedDate;

        private void LoadStorageForTemplate()
        {
            if (_selectedTemplate == null || string.IsNullOrEmpty(_selectedTemplate.StorageId))
            {
                _selectedStorage = null;
                return;
            }

            var storages = _storageConfigService.LoadAll();
            _selectedStorage = storages.FirstOrDefault(s => s.Id == _selectedTemplate.StorageId);
        }
    }
}
