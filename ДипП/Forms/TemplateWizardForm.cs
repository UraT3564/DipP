using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ДипП.Models;
using ДипП.Services;
using Microsoft.VisualBasic;

namespace ДипП
{
    public partial class TemplateWizardForm : Form
    {
        private ConfigService _configService;
        private StorageConfigService _storageConfigService;
        private List<TemplateConfig> _existingConfigs;
        private List<StorageConfig> _availableStorages;

        private bool _isEditMode;
        private TemplateConfig _editingTemplate;

        // Данные из документа
        private string _selectedFilePath;
        private string _originalFilePath;
        private List<TemplateMarkerInfo> _markers;
        private StorageConfig _selectedStorage;
        private List<TableInfo> _foundTables;
        private string _defaultOrganizationName;

        // Сохранение настроек маркеров
        private Dictionary<string, FieldMapping> _markerMappings;

        // Панели шагов
        private Panel panelStep1;
        private Panel panelStep2;
        private Panel panelStep3;
        private Panel panelStep4;
        private int _currentStep = 1;

        // Шаг 1
        private TextBox txtDisplayName;
        private TextBox txtFilePath;
        private Label lblFileStatus;
        private ComboBox cmbStorage;
        private RichTextBox rtbMarkersFound;
        private ComboBox cmbPeriodType;

        // Шаг 2
        private CheckedListBox clbTables;
        private CheckBox chkShowTotalRow;
        private TextBox txtTotalRowCaption;
        private CheckedListBox clbSummaryColumns;
        private CheckedListBox clbTotalRowTables;

        // Шаг 3
        private TabControl tabControlMarkers;
        private ListBox lbTextMarkers;
        private ListBox lbTableMarkers;
        private Panel pnlTextSettings;
        private Panel pnlTableSettings;
        private ComboBox cmbDataType;
        private ComboBox cmbStorageField;
        private TextBox txtStaticValue;
        private ComboBox cmbDateFormat;
        private Panel currentSettingsPanel;
        private TemplateMarkerInfo currentMarker;
        private bool isCurrentTableMarker;

        // Шаг 4
        private Label lblSummary;

        // Навигация
        private Button btnBack;
        private Button btnNext;
        private Button btnFinish;
        private Button btnCancel;
        private Label lblStepIndicator;

        public TemplateWizardForm(ConfigService configService, StorageConfigService storageConfigService)
            : this(configService, storageConfigService, null)
        {
        }

        public TemplateWizardForm(ConfigService configService, StorageConfigService storageConfigService, TemplateConfig existingTemplate)
        {
            _configService = configService;
            _storageConfigService = storageConfigService;
            _existingConfigs = _configService.GetAllConfigs() ?? new List<TemplateConfig>();
            _availableStorages = _storageConfigService.LoadAll() ?? new List<StorageConfig>();
            _markers = new List<TemplateMarkerInfo>();
            _markerMappings = new Dictionary<string, FieldMapping>();

            _isEditMode = existingTemplate != null;
            _editingTemplate = existingTemplate;

            try
            {
                var settingsService = new SettingsService();
                _defaultOrganizationName = settingsService.GetOrganizationName();
            }
            catch
            {
                _defaultOrganizationName = "Организация не указана";
            }

            InitializeComponent();
            SetupForm();

            if (_isEditMode)
            {
                LoadTemplateForEdit();
            }

            UpdateNavigation();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // запрещает растягивание
            this.MaximizeBox = false;
            this.Text = _isEditMode ? "Редактирование шаблона" : "Создание шаблона";
            this.Size = new Size(850, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 650);
        }

        private void SetupForm()
        {
            this.Icon = new Icon(System.IO.Path.Combine(Application.StartupPath, "Res\\icons8.ico"));
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                RowStyles = {
                    new RowStyle(SizeType.Percent, 90f),
                    new RowStyle(SizeType.Percent, 10f)
                }
            };
            this.Controls.Add(mainLayout);

            Panel topPanel = new Panel { Dock = DockStyle.Fill };
            mainLayout.Controls.Add(topPanel, 0, 0);

            SetupBottomPanel(mainLayout);
            SetupStep1(topPanel);
            SetupStep2(topPanel);
            SetupStep3(topPanel);
            SetupStep4(topPanel);
        }

        // ========== ШАГ 1: ОСНОВНЫЕ ПАРАМЕТРЫ ==========
        private void SetupStep1(Panel parent)
        {
            panelStep1 = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = true
            };

            int y = 15;
            int margin = 15;
            int labelWidth = 150;
            int controlWidth = 350;

            AddSectionTitle(panelStep1, "ШАГ 1: ОСНОВНЫЕ ПАРАМЕТРЫ", margin, ref y);
            y += 10;

            AddLabel(panelStep1, "Название шаблона:", margin, y, labelWidth);
            txtDisplayName = AddTextBox(panelStep1, margin + labelWidth + 5, y, controlWidth);
            y += 35;

            AddLabel(panelStep1, "Файл шаблона (.docx):", margin, y, labelWidth);
            txtFilePath = AddTextBox(panelStep1, margin + labelWidth + 5, y, controlWidth - 80);
            Button btnBrowse = AddButton(panelStep1, "Обзор", margin + labelWidth + 5 + controlWidth - 75, y, 70);
            btnBrowse.Click += BtnBrowse_Click;
            y += 35;

            lblFileStatus = AddLabel(panelStep1, "Статус: файл не выбран", margin, y);
            lblFileStatus.ForeColor = Color.Gray;
            y += 35;

