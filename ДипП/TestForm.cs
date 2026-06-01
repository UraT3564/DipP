using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;

namespace ДипП
{
    public partial class TestForm : Form
    {
        private ConfigService _configService;
        private DataRepository _dataRepository;
        private TemplateConfig _loadedConfig;
        private StorageConfig _currentStorageConfig;
        private List<StorageConfig> _allStorageConfigs;
        private DateTime _currentDate = DateTime.Now;

        // Элементы интерфейса
        private DataGridView gridEvents;
        private Button btnCreateReport;
        private Button btnTemplates;
        private Button btnSettings;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
        private Label lblMonthInfo;
        private SettingsService _settingsService;
        private string _organizationName;
        public TestForm()
        {
            InitializeComponent();
            _configService = new ConfigService();
            _dataRepository = new DataRepository();
            _dataRepository.Load();
            _settingsService = new SettingsService();
            _organizationName = _settingsService.GetOrganizationName();

            LoadConfiguration();
            SetupForm();
            LoadCurrentMonthData();
        }

        private void LoadConfiguration()
        {
            var configs = _configService.GetAllConfigs();
            if (configs.Count > 0) _loadedConfig = configs[0];

            // Загрузка всех конфигов хранилищ
            var storageConfigs = new StorageConfigService().LoadAll();
            _allStorageConfigs = storageConfigs;

            if (_allStorageConfigs != null && _allStorageConfigs.Count > 0)
            {
                _currentStorageConfig = _allStorageConfigs[0];
            }
            else
            {
                MessageBox.Show("Ошибка загрузки хранилища", "Error 1", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupForm()
        {
            this.Text = "Управление данными СДК";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);

            int currentY = 10;
            int margin = 15;

            // ===== ЗАГОЛОВОК =====
            lblMonthInfo = new Label
            {
                Text = $"События за {DateTime.Now.ToString("MMMM yyyy")} года",
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(margin, currentY),
                AutoSize = true
            };
            this.Controls.Add(lblMonthInfo);
            currentY += 30;

            // ===== ВЫБОР ХРАНИЛИЩА =====
            Label lblStorage = new Label
            {
                Text = "Хранилище:",
                Location = new Point(margin, currentY),
                AutoSize = true
            };
            this.Controls.Add(lblStorage);

            ComboBox cmbStorage = new ComboBox
            {
                Name = "cmbStorage",
                Location = new Point(margin + 80, currentY),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStorage.SelectedIndexChanged += CmbStorage_SelectedIndexChanged;
            this.Controls.Add(cmbStorage);
            currentY += 40;

            // ===== ТАБЛИЦА СОБЫТИЙ =====
            gridEvents = new DataGridView
            {
                Name = "gridEvents",
                Location = new Point(margin, currentY),
                Size = new Size(960, 400),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ScrollBars = ScrollBars.Both,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            gridEvents.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in gridEvents.Columns)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                }
            };
            this.Controls.Add(gridEvents);
            currentY += gridEvents.Height + 15;

            // ===== ОБЛАСТЬ КНОПОК (РАЗДЕЛЕННАЯ НА ДВЕ КОЛОНКИ) =====
            int panelWidth = 760;
            int panelHeight = 210;
            int halfWidth = (panelWidth - 10) / 2; // с отступом между колонками

            // Левая панель: Навигация
            GroupBox navGroup = new GroupBox
            {
                Text = "Навигация по модулям",
                Location = new Point(margin, currentY),
                Size = new Size(halfWidth, panelHeight),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            this.Controls.Add(navGroup);

            // Правая панель: Управление данными
            GroupBox dataGroup = new GroupBox
            {
                Text = "Управление данными",
                Location = new Point(margin + halfWidth + 10, currentY),
                Size = new Size(halfWidth, panelHeight),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            this.Controls.Add(dataGroup);

            // ===== КНОПКИ НАВИГАЦИИ (ЛЕВАЯ ПАНЕЛЬ) =====
            int buttonWidth = 140;
            int buttonHeight = 50;
            int spacing = 10;
            int startX = 10;
            int startY = 22;

            btnCreateReport = new Button
            {
                Text = "📊 Создать отчет",
                Location = new Point(startX, startY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnCreateReport.Click += BtnCreateReport_Click;
            navGroup.Controls.Add(btnCreateReport);

            btnTemplates = new Button
            {
                Text = "📄 Редактор шаблонов",
                Location = new Point(startX + buttonWidth + spacing, startY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnTemplates.Click += BtnTemplates_Click;
            navGroup.Controls.Add(btnTemplates);

            btnSettings = new Button
            {
                Text = "⚙️ Настройки",
                Location = new Point(startX, startY + buttonHeight + spacing),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnSettings.Click += BtnSettings_Click;
            navGroup.Controls.Add(btnSettings);

            Button btnHelp = new Button
            {
                Text = "❓ Справка",
                Location = new Point(startX + buttonWidth + spacing, startY + buttonHeight + spacing),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnHelp.Click += BtnHelp_Click;
            navGroup.Controls.Add(btnHelp);
            Button btnStorage = new Button
            {
                Text = "🗄️ Хранилища",
                Location = new Point(startX, startY + (buttonHeight + spacing) * 2), // третий ряд, левая колонка
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnStorage.Click += BtnStorageManager_Click;
            navGroup.Controls.Add(btnStorage);

            // ===== КНОПКИ УПРАВЛЕНИЯ ДАННЫМИ (ПРАВАЯ ПАНЕЛЬ) =====
            btnAdd = new Button
            {
                Text = "➕ Добавить",
                Location = new Point(startX, startY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnAdd.Click += BtnAdd_Click;
            dataGroup.Controls.Add(btnAdd);

            btnEdit = new Button
            {
                Text = "✏️ Редактировать",
                Location = new Point(startX + buttonWidth + spacing, startY),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnEdit.Click += BtnEdit_Click;
            dataGroup.Controls.Add(btnEdit);

            btnDelete = new Button
            {
                Text = "🗑️ Удалить",
                Location = new Point(startX, startY + buttonHeight + spacing),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.Click += BtnDelete_Click;
            dataGroup.Controls.Add(btnDelete);

            btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(startX + buttonWidth + spacing, startY + buttonHeight + spacing),
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = Color.LightYellow,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += BtnRefresh_Click;
            dataGroup.Controls.Add(btnRefresh);

            currentY += panelHeight + 15;

            // ===== КАЛЕНДАРЬ =====
            MonthCalendar monthCalendar = new MonthCalendar
            {
                Name = "monthCalendar",
                Location = new Point(810, currentY - buttonHeight * 3 - 48),
                MaxSelectionCount = 1,
                ShowToday = true,
                ShowTodayCircle = true,
                CalendarDimensions = new Size(1, 1),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            monthCalendar.DateSelected += MonthCalendar_DateSelected;
            this.Controls.Add(monthCalendar);

            this.Size = new Size(1000, currentY + monthCalendar.Size.Height - 115);
            InitStorageAndDate();
        }

        private void MonthCalendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            _currentDate = e.Start;
            LoadDataByDate(_currentDate);
        }
        private void LoadCurrentMonthData()
        {
            try
            {
                var now = DateTime.Now;
                var events = _dataRepository.GetByType(_currentStorageConfig.Type)
                    .Where(e =>
                    {
                        if (DateTime.TryParse(e.Date, out DateTime eventDate))
                        {
                            return eventDate.Year == now.Year && eventDate.Month == now.Month;
                        }
                        return false;
                    })
                    .ToList();

                DisplayEvents(events);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}","Error 2");
            }
        }
        // Обработчики событий (заглушки)
        private void BtnCreateReport_Click(object sender, EventArgs e)
        {
            var settingsForm = new ReportSettingsForm(_configService.GetAllConfigs(), _dataRepository);

            // Подписываемся на событие генерации
            settingsForm.ReportGenerated += (s, args) =>
            {
                var form = s as ReportSettingsForm;
                GenerateReport(form.SelectedTemplate, form.SelectedDate);
            };

            settingsForm.ShowDialog();  // Показываем как диалог, но не закрываем программно
        }

        private void BtnTemplates_Click(object sender, EventArgs e)
        {
            var manager = new TemplateManagerForm();
            manager.ShowDialog();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(_settingsService, _organizationName);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                _organizationName = _settingsService.GetOrganizationName();
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var editForm = new EventEditForm(_dataRepository, _currentStorageConfig);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                _dataRepository.Add(editForm.GetEvent());
                LoadCurrentMonthData();
                MessageBox.Show("Запись добавлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridEvents.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedIndex = gridEvents.SelectedRows[0].Index;
            string eventId = GetEventIdFromSelectedRow();

            var targetEvent = _dataRepository.GetByType("event").FirstOrDefault(ev => ev.Id == eventId);
            if (targetEvent == null)
            {
                MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var editForm = new EventEditForm(_dataRepository, _currentStorageConfig, targetEvent);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                _dataRepository.Update(editForm.GetEvent());
                LoadCurrentMonthData();
                NavigateToRow(selectedIndex);
                MessageBox.Show("Запись обновлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridEvents.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем Id через универсальный метод
            string eventId = GetEventIdFromSelectedRow();

            // Если Id не нашли (старый метод), пробуем найти по обязательному полю
            if (string.IsNullOrEmpty(eventId))
            {
                var firstRequiredField = _currentStorageConfig.Fields.FirstOrDefault(f => f.Required);
                if (firstRequiredField == null)
                {
                    MessageBox.Show("Не удалось идентифицировать запись", "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string fieldKey = firstRequiredField.Key;
                string fieldValue = gridEvents.SelectedRows[0].Cells[fieldKey]?.Value?.ToString();
                string eventDate = gridEvents.SelectedRows[0].Cells["Date"]?.Value?.ToString();

                var events = _dataRepository.GetByType(_currentStorageConfig.Type);
                var target = events.FirstOrDefault(ev =>
                    ev.Date == eventDate &&
                    ev.Fields.ContainsKey(fieldKey) &&
                    ev.Fields[fieldKey]?.ToString() == fieldValue);

                eventId = target?.Id;
            }

            if (string.IsNullOrEmpty(eventId))
            {
                MessageBox.Show("Запись не найдена", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                                          MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _dataRepository.Delete(eventId);
                LoadDataByDate(_currentDate);
                MessageBox.Show("Запись удалена", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            _dataRepository.Load();
            LoadCurrentMonthData();
        }

        private void BtnStorageManager_Click(object sender, EventArgs e)
        {
            var storageConfigService = new StorageConfigService();
            var managerForm = new StorageManagerForm(storageConfigService, _dataRepository);
            managerForm.ShowDialog();

            // После закрытия формы обновляем список хранилищ в комбобоксе
            var storageConfigs = storageConfigService.LoadAll();
            _allStorageConfigs = storageConfigs;

            var cmb = this.Controls["cmbStorage"] as ComboBox;
            if (cmb != null && _allStorageConfigs != null)
            {
                cmb.DataSource = null;
                cmb.DataSource = _allStorageConfigs;
                cmb.DisplayMember = "DisplayName";
                cmb.ValueMember = "Id";
                if (cmb.Items.Count > 0 && _currentStorageConfig != null)
                {
                    cmb.SelectedItem = _currentStorageConfig;
                }
            }
        }
        
        private void GenerateReport(TemplateConfig template, DateTime date)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Определяем диапазон дат
                DateTime startDate, endDate;
                switch (template.PeriodType)
                {
                    case "day":
                        startDate = date.Date;
                        endDate = date.Date;
                        break;
                    case "month":
                        startDate = new DateTime(date.Year, date.Month, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                        break;
                    case "year":
                        startDate = new DateTime(date.Year, 1, 1);
                        endDate = new DateTime(date.Year, 12, 31);
                        break;
                    default:
                        startDate = new DateTime(date.Year, date.Month, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                        break;
                }

                // Загружаем хранилище
                var storageConfigs = new StorageConfigService().LoadAll();
                var targetStorage = storageConfigs.FirstOrDefault(s => s.Id == template.StorageId);

                if (targetStorage == null)
                {
                    MessageBox.Show($"Для шаблона \"{template.DisplayName}\" не указано хранилище данных",
                                   "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var repo = new DataRepository(targetStorage.FilePath);
                repo.Load();

                // 1. ПОЛУЧАЕМ СОБЫТИЯ ЗА ПЕРИОД
                var events = repo.GetByType(targetStorage.Type)
                    .Where(e =>
                    {
                        if (DateTime.TryParse(e.Date, out DateTime eventDate))
                            return eventDate >= startDate && eventDate <= endDate;
                        return false;
                    })
                    .ToList();

                // 2. ФОРМИРУЕМ ТЕКСТОВЫЕ ПОЛЯ (ОДИН ЦИКЛ)
                var textFields = new Dictionary<string, string>();

                foreach (var mapping in template.TextFieldMappings)
                {
                    string markerKey = mapping.Key;
                    var fieldMapping = mapping.Value;
                    string value = "";

                    if (fieldMapping.IsStatic)
                    {
                        value = fieldMapping.StaticValue ?? "";
                    }
                    else if (fieldMapping.IsAggregate)
                    {
                        if (fieldMapping.AggregateType == "OrganizationName")
                        {
                            value = _organizationName;
                            Console.WriteLine($"OrganizationName = {value}");
                        }
                        else if (fieldMapping.AggregateType == "AutoNumber")
                        {
                            continue;
                        }
                    }
                    else if (!string.IsNullOrEmpty(fieldMapping.StorageField))
                    {
                        string storageField = fieldMapping.StorageField;

                        if (storageField == "Month")
                        {
                            value = date.ToString("MMMM");
                        }
                        else if (storageField == "Year")
                        {
                            value = date.Year.ToString();
                        }
                        else if (events.Count > 0 && events[0].Fields.ContainsKey(storageField))
                        {
                            value = events[0].Fields[storageField]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(fieldMapping.DateFormat))
                            {
                                value = DateHelper.FormatDateValue(value, fieldMapping.DateFormat);
                            }
                        }
                    }

                    textFields[markerKey] = value;
                    Console.WriteLine($"Добавлено: {markerKey} = {value}");
                }

                // 3. ФОРМИРУЕМ ТАБЛИЧНЫЕ ДАННЫЕ
                var tableData = new List<Dictionary<string, string>>();

                foreach (var ev in events)
                {
                    var row = new Dictionary<string, string>();
                    foreach (var mapping in template.TableFieldMappings)
                    {
                        string markerKey = mapping.Key;
                        var fieldMapping = mapping.Value;

                        if (fieldMapping.IsStatic)
                        {
                            row[markerKey] = fieldMapping.StaticValue ?? "";
                        }
                        else if (!string.IsNullOrEmpty(fieldMapping.StorageField))
                        {
                            string storageField = fieldMapping.StorageField;
                            if (ev.Fields.ContainsKey(storageField))
                            {
                                string value = ev.Fields[storageField]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(fieldMapping.DateFormat))
                                {
                                    value = DateHelper.FormatDateValue(value, fieldMapping.DateFormat);
                                }
                                row[markerKey] = value;
                            }
                            else
                            {
                                row[markerKey] = "";
                            }
                        }
                        else
                        {
                            row[markerKey] = "";
                        }
                    }
                    tableData.Add(row);
                }

                var docService = new DocumentService();
                string tempOutputPath = docService.GenerateReport(template, textFields, tableData, date);

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить отчет",
                    Filter = "Документ Word (*.docx)|*.docx",
                    FileName = $"{template.DisplayName}_{date:yyyy-MM-dd}.docx",
                    DefaultExt = "docx"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(tempOutputPath, saveFileDialog.FileName, true);
                    File.Delete(tempOutputPath);
                    MessageBox.Show($"Отчет сохранен:\n{saveFileDialog.FileName}", "Успех",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчета:\n{ex.Message}", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        
        private void DisplayEvents(List<DataEntity> events)
        {
            if (events == null || events.Count == 0)
            {
                gridEvents.DataSource = null;
                lblMonthInfo.Text = $"События за {DateTime.Now.ToString("MMMM yyyy")} года (нет данных)";
                return;
            }

            var dt = new System.Data.DataTable();

            // Колонка Date всегда первая
            dt.Columns.Add("Date", typeof(string));

            // Добавляем колонки из конфига хранилища
            foreach (var field in _currentStorageConfig.Fields)
            {
                dt.Columns.Add(field.Key, typeof(string));
            }

            // Заполняем строки
            foreach (var ev in events)
            {
                var row = dt.NewRow();

                // Дата
                row["Date"] = ev.Date;

                // Остальные поля по ключам из конфига
                foreach (var field in _currentStorageConfig.Fields)
                {
                    if (ev.Fields.ContainsKey(field.Key))
                        row[field.Key] = ev.Fields[field.Key]?.ToString() ?? "";
                }

                dt.Rows.Add(row);
            }

            // Назначаем источник данных
            gridEvents.DataSource = dt;

            // Настраиваем заголовки колонок (русские названия)
            foreach (DataGridViewColumn col in gridEvents.Columns)
            {
                if (col.Name == "Date")
                {
                    col.HeaderText = "Дата";
                }
                else
                {
                    var field = _currentStorageConfig.Fields.FirstOrDefault(f => f.Key == col.Name);
                    col.HeaderText = field?.Display ?? col.Name;
                }
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            }

            // Обновляем заголовок
            lblMonthInfo.Text = $"События за {DateTime.Now.ToString("MMMM yyyy")} года (всего: {events.Count})";
        }

        private string GetEventIdFromSelectedRow()
        {
            if (gridEvents.SelectedRows.Count == 0) return null;

            // Получаем Id из скрытой колонки (если добавили)
            if (gridEvents.Columns.Contains("Id") && gridEvents.Columns["Id"].Visible == false)
            {
                return gridEvents.SelectedRows[0].Cells["Id"]?.Value?.ToString();
            }

            // Резервный вариант — ищем по первому обязательному полю из конфига
            var firstRequiredField = _currentStorageConfig.Fields.FirstOrDefault(f => f.Required);
            if (firstRequiredField == null) return null;

            string fieldKey = firstRequiredField.Key;
            string fieldValue = gridEvents.SelectedRows[0].Cells[fieldKey]?.Value?.ToString();
            string eventDate = gridEvents.SelectedRows[0].Cells["Date"]?.Value?.ToString();

            var events = _dataRepository.GetByType(_currentStorageConfig.Type);
            var target = events.FirstOrDefault(ev =>
                ev.Date == eventDate &&
                ev.Fields.ContainsKey(fieldKey) &&
                ev.Fields[fieldKey]?.ToString() == fieldValue);

            return target?.Id;
        }

        private void NavigateToRow(int rowIndex)
        {
            if (gridEvents.Rows.Count == 0) return;
            if (rowIndex < 0) rowIndex = 0;
            if (rowIndex >= gridEvents.Rows.Count) rowIndex = gridEvents.Rows.Count - 1;

            gridEvents.ClearSelection();
            gridEvents.Rows[rowIndex].Selected = true;
            gridEvents.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            try
            {
                string helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Manual.pdf");

                if (File.Exists(helpPath))
                {
                    System.Diagnostics.Process.Start(helpPath);
                }
                else
                {
                    // Если PDF не найден, показываем сообщение с краткой справкой
                    string helpText = "КРАТКОЕ РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ\n\n" +
                                      "1. ПРОСМОТР СОБЫТИЙ\n" +
                                      "   • На главной форме отображаются события за текущий месяц\n" +
                                      "   • Используйте кнопку 'Обновить' для перезагрузки данных\n\n" +
                                      "2. СОЗДАНИЕ ОТЧЕТА\n" +
                                      "   • Нажмите 'Создать отчет'\n" +
                                      "   • Выберите шаблон и период\n" +
                                      "   • Просмотрите данные в предпросмотре\n" +
                                      "   • Нажмите 'Создать отчет' для генерации\n\n" +
                                      "3. УПРАВЛЕНИЕ ДАННЫМИ\n" +
                                      "   • 'Добавить' - новое мероприятие\n" +
                                      "   • 'Редактировать' - изменение выбранного\n" +
                                      "   • 'Удалить' - удаление выбранного\n\n" +
                                      "4. РАБОТА С ШАБЛОНАМИ\n" +
                                      "   • 'Редактор шаблонов' - создание новых шаблонов\n\n" +
                                      "Подробная информация содержится в файле 'Руководство пользователя.pdf'\n" +
                                      "в папке Res приложения.";

                    MessageBox.Show(helpText, "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии справки: {ex.Message}", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitStorageAndDate()
        {
            var cmb = this.Controls["cmbStorage"] as ComboBox;
            if (cmb != null && _allStorageConfigs != null)
            {
                cmb.DataSource = _allStorageConfigs;
                cmb.DisplayMember = "DisplayName";
                cmb.ValueMember = "Id";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            }

            var dtp = this.Controls["dtpPeriod"] as DateTimePicker;
            if (dtp != null)
            {
                dtp.Value = DateTime.Now;
            }
        }

        private void CmbStorage_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cmb = sender as ComboBox;
            _currentStorageConfig = cmb.SelectedItem as StorageConfig;

            if (_currentStorageConfig != null)
            {
                // Перезагружаем DataRepository с новым файлом
                _dataRepository = new DataRepository(_currentStorageConfig.FilePath);
                _dataRepository.Load();

                // Загружаем данные за выбранную дату
                LoadDataByDate(_currentDate);
            }
        }

        private void LoadDataByDate(DateTime date)
        {
            try
            {
                var events = _dataRepository.GetByType(_currentStorageConfig.Type)
                    .Where(e =>
                    {
                        if (DateTime.TryParse(e.Date, out DateTime eventDate))
                        {
                            return eventDate.Year == date.Year && eventDate.Month == date.Month;
                        }
                        return false;
                    })
                    .ToList();

                DisplayEvents(events);
                lblMonthInfo.Text = $"События за {date:MMMM yyyy} года (всего: {events.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}