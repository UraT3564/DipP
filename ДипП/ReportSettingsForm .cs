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
        private DateTime _selectedDate;
        private ReportCalculator _calculator = new ReportCalculator();

        public ReportSettingsForm(List<TemplateConfig> templates, DataRepository repository)
        {
            InitializeComponent();
            _templates = templates;
            _repository = repository;

            SetupControls();
        }

        private void SetupControls()
        {
            this.Text = "Настройки отчета";
            this.Size = new Size(850, 650); // шире для правой панели
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 10);
            this.MinimumSize = new Size(850, 650);

            int currentY = 10;
            int leftMargin = 10;
            int rightPanelX = 730; // позиция для правой панели

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
            if (_selectedTemplate == null)return;
            

            // Получаем все контролы
            var grid = this.Controls["gridPreview"] as DataGridView;
            if (grid == null)
            {
                MessageBox.Show("grid не найден");
                return;
            }

            try
            {
                // ЗАГРУЖАЕМ ДАННЫЕ В ГРИД
                var allEvents = _repository.GetByType("event");
                var events = allEvents.Where(e =>
                {
                    if (DateTime.TryParse(e.Date, out DateTime eventDate))
                    {
                        return eventDate.Year == _selectedDate.Year &&
                               eventDate.Month == _selectedDate.Month;
                    }
                    return false;
                }).ToList();

                var dt = new System.Data.DataTable();
                foreach (var col in _selectedTemplate.TableColumns)
                {
                    dt.Columns.Add(col);
                }

                foreach (var ev in events)
                {
                    var row = dt.NewRow();
                    foreach (var col in _selectedTemplate.TableColumns)
                    {
                        if (ev.Fields.ContainsKey(col))
                            row[col] = ev.Fields[col]?.ToString() ?? "";
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
            // Ищем GroupBox по имени
            var grpInfo = this.Controls["grpTemplateInfo"] as GroupBox;
            if (grpInfo == null) return;

            var lstProperties = grpInfo.Controls["lstProperties"] as ListBox;
            if (lstProperties == null) return;

            lstProperties.Items.Clear();

            if (_selectedTemplate == null) return;

            // Основная информация (не видна в таблице)
            lstProperties.Items.Add($"📄 {_selectedTemplate.DisplayName}");
            lstProperties.Items.Add($"Файл: {_selectedTemplate.TemplateFile}");
            lstProperties.Items.Add("──────────────");

            // 1. Есть ли автоматическая нумерация строк
            bool hasNumberColumn = _selectedTemplate.TableColumns?
                .Any(c => c.IndexOf("Number", StringComparison.OrdinalIgnoreCase) >= 0) ?? false;
            lstProperties.Items.Add($"🔢 Нумерация строк: {(hasNumberColumn ? "Да (авто)" : "Нет")}");

            // 2. Есть ли итоговая строка (не видна в预览, но будет в документе)
            bool hasSummary = _selectedTemplate.SummaryColumns != null &&
                             _selectedTemplate.SummaryColumns.Count > 0;
            lstProperties.Items.Add($"🧮 Итоговая строка: {(hasSummary ? "Да" : "Нет")}");

            // 3. Какие колонки будут суммироваться (если есть)
            if (hasSummary)
            {
                foreach (var col in _selectedTemplate.SummaryColumns)
                {
                    lstProperties.Items.Add($"   • {col}");
                }
            }

            // 4. Есть ли поля, которые не отображаются в таблице
            if (_selectedTemplate.Fields != null && _selectedTemplate.Fields.Count > 0)
            {
                lstProperties.Items.Add("──────────────");
                lstProperties.Items.Add($"📌 Поля шаблона (вне таблицы):");
                foreach (var field in _selectedTemplate.Fields)
                {
                    lstProperties.Items.Add($"   • {field}");
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
    }
}