            AddLabel(panelStep1, "Хранилище данных:", margin, y, labelWidth);
            cmbStorage = new ComboBox
            {
                Location = new Point(margin + labelWidth + 5, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "DisplayName",
                ValueMember = "Id"
            };
            cmbStorage.DataSource = _availableStorages;
            panelStep1.Controls.Add(cmbStorage);
            y += 45;

            AddLabel(panelStep1, "Период отчета:", margin, y, labelWidth);
            cmbPeriodType = new ComboBox
            {
                Location = new Point(margin + labelWidth + 5, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPeriodType.Items.AddRange(new[] { "День", "Месяц", "Год" });
            cmbPeriodType.SelectedIndex = 1;
            panelStep1.Controls.Add(cmbPeriodType);
            y += 45;

            AddSectionTitle(panelStep1, "НАЙДЕННЫЕ МАРКЕРЫ В ДОКУМЕНТЕ:", margin, ref y);
            y += 5;

            rtbMarkersFound = new RichTextBox
            {
                Location = new Point(margin, y),
                Size = new Size(panelStep1.Width - margin * 2 - 20, 250),
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelStep1.Controls.Add(rtbMarkersFound);

            parent.Controls.Add(panelStep1);
        }

        // ========== ШАГ 2: НАСТРОЙКА ТАБЛИЦ И ИТОГОВ ==========
        private void SetupStep2(Panel parent)
        {
            panelStep2 = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };

            int y = 15;
            int margin = 15;

            AddSectionTitle(panelStep2, "ШАГ 2: НАСТРОЙКА ТАБЛИЦ И ИТОГОВ", margin, ref y);
            y += 10;

            GroupBox grpTables = new GroupBox
            {
                Text = "Какие таблицы заполнять данными?",
                Location = new Point(margin, y),
                Size = new Size(panelStep2.Width - margin * 2 - 20, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelStep2.Controls.Add(grpTables);

            Label lblTablesInfo = new Label
            {
                Text = "Отметьте таблицы, которые будут заполняться данными из хранилища:",
                Location = new Point(10, 20),
                AutoSize = true
            };
            grpTables.Controls.Add(lblTablesInfo);

            clbTables = new CheckedListBox
            {
                Location = new Point(10, 45),
                Size = new Size(grpTables.Width - 30, 50),
                CheckOnClick = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            grpTables.Controls.Add(clbTables);
            y = grpTables.Bottom + 15;

            GroupBox grpTotalRow = new GroupBox
            {
                Text = "Настройки итоговой строки",
                Location = new Point(margin, y),
                Size = new Size(panelStep2.Width - margin * 2 - 20, 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelStep2.Controls.Add(grpTotalRow);

            int ty = 15;

            chkShowTotalRow = new CheckBox
            {
                Text = "Добавлять итоговую строку",
                Location = new Point(10, ty),
                AutoSize = true,
                Checked = true
            };
            grpTotalRow.Controls.Add(chkShowTotalRow);
            ty += 30;

            Label lblTotalRowTables = new Label
            {
                Text = "В каких таблицах показывать итоговую строку:",
                Location = new Point(25, ty),
                AutoSize = true
            };
            grpTotalRow.Controls.Add(lblTotalRowTables);
            ty += 22;

            clbTotalRowTables = new CheckedListBox
            {
                Location = new Point(25, ty),
                Size = new Size(grpTotalRow.Width - 50, 60),
                CheckOnClick = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            grpTotalRow.Controls.Add(clbTotalRowTables);
            ty += 70;

            Label lblCaption = new Label
            {
                Text = "Подпись в итоговой строке:",
                Location = new Point(25, ty),
                AutoSize = true
            };
            grpTotalRow.Controls.Add(lblCaption);

            txtTotalRowCaption = new TextBox
            {
                Text = "ИТОГО:",
                Location = new Point(200, ty - 3),
                Size = new Size(150, 25)
            };
            grpTotalRow.Controls.Add(txtTotalRowCaption);
            ty += 35;

            Label lblSummaryColumns = new Label
            {
                Text = "Какие колонки суммировать:",
                Location = new Point(10, ty),
                AutoSize = true
            };
            grpTotalRow.Controls.Add(lblSummaryColumns);
            ty += 22;

            clbSummaryColumns = new CheckedListBox
            {
                Location = new Point(10, ty),
                Size = new Size(grpTotalRow.Width - 30, 80),
                CheckOnClick = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            grpTotalRow.Controls.Add(clbSummaryColumns);

            chkShowTotalRow.CheckedChanged += (s, e) =>
            {
                bool enabled = chkShowTotalRow.Checked;
                txtTotalRowCaption.Enabled = enabled;
                clbSummaryColumns.Enabled = enabled;
                clbTotalRowTables.Enabled = enabled;
            };

            parent.Controls.Add(panelStep2);
        }

        // ========== ШАГ 3: НАСТРОЙКА МАРКЕРОВ ==========
        private void SetupStep3(Panel parent)
        {
            panelStep3 = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };

            int y = 15;
            int margin = 15;

            AddSectionTitle(panelStep3, "ШАГ 3: НАСТРОЙКА МАРКЕРОВ", margin, ref y);
            y += 10;

            tabControlMarkers = new TabControl
            {
                Location = new Point(margin, y),
                Size = new Size(panelStep3.Width - margin * 2 - 20, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            panelStep3.Controls.Add(tabControlMarkers);

            // Вкладка: Текстовые маркеры
            TabPage tpTextMarkers = new TabPage("Текстовые маркеры");
            tabControlMarkers.TabPages.Add(tpTextMarkers);
            SetupMarkersTab(tpTextMarkers, isTableMarker: false);

            // Вкладка: Табличные маркеры
            TabPage tpTableMarkers = new TabPage("Табличные маркеры");
            tabControlMarkers.TabPages.Add(tpTableMarkers);
            SetupMarkersTab(tpTableMarkers, isTableMarker: true);

            parent.Controls.Add(panelStep3);
        }

        private void SetupMarkersTab(TabPage page, bool isTableMarker)
        {
            // SplitContainer
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 65
            };
            page.Controls.Add(splitContainer);

            // Левая часть: список маркеров
            Label lblMarkersList = new Label
            {
                Text = "Маркеры:",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            splitContainer.Panel1.Controls.Add(lblMarkersList);

            ListBox lbMarkers = new ListBox
            {
                Location = new Point(5, 30),
                Size = new Size(splitContainer.Panel1.Width - 10, splitContainer.Panel1.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Consolas", 9)
            };
            splitContainer.Panel1.Controls.Add(lbMarkers);

            // Правая часть: настройки
            Label lblSettings = new Label
            {
                Text = "Настройки маркера:",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            splitContainer.Panel2.Controls.Add(lblSettings);

            Panel pnlSettings = new Panel
            {
                Location = new Point(5, 30),
                Size = new Size(splitContainer.Panel2.Width - 10, splitContainer.Panel2.Height - 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true
            };
            splitContainer.Panel2.Controls.Add(pnlSettings);

            // Сохраняем ссылки
            if (isTableMarker)
            {
                lbTableMarkers = lbMarkers;
                pnlTableSettings = pnlSettings;
            }
            else
            {
                lbTextMarkers = lbMarkers;
                pnlTextSettings = pnlSettings;
            }

            lbMarkers.SelectedIndexChanged += (s, e) =>
            {
                if (lbMarkers.SelectedItem == null) return;
                string selectedItem = lbMarkers.SelectedItem.ToString();
                if (selectedItem.StartsWith("───")) return;

                var marker = GetMarkerByName(selectedItem);
                if (marker != null)
                {
                    currentMarker = marker;
                    isCurrentTableMarker = isTableMarker;
                    currentSettingsPanel = pnlSettings;
                    BuildSettingsPanel(pnlSettings, marker, isTableMarker);
                }
            };
        }

        private void FillMarkersList()
        {
            // Получаем индексы таблиц, которые пользователь отметил как таблицы данных
            var dataTableIndices = new List<int>();
            for (int i = 0; i < clbTables.Items.Count; i++)
            {
                if (clbTables.GetItemChecked(i))
                    dataTableIndices.Add(i);
            }

            // Текстовые маркеры (не в таблице ИЛИ в таблице, но таблица НЕ отмечена)
            lbTextMarkers.Items.Clear();
            var textMarkers = _markers.Where(m => !m.IsInTable || !dataTableIndices.Contains(m.TableIndex)).ToList();
            if (textMarkers.Any())
            {
                lbTextMarkers.Items.Add("─── В ТЕКСТЕ ───");
                foreach (var m in textMarkers)
                {
                    lbTextMarkers.Items.Add($"   {m.Marker}");
                }
            }

            // Табличные маркеры (в таблице И таблица отмечена)
            lbTableMarkers.Items.Clear();
            var markersByTable = _markers.Where(m => m.IsInTable && dataTableIndices.Contains(m.TableIndex))
                                         .GroupBy(m => m.TableIndex).OrderBy(g => g.Key);
            foreach (var tableGroup in markersByTable)
            {
                int tableNumber = tableGroup.Key + 1;
                lbTableMarkers.Items.Add($"─── ТАБЛИЦА {tableNumber} ───");
                foreach (var m in tableGroup)
                {
                    lbTableMarkers.Items.Add($"   {m.Marker}");
                }
            }
        }

        private TemplateMarkerInfo GetMarkerByName(string markerText)
        {
            string cleanMarker = markerText.Trim();
            return _markers.FirstOrDefault(m => m.Marker == cleanMarker);
        }

        private void BuildSettingsPanel(Panel pnlSettings, TemplateMarkerInfo marker, bool isTableMarker)
        {
            pnlSettings.Controls.Clear();
            cmbDataType = null;
            cmbStorageField = null;
            txtStaticValue = null;
            cmbDateFormat = null;

            if (!_markerMappings.ContainsKey(marker.Marker))
                _markerMappings[marker.Marker] = new FieldMapping();

            var mapping = _markerMappings[marker.Marker];

            int y = 5;
            int labelWidth = 120;
            int controlWidth = pnlSettings.Width - labelWidth - 30;

            // Заголовок
            Label lblTitle = new Label
            {
                Text = $"Настройка: {marker.Marker}",
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(5, y),
                AutoSize = true
            };
            pnlSettings.Controls.Add(lblTitle);
            y += 30;

            // Тип данных
            Label lblDataType = new Label
            {
                Text = "Тип данных:",
                Location = new Point(5, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSettings.Controls.Add(lblDataType);

            cmbDataType = new ComboBox
            {
                Location = new Point(labelWidth + 10, y),
                Size = new Size(controlWidth, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            if (isTableMarker)
            {
                cmbDataType.Items.AddRange(new[] { "📁 Данные из хранилища", "🔢 Автонумерация" });
                if (mapping.IsAggregate && mapping.AggregateType == "AutoNumber")
                    cmbDataType.SelectedItem = "🔢 Автонумерация";
                else
                    cmbDataType.SelectedItem = "📁 Данные из хранилища";
            }
            else
            {
                cmbDataType.Items.AddRange(new[] { "📝 Статическое значение", "⚙️ Из настроек", "📅 Текущая дата" });
                if (mapping.IsStatic && mapping.AggregateType != "OrganizationName")
                    cmbDataType.SelectedItem = "📝 Статическое значение";
                else if (mapping.AggregateType == "OrganizationName")
                    cmbDataType.SelectedItem = "⚙️ Из настроек";
                else if (!string.IsNullOrEmpty(mapping.DateFormat))
                    cmbDataType.SelectedItem = "📅 Текущая дата";
                else
                    cmbDataType.SelectedItem = "📝 Статическое значение";
            }

            pnlSettings.Controls.Add(cmbDataType);
            y += 35;

            // Значение
            Label lblValue = new Label
            {
                Text = "Значение:",
                Location = new Point(5, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlSettings.Controls.Add(lblValue);

            Panel valuePanel = new Panel
            {
                Location = new Point(labelWidth + 10, y),
                Size = new Size(controlWidth, 80)
            };
            pnlSettings.Controls.Add(valuePanel);

            // Создаем контролы
            cmbStorageField = new ComboBox
            {
                Name = "cmbStorageField",
                Location = new Point(0, 0),
                Size = new Size(valuePanel.Width, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            if (_selectedStorage != null)
            {
                // Создаем список с "не выбрано" в начале
                var fieldList = new List<StorageField>();
                fieldList.Add(new StorageField { Id = 0, Display = "(не выбрано)" });
                fieldList.AddRange(_selectedStorage.Fields);

                cmbStorageField.DataSource = fieldList;
                cmbStorageField.DisplayMember = "Display";
                cmbStorageField.ValueMember = "Id";
            }
            valuePanel.Controls.Add(cmbStorageField);

            txtStaticValue = new TextBox
            {
                Name = "txtStaticValue",
                Location = new Point(0, 0),
                Size = new Size(valuePanel.Width, 25),
                Visible = false
            };
            valuePanel.Controls.Add(txtStaticValue);

            cmbDateFormat = new ComboBox
            {
                Name = "cmbDateFormat",
                Location = new Point(0, 0),
                Size = new Size(valuePanel.Width, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            cmbDateFormat.Items.AddRange(new[] { "ДД.ММ.ГГГГ", "ММ.ГГГГ", "ГГГГ", "Месяц (словом)" });
            valuePanel.Controls.Add(cmbDateFormat);

            // Загружаем сохраненные значения
            LoadSavedValues(valuePanel, mapping);

            // Обработчик изменения типа данных
            cmbDataType.SelectedIndexChanged += (s, e) =>
            {
                UpdateValueControl(valuePanel, marker);
                SaveCurrentMapping(marker);
            };

            // Обработчики изменения значений
            cmbStorageField.SelectedIndexChanged += (s, e) => SaveCurrentMapping(marker);
            txtStaticValue.TextChanged += (s, e) => SaveCurrentMapping(marker);
            cmbDateFormat.SelectedIndexChanged += (s, e) => SaveCurrentMapping(marker);
        }

        private void LoadSavedValues(Panel valuePanel, FieldMapping mapping)
        {
            cmbStorageField.Visible = false;
            txtStaticValue.Visible = false;
            cmbDateFormat.Visible = false;

            string selectedType = cmbDataType.SelectedItem?.ToString();

            if (isCurrentTableMarker)
            {
                if (selectedType == "📁 Данные из хранилища")
                {
                    cmbStorageField.Visible = true;
                    if (!string.IsNullOrEmpty(mapping.StorageFieldId) && int.TryParse(mapping.StorageFieldId, out int id))
                    {
                        // Ищем поле с таким Id в DataSource
                        foreach (var item in cmbStorageField.Items)
                        {
                            var field = item as StorageField;
                            if (field != null && field.Id == id)
                            {
                                cmbStorageField.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Если не найдено — выбираем "(не выбрано)"
                        foreach (var item in cmbStorageField.Items)
                        {
                            var field = item as StorageField;
                            if (field != null && field.Id == 0)
                            {
                                cmbStorageField.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (selectedType == "📝 Статическое значение")
                {
                    txtStaticValue.Visible = true;
                    txtStaticValue.Text = mapping.StaticValue ?? "";
                    if (string.IsNullOrEmpty(txtStaticValue.Text) && currentMarker?.CleanName == "OrganizationName")
                        txtStaticValue.Text = _defaultOrganizationName;
                }
                else if (selectedType == "⚙️ Из настроек")
                {
                    // ничего не показываем
                }
                else if (selectedType == "📅 Текущая дата")
                {
                    cmbDateFormat.Visible = true;
                    cmbDateFormat.SelectedItem = !string.IsNullOrEmpty(mapping.DateFormat) ? mapping.DateFormat : "ДД.ММ.ГГГГ";
                }
            }
        }

        private void UpdateValueControl(Panel valuePanel, TemplateMarkerInfo marker)
        {
            string selectedType = cmbDataType.SelectedItem?.ToString();

            cmbStorageField.Visible = false;
            txtStaticValue.Visible = false;
            cmbDateFormat.Visible = false;

            if (isCurrentTableMarker)
            {
                if (selectedType == "📁 Данные из хранилища")
                {
                    cmbStorageField.Visible = true;
                    if (cmbStorageField.SelectedItem == null)
                        cmbStorageField.SelectedItem = "(не выбрано)";
                }
            }
            else
            {
                if (selectedType == "📝 Статическое значение")
                {
                    txtStaticValue.Visible = true;
                    if (string.IsNullOrEmpty(txtStaticValue.Text) && marker.CleanName == "OrganizationName")
                        txtStaticValue.Text = _defaultOrganizationName;
                }
                else if (selectedType == "📅 Текущая дата")
                {
                    cmbDateFormat.Visible = true;
                    if (cmbDateFormat.SelectedItem == null)
                        cmbDateFormat.SelectedIndex = 0;
                }
            }
        }

        private void SaveCurrentMapping(TemplateMarkerInfo marker)
        {
            string selectedType = cmbDataType.SelectedItem?.ToString();
            var mapping = new FieldMapping();

            if (isCurrentTableMarker)
            {
                if (selectedType == "📁 Данные из хранилища")
                {
                    var selectedField = cmbStorageField.SelectedItem as StorageField;
                    mapping.StorageFieldId = selectedField?.Id.ToString() ?? "";
                }
                else if (selectedType == "🔢 Автонумерация")
                {
                    mapping.IsAggregate = true;
                    mapping.AggregateType = "AutoNumber";
                }
            }
            else
            {
                if (selectedType == "📝 Статическое значение")
                {
                    mapping.IsStatic = true;
                    mapping.StaticValue = txtStaticValue.Text;
                }
                else if (selectedType == "⚙️ Из настроек")
                {
                    mapping.IsAggregate = true;
                    mapping.AggregateType = "OrganizationName";
                }
                else if (selectedType == "📅 Текущая дата")
                {
                    mapping.DateFormat = cmbDateFormat.SelectedItem?.ToString();
                }
            }

            _markerMappings[marker.Marker] = mapping;
        }

        private void LoadMarkersData()
        {
            FillMarkersList();

            // Выбираем первый текстовый маркер, если есть
            if (lbTextMarkers.Items.Count > 0)
            {
                for (int i = 0; i < lbTextMarkers.Items.Count; i++)
                {
                    string item = lbTextMarkers.Items[i].ToString();
                    if (!item.StartsWith("───"))
                    {
                        lbTextMarkers.SelectedIndex = i;
                        break;
                    }
                }
            }
            // Иначе выбираем первый табличный
            else if (lbTableMarkers.Items.Count > 0)
            {
                for (int i = 0; i < lbTableMarkers.Items.Count; i++)
                {
                    string item = lbTableMarkers.Items[i].ToString();
                    if (!item.StartsWith("───"))
                    {
                        lbTableMarkers.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        // ========== ШАГ 4: ПОДТВЕРЖДЕНИЕ ==========
        private void SetupStep4(Panel parent)
        {
            panelStep4 = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };

            int y = 15;
            int margin = 15;

            AddSectionTitle(panelStep4, "ШАГ 4: ПОДТВЕРЖДЕНИЕ", margin, ref y);
            y += 10;

            lblSummary = new Label
            {
                Location = new Point(margin, y),
                Size = new Size(panelStep4.Width - margin * 2 - 20, 450),
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelStep4.Controls.Add(lblSummary);

            parent.Controls.Add(panelStep4);
        }

        private void SetupBottomPanel(TableLayoutPanel parent)
        {
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            parent.Controls.Add(bottomPanel, 0, 1);

            int y = 12;

            lblStepIndicator = new Label
            {
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 9)
            };
            bottomPanel.Controls.Add(lblStepIndicator);

            btnBack = new Button
            {
                Text = "< Назад",
                Location = new Point(bottomPanel.Width - 320, y),
                Size = new Size(100, 35),
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBack.Click += BtnBack_Click;
            bottomPanel.Controls.Add(btnBack);

            btnNext = new Button
            {
                Text = "Далее >",
                Location = new Point(bottomPanel.Width - 210, y),
                Size = new Size(100, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNext.Click += BtnNext_Click;
            bottomPanel.Controls.Add(btnNext);

            btnFinish = new Button
            {
                Text = "Создать",
                Location = new Point(bottomPanel.Width - 210, y),
                Size = new Size(100, 35),
                Visible = false,
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnFinish.Click += BtnFinish_Click;
            bottomPanel.Controls.Add(btnFinish);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(bottomPanel.Width - 100, y),
                Size = new Size(90, 35),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            bottomPanel.Controls.Add(btnCancel);

            bottomPanel.Resize += (s, e) =>
            {
                btnBack.Location = new Point(bottomPanel.Width - 320, 12);
                btnNext.Location = new Point(bottomPanel.Width - 210, 12);
                btnFinish.Location = new Point(bottomPanel.Width - 210, 12);
                btnCancel.Location = new Point(bottomPanel.Width - 100, 12);
            };
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private void AddSectionTitle(Panel panel, string text, int x, ref int y)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            panel.Controls.Add(lbl);
            y += 25;
        }

        private Label AddLabel(Panel panel, string text, int x, int y, int width = 0)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = width > 0 ? new Size(width, 25) : new Size(0, 25),
                AutoSize = width == 0
            };
            panel.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(Panel panel, int x, int y, int width)
        {
            TextBox txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25)
            };
            panel.Controls.Add(txt);
            return txt;
        }

        private Button AddButton(Panel panel, string text, int x, int y, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25)
            };
            panel.Controls.Add(btn);
            return btn;
        }

        private void ShowStep(int step)
        {
            panelStep1.Visible = (step == 1);
            panelStep2.Visible = (step == 2);
            panelStep3.Visible = (step == 3);
            panelStep4.Visible = (step == 4);
            _currentStep = step;
            lblStepIndicator.Text = $"Шаг {step} из 4";

            if (step == 3)
            {
                LoadMarkersData();
            }
            else if (step == 4)
            {
                UpdateSummary();
            }
        }

        private void UpdateNavigation()
        {
            btnBack.Enabled = _currentStep > 1;
            btnNext.Visible = _currentStep < 4;
            btnFinish.Visible = _currentStep == 4;
        }

        // ========== ОСНОВНАЯ ЛОГИКА ==========

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Выберите файл шаблона отчета",
                Filter = "Документ Word (*.docx)|*.docx",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _originalFilePath = ofd.FileName;
                txtFilePath.Text = _originalFilePath;

                if (_isEditMode && _editingTemplate != null)
                    _editingTemplate.TemplateFile = Path.GetFileName(_originalFilePath);

                string tempFolder = Path.Combine(Path.GetTempPath(), "TemplateWizard");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                foreach (var oldFile in Directory.GetFiles(tempFolder, "*.docx"))
                {
                    try { File.Delete(oldFile); } catch { }
                }

                _selectedFilePath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + ".docx");
                File.Copy(_originalFilePath, _selectedFilePath, true);

                AnalyzeFile();
            }
        }

        private void AnalyzeFile()
        {
            try
            {
                var docService = new DocumentService();
                _markers = docService.ExtractMarkersWithLocation(_selectedFilePath);
                _foundTables = docService.GetTableInfos(_selectedFilePath);
                _markerMappings.Clear();

                if (_markers.Count == 0)
                {
                    lblFileStatus.Text = "❌ Маркеров [*...*] не найдено";
                    rtbMarkersFound.Text = "В документе не найдено маркеров вида [*ИмяПоля*]";
                    btnNext.Enabled = false;
                    return;
                }

                lblFileStatus.Text = $"✅ Найдено маркеров: {_markers.Count}";

                string markersText = "МАРКЕРЫ В ДОКУМЕНТЕ:\n\n";
                var markersByTable = _markers.Where(m => m.IsInTable).GroupBy(m => m.TableIndex).OrderBy(g => g.Key);

                foreach (var tableGroup in markersByTable)
                {
                    int tableNumber = tableGroup.Key + 1;
                    markersText += $"--- В ТАБЛИЦЕ {tableNumber} ---\n";
                    foreach (var m in tableGroup)
                        markersText += $"   • {m.Marker}\n";
                    markersText += "\n";
                }

                var textMarkers = _markers.Where(m => !m.IsInTable).ToList();
                if (textMarkers.Any())
                {
                    markersText += "--- В ТЕКСТЕ ---\n";
                    foreach (var m in textMarkers)
                        markersText += $"   • {m.Marker}\n";
                }

                rtbMarkersFound.Text = markersText;

                // Заполняем clbTables
                clbTables.Items.Clear();
                foreach (var table in _foundTables)
                {
                    int markerCount = _markers.Count(m => m.IsInTable && m.TableIndex == table.Index);
                    string display = $"Таблица {table.Index + 1}: {table.RowCount} строк × {table.ColCount} колонок (маркеров: {markerCount})";
                    if (!string.IsNullOrEmpty(table.FirstCellText))
                    {
                        string preview = table.FirstCellText.Length > 40 ? table.FirstCellText.Substring(0, 40) + "..." : table.FirstCellText;
                        display += $" — \"{preview.Replace("\n", " ").Replace("\r", "")}\"";
                    }
                    clbTables.Items.Add(display, true);
                }

                // Заполняем clbTotalRowTables
                clbTotalRowTables.Items.Clear();
                foreach (var table in _foundTables)
                {
                    string display = $"Таблица {table.Index + 1}: {table.RowCount} строк × {table.ColCount} колонок";
                    if (!string.IsNullOrEmpty(table.FirstCellText))
                    {
                        string preview = table.FirstCellText.Length > 40 ? table.FirstCellText.Substring(0, 40) + "..." : table.FirstCellText;
                        preview = preview.Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");
                        display += $" — \"{preview}\"";
                    }
                    clbTotalRowTables.Items.Add(display, true);
                }

                // Заполняем clbSummaryColumns
                clbSummaryColumns.Items.Clear();
                foreach (var marker in _markers.Where(m => m.IsInTable))
                {
                    clbSummaryColumns.Items.Add(marker.Marker, false);
                }

                btnNext.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnNext.Enabled = false;
            }
        }

        private void UpdateSummary()
        {
            string summary = $"═══════════════════════════════════════════\n";
            summary += $"СВОДКА ШАБЛОНА\n";
            summary += $"═══════════════════════════════════════════\n\n";
            summary += $"Название: {txtDisplayName.Text}\n";
            summary += $"Файл: {Path.GetFileName(_selectedFilePath)}\n";
            summary += $"Хранилище: {(_selectedStorage?.DisplayName ?? "не выбрано")}\n\n";

            string period = cmbPeriodType.SelectedItem?.ToString() ?? "Месяц";
            summary += $"Период: {period}\n\n";

            summary += $"Таблиц с данными: {clbTables.CheckedItems.Count}\n";
            summary += $"Итоговая строка: {(chkShowTotalRow.Checked ? "Да" : "Нет")}\n";
            if (chkShowTotalRow.Checked)
            {
                summary += $"  Подпись: {txtTotalRowCaption.Text}\n";
                summary += $"  Колонок для суммирования: {clbSummaryColumns.CheckedItems.Count}\n";
            }

            summary += $"\nНАСТРОЙКА МАРКЕРОВ:\n";
            foreach (var kvp in _markerMappings)
            {
                string marker = kvp.Key;
                var mapping = kvp.Value;
                string info = "";
                if (mapping.IsStatic)
                    info = $"статическое: {mapping.StaticValue}";
                else if (!string.IsNullOrEmpty(mapping.DateFormat))
                    info = $"дата: {mapping.DateFormat}";
                else if (!string.IsNullOrEmpty(mapping.StorageFieldId))
                    info = $"из хранилища: {mapping.StorageFieldId}";
                else if (mapping.IsAggregate)
                    info = mapping.AggregateType == "OrganizationName" ? "название организации" : "автонумерация";
                summary += $"   • {marker} → {info}\n";
            }

            lblSummary.Text = summary;
        }

        private void LoadTemplateForEdit()
        {
            if (_editingTemplate == null) return;

            txtDisplayName.Text = _editingTemplate.DisplayName;

            string templatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Templates");
            _originalFilePath = Path.Combine(templatesFolder, _editingTemplate.TemplateFile);
            txtFilePath.Text = _originalFilePath;

            if (!File.Exists(_originalFilePath))
            {
                DialogResult result = MessageBox.Show(
                    $"Файл шаблона не найден:\n{_originalFilePath}\n\nВыбрать новый файл?",
                    "Файл не найден",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Title = "Выберите файл шаблона",
                        Filter = "Документ Word (*.docx)|*.docx"
                    };
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _originalFilePath = ofd.FileName;
                        txtFilePath.Text = _originalFilePath;
                        _editingTemplate.TemplateFile = Path.GetFileName(_originalFilePath);
                    }
                    else
                    {
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                        return;
                    }
                }
                else if (result == DialogResult.No)
                {
                    _selectedFilePath = null;
                    txtFilePath.Text = _originalFilePath + " (ФАЙЛ НЕ НАЙДЕН)";
                    txtFilePath.ForeColor = Color.Red;
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }
            }

            if (File.Exists(_originalFilePath))
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "TemplateWizard");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);
                _selectedFilePath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + ".docx");
                File.Copy(_originalFilePath, _selectedFilePath, true);
            }

            if (!string.IsNullOrEmpty(_editingTemplate.StorageId))
            {
                var storage = _availableStorages.FirstOrDefault(s => s.Id == _editingTemplate.StorageId);
                if (storage != null)
                    cmbStorage.SelectedItem = storage;
            }

            chkShowTotalRow.Checked = _editingTemplate.ShowTotalRow;
            txtTotalRowCaption.Text = _editingTemplate.TotalRowCaption;

            if (!string.IsNullOrEmpty(_editingTemplate.PeriodType))
            {
                switch (_editingTemplate.PeriodType)
                {
                    case "day":
                        cmbPeriodType.SelectedItem = "День";
                        break;
                    case "month":
                        cmbPeriodType.SelectedItem = "Месяц";
                        break;
                    case "year":
                        cmbPeriodType.SelectedItem = "Год";
                        break;
                    default:
                        cmbPeriodType.SelectedItem = "Месяц";
                        break;
                }
            }
            else
            {
                cmbPeriodType.SelectedItem = "Месяц";  // значение по умолчанию
            }

            _markerMappings.Clear();
            foreach (var kvp in _editingTemplate.TextFieldMappings)
                _markerMappings[$"[*{kvp.Key}*]"] = kvp.Value;
            foreach (var kvp in _editingTemplate.TableFieldMappings)
                _markerMappings[$"[*{kvp.Key}*]"] = kvp.Value;



            if (_selectedFilePath != null && File.Exists(_selectedFilePath))
            {
                try
                {
                    var docService = new DocumentService();
                    _markers = docService.ExtractMarkersWithLocation(_selectedFilePath);
                    _foundTables = docService.GetTableInfos(_selectedFilePath);

                    string markersText = "МАРКЕРЫ В ДОКУМЕНТЕ:\n\n";
                    var markersByTable = _markers.Where(m => m.IsInTable).GroupBy(m => m.TableIndex).OrderBy(g => g.Key);
                    if (markersByTable.Any())
                    {
                        foreach (var tableGroup in markersByTable)
                        {
                            int tableNumber = tableGroup.Key + 1;
                            markersText += $"--- В ТАБЛИЦЕ {tableNumber} ---\n";
                            foreach (var m in tableGroup)
                                markersText += $"   • {m.Marker}\n";
                            markersText += "\n";
                        }
                    }

                    var textMarkers = _markers.Where(m => !m.IsInTable).ToList();
                    if (textMarkers.Any())
                    {
                        markersText += "--- В ТЕКСТЕ ---\n";
                        foreach (var m in textMarkers)
                            markersText += $"   • {m.Marker}\n";
                    }

                    rtbMarkersFound.Text = markersText;

                    clbTables.Items.Clear();
                    foreach (var table in _foundTables)
                    {
                        string display = $"Таблица {table.Index + 1}: {table.RowCount} строк × {table.ColCount} колонок";
                        if (!string.IsNullOrEmpty(table.FirstCellText))
                            display += $" — \"{table.FirstCellText.Substring(0, Math.Min(40, table.FirstCellText.Length))}...\"";
                        clbTables.Items.Add(display, _editingTemplate.DataTableIndices?.Contains(table.Index) ?? true);
                    }

                    clbTotalRowTables.Items.Clear();
                    foreach (var table in _foundTables)
                    {
                        string display = $"Таблица {table.Index + 1}: {table.RowCount} строк × {table.ColCount} колонок";
                        if (!string.IsNullOrEmpty(table.FirstCellText))
                        {
                            string preview = table.FirstCellText.Length > 40 ? table.FirstCellText.Substring(0, 40) + "..." : table.FirstCellText;
                            display += $" — \"{preview.Replace("\n", " ").Replace("\r", "")}\"";
                        }
                        clbTotalRowTables.Items.Add(display, _editingTemplate.TotalRowTableIndices?.Contains(table.Index) ?? true);
                    }
                    clbSummaryColumns.Items.Clear();
                    foreach (var marker in _markers.Where(m => m.IsInTable))
                    {
                        bool isChecked = _editingTemplate.SummaryColumns != null &&
                                         _editingTemplate.SummaryColumns.Contains(marker.Marker);
                        clbSummaryColumns.Items.Add(marker.Marker, isChecked);
                    }
                    // =============================

                    btnNext.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка анализа документа: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnNext.Enabled = true;
                }
            }
            else
            {
                rtbMarkersFound.Text = "Файл шаблона не найден. Вы можете редактировать настройки маркеров.";
                btnNext.Enabled = true;
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_currentStep == 1)
            {
                if (string.IsNullOrEmpty(_selectedFilePath))
                {
                    MessageBox.Show("Выберите файл шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtDisplayName.Text))
                {
                    MessageBox.Show("Введите название шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cmbStorage.SelectedItem == null)
                {
                    MessageBox.Show("Выберите хранилище данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _selectedStorage = cmbStorage.SelectedItem as StorageConfig;
                ShowStep(2);
            }
            else if (_currentStep == 2)
            {
                ShowStep(3);
            }
            else if (_currentStep == 3)
            {
                ShowStep(4);
            }
            UpdateNavigation();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (_currentStep > 1)
                ShowStep(_currentStep - 1);
            UpdateNavigation();
        }

        private void BtnFinish_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_originalFilePath))
            {
                var result = MessageBox.Show(
                    $"Исходный файл шаблона не найден:\n{_originalFilePath}\n\nВыбрать другой файл?",
                    "Файл не найден",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Title = "Выберите файл шаблона",
                        Filter = "Документ Word (*.docx)|*.docx"
                    };
                    if (ofd.ShowDialog() == DialogResult.OK)
                        _originalFilePath = ofd.FileName;
                    else
                        return;
                }
                else
                    return;
            }

            if (string.IsNullOrWhiteSpace(txtDisplayName.Text))
            {
                MessageBox.Show("Введите название шаблона", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dataTableIndices = new List<int>();
            for (int i = 0; i < clbTables.Items.Count; i++)
            {
                if (clbTables.GetItemChecked(i))
                    dataTableIndices.Add(i);
            }

            var textFieldMappings = new Dictionary<string, FieldMapping>();
            var tableFieldMappings = new Dictionary<string, FieldMapping>();
            string autoNumberMarker = null;

            foreach (var kvp in _markerMappings)
            {
                string marker = kvp.Key;
                var mapping = kvp.Value;
                var cleanName = GetCleanMarkerName(marker);
                var markerInfo = _markers.FirstOrDefault(m => m.Marker == marker);

                if (markerInfo == null) continue;

                bool isInDataTable = markerInfo.IsInTable && dataTableIndices.Contains(markerInfo.TableIndex);

                if (mapping.IsAggregate && mapping.AggregateType == "AutoNumber")
                {
                    autoNumberMarker = cleanName;
                    continue;
                }

                if (isInDataTable)
                {
                    tableFieldMappings[cleanName] = mapping;
                }
                else
                {
                    textFieldMappings[cleanName] = mapping;
                }
            }

            var newConfig = new TemplateConfig
            {
                Id = _isEditMode ? _editingTemplate.Id : Guid.NewGuid().ToString(),
                DisplayName = txtDisplayName.Text,
                TemplateFile = Path.GetFileName(_selectedFilePath),
                StorageId = _selectedStorage.Id,
                TextFieldMappings = textFieldMappings,
                TableFieldMappings = tableFieldMappings,
                AutoNumberMarker = autoNumberMarker,
                DataTableIndices = dataTableIndices,
                ShowTotalRow = chkShowTotalRow.Checked,
                TotalRowCaption = txtTotalRowCaption.Text.Trim(),
                SummaryColumns = clbSummaryColumns.CheckedItems.Cast<string>().Select(m => GetCleanMarkerName(m)).ToList()
            };

            var totalRowTableIndices = new List<int>();
            for (int i = 0; i < clbTotalRowTables.Items.Count; i++)
            {
                if (clbTotalRowTables.GetItemChecked(i))
                    totalRowTableIndices.Add(i);
            }
            newConfig.TotalRowTableIndices = totalRowTableIndices;

            string periodType = "month";
            if (cmbPeriodType != null)
            {
                switch (cmbPeriodType.SelectedItem?.ToString())
                {
                    case "День": periodType = "day"; break;
                    case "Месяц": periodType = "month"; break;
                    case "Год": periodType = "year"; break;
                }
            }
            newConfig.PeriodType = periodType;

            if (!_isEditMode && _existingConfigs.Any(c => c.DisplayName == txtDisplayName.Text))
            {
                MessageBox.Show("Шаблон с таким названием уже существует", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isEditMode)
            {
                int index = _existingConfigs.FindIndex(c => c.Id == _editingTemplate.Id);
                _existingConfigs[index] = newConfig;
            }
            else
            {
                _existingConfigs.Add(newConfig);
            }

            string templatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Templates");
            if (!Directory.Exists(templatesFolder))
                Directory.CreateDirectory(templatesFolder);

            string targetFilePath = Path.Combine(templatesFolder, newConfig.TemplateFile);
            File.Copy(_originalFilePath, targetFilePath, true);

            _configService.SaveAllConfigs(_existingConfigs);

            MessageBox.Show($"Шаблон \"{txtDisplayName.Text}\" успешно {(_isEditMode ? "обновлен" : "создан")}!",
                           "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private string GetCleanMarkerName(string fullMarker)
        {
            if (string.IsNullOrEmpty(fullMarker)) return null;
            var regex = new Regex(@"\[\*([^\]]+)\*\]");
            var match = regex.Match(fullMarker);
            return match.Success ? match.Groups[1].Value : fullMarker;
        }
    }
}