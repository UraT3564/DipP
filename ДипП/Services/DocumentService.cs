using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ДипП.Models;

namespace ДипП.Services
{
    public class DocumentService
    {
        private readonly Dictionary<string, string> _monthTranslations = new Dictionary<string, string>
        {
            ["January"] = "Январь",
            ["February"] = "Февраль",
            ["March"] = "Март",
            ["April"] = "Апрель",
            ["May"] = "Май",
            ["June"] = "Июнь",
            ["July"] = "Июль",
            ["August"] = "Август",
            ["September"] = "Сентябрь",
            ["October"] = "Октябрь",
            ["November"] = "Ноябрь",
            ["December"] = "Декабрь"
        };

        public string GenerateReport(TemplateConfig config, Dictionary<string, string> textFields,
            List<Dictionary<string, string>> tableData, DateTime reportDate)
        {
            try
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Templates", config.TemplateFile);

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Шаблон не найден: {templatePath}");

                // Формируем имя выходного файла
                string orgName = textFields.ContainsKey("OrganizationName") ? textFields["OrganizationName"] : "отчет";
                string month = reportDate.ToString("MMMM");
                string year = reportDate.Year.ToString();

                foreach (char c in Path.GetInvalidFileNameChars())
                    orgName = orgName.Replace(c, '_');

                if (orgName.Length > 50)
                    orgName = orgName.Substring(0, 50) + "...";

                string outputFileName = $"Отчет_{orgName}_{month}_{year}.docx";
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outputFileName);

                Document document = new Document();
                document.LoadFromFile(templatePath);

                // Заменяем текстовые поля
                ReplaceTextFields(document, textFields);

                // Обрабатываем все таблицы
                ProcessAllTables(document, config, tableData);

                document.SaveToFile(outputPath, FileFormat.Docx);
                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка генерации отчета: {ex.Message}", ex);
            }
        }

        private void ReplaceTextFields(Document document, Dictionary<string, string> textFields)
        {
            foreach (var field in textFields)
            {
                string marker = "[*" + field.Key + "*]";
                Console.WriteLine($"Поиск маркера: '{marker}', замена на: '{field.Value}'");

                int count = document.Replace(marker, field.Value, false, true);
                Console.WriteLine($"  Найдено и заменено: {count} раз(а)");

                if (count == 0)
                {
                    // Проверяем, есть ли маркер в тексте документа
                    string docText = document.GetText();
                    if (docText.Contains(marker))
                        Console.WriteLine($"  ВНИМАНИЕ: Маркер '{marker}' найден в тексте, но не заменен!");
                    else
                        Console.WriteLine($"  Маркер '{marker}' отсутствует в документе");
                }
            }
        }

        private void ProcessAllTables(Document document, TemplateConfig config, List<Dictionary<string, string>> tableData)
        {
            var dataTableIndices = config.DataTableIndices ?? new List<int>();
            int tableIndex = 0;

            foreach (Section section in document.Sections)
            {
                for (int i = 0; i < section.Body.ChildObjects.Count; i++)
                {
                    if (section.Body.ChildObjects[i] is Table table)
                    {
                        if (dataTableIndices.Contains(tableIndex))
                        {
                            ProcessDataTable(table, config, tableData);
                        }
                        else
                        {
                            ProcessLayoutTable(table, config);
                        }
                        tableIndex++;
                    }
                }
            }
        }

        private void ProcessDataTable(Table table, TemplateConfig config, List<Dictionary<string, string>> tableData)
        {
            if (table.Rows.Count == 0 || tableData == null || tableData.Count == 0) return;

            // 1. Находим строку с маркерами (строку-образец)
            int markerRowIndex = -1;
            for (int r = 0; r < table.Rows.Count; r++)
            {
                for (int c = 0; c < table.Rows[r].Cells.Count; c++)
                {
                    string cellText = table.Rows[r].Cells[c].Paragraphs[0].Text;
                    if (cellText.Contains("[*") && cellText.Contains("*]"))
                    {
                        markerRowIndex = r;
                        break;
                    }
                }
                if (markerRowIndex != -1) break;
            }

            if (markerRowIndex == -1) return;

            // 2. Извлекаем имена маркеров из строки-образца
            var markerNames = new List<string>();
            for (int c = 0; c < table.Rows[markerRowIndex].Cells.Count; c++)
            {
                string cellText = table.Rows[markerRowIndex].Cells[c].Paragraphs[0].Text;
                var match = Regex.Match(cellText, @"\[\*([^\]]+)\*\]");
                markerNames.Add(match.Success ? match.Groups[1].Value : null);
            }

            // 3. Определяем колонку для автонумерации
            int numberColumnIndex = -1;
            for (int i = 0; i < markerNames.Count; i++)
            {
                if (markerNames[i] == config.AutoNumberMarker ||
                    (markerNames[i] != null && markerNames[i].ToLower().Contains("number")))
                {
                    numberColumnIndex = i;
                    break;
                }
            }

            // 4. Сохраняем форматирование строки-образца и удаляем её
            TableRow sampleRow = table.Rows[markerRowIndex].Clone();
            table.Rows.RemoveAt(markerRowIndex);

            // 5. Добавляем строки данных
            int rowNumber = 1;
            foreach (var rowData in tableData)
            {
                TableRow newRow = sampleRow.Clone();

                for (int c = 0; c < markerNames.Count && c < newRow.Cells.Count; c++)
                {
                    TableCell cell = newRow.Cells[c];
                    cell.Paragraphs.Clear();
                    Paragraph para = cell.AddParagraph();

                    if (c == numberColumnIndex)
                    {
                        AddTextWithFont(para, rowNumber.ToString());
                    }
                    else
                    {
                        string markerName = markerNames[c];
                        if (!string.IsNullOrEmpty(markerName) && rowData.ContainsKey(markerName))
                        {
                            AddTextWithFont(para, rowData[markerName]?.ToString() ?? "");
                        }
                    }
                }

                table.Rows.Add(newRow);
                rowNumber++;
            }

            // 6. Добавляем итоговую строку
            if (config.ShowTotalRow && config.SummaryColumns != null && config.SummaryColumns.Count > 0)
            {
                AddTotalRow(table, config, tableData, markerNames, numberColumnIndex);
            }

            table.ApplyStyle(DefaultTableStyle.MediumGrid1);
            FixParagraphIndents(table);
        }

        private void AddTotalRow(Table table, TemplateConfig config, List<Dictionary<string, string>> tableData,
            List<string> markerNames, int numberColumnIndex)
        {
            TableRow totalRow = new TableRow(table.Document);
            for (int i = 0; i < table.Rows[0].Cells.Count; i++)
                totalRow.AddCell();

            for (int c = 0; c < markerNames.Count && c < totalRow.Cells.Count; c++)
            {
                TableCell cell = totalRow.Cells[c];
                Paragraph para = cell.AddParagraph();

                if (c == 0)
                {
                    AddTextWithFont(para, config.TotalRowCaption);
                    SetBold(para);
                }
                else if (c == numberColumnIndex)
                {
                    continue;
                }
                else
                {
                    string markerName = markerNames[c];
                    if (config.SummaryColumns.Contains(markerName))
                    {
                        decimal sum = 0;
                        foreach (var row in tableData)
                        {
                            if (row.ContainsKey(markerName) && decimal.TryParse(row[markerName], out decimal val))
                                sum += val;
                        }
                        AddTextWithFont(para, sum.ToString());
                        SetBold(para);
                    }
                }
            }
            table.Rows.Add(totalRow);
        }

        private void ProcessLayoutTable(Table table, TemplateConfig config)
        {
            var regex = new Regex(@"\[\*([^\]]+)\*\]");

            foreach (TableRow row in table.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    string cellText = cell.Paragraphs[0].Text;
                    var matches = regex.Matches(cellText);

                    foreach (Match match in matches)
                    {
                        string marker = match.Value;
                        string cleanName = match.Groups[1].Value;
                        string replacement = GetValueFromTextMappings(cleanName, config);

                        if (!string.IsNullOrEmpty(replacement))
                        {
                            cell.Paragraphs[0].Text = cellText.Replace(marker, replacement);
                        }
                    }
                }
            }
        }

        private string GetValueFromTextMappings(string cleanName, TemplateConfig config)
        {
            if (config.TextFieldMappings.ContainsKey(cleanName))
            {
                var mapping = config.TextFieldMappings[cleanName];
                if (mapping.IsStatic)
                    return mapping.StaticValue ?? "";
                if (!string.IsNullOrEmpty(mapping.StorageFieldId))
                {
                    if (mapping.StorageFieldId == "OrganizationName")
                        return "Алексеевский КДЦ";
                    if (mapping.StorageFieldId == "Month")
                        return DateTime.Now.ToString("MMMM");
                    if (mapping.StorageFieldId == "Year")
                        return DateTime.Now.Year.ToString();
                }
            }
            return "";
        }

        private void AddTextWithFont(Paragraph paragraph, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextRange textRange = paragraph.AppendText(text);
            textRange.CharacterFormat.FontName = "Times New Roman";
            textRange.CharacterFormat.FontSize = 14;
        }

        private void SetBold(Paragraph paragraph)
        {
            if (paragraph.ChildObjects.Count > 0 && paragraph.ChildObjects[0] is TextRange textRange)
                textRange.CharacterFormat.Bold = true;
        }

        public List<TemplateMarkerInfo> ExtractMarkersWithLocation(string templatePath)
        {
            var markers = new List<TemplateMarkerInfo>();
            Document document = new Document();
            document.LoadFromFile(templatePath);
            var regex = new Regex(@"\[\*([^\]]+)\*\]");

            int tableIndex = 0;
            foreach (Section section in document.Sections)
            {
                foreach (var obj in section.Body.ChildObjects)
                {
                    if (obj is Table table)
                    {
                        foreach (TableRow row in table.Rows)
                        {
                            foreach (TableCell cell in row.Cells)
                            {
                                string cellText = cell.Paragraphs[0].Text;
                                foreach (Match match in regex.Matches(cellText))
                                {
                                    string fullMarker = match.Value;
                                    string cleanName = match.Groups[1].Value.Trim();
                                    if (!markers.Any(m => m.Marker == fullMarker))
                                    {
                                        markers.Add(new TemplateMarkerInfo
                                        {
                                            Marker = fullMarker,
                                            CleanName = cleanName,
                                            IsInTable = true,
                                            TableIndex = tableIndex
                                        });
                                    }
                                }
                            }
                        }
                        tableIndex++;
                    }
                }
            }

            string documentText = document.GetText();
            foreach (Match match in regex.Matches(documentText))
            {
                string fullMarker = match.Value;
                string cleanName = match.Groups[1].Value.Trim();
                if (!markers.Any(m => m.Marker == fullMarker))
                {
                    markers.Add(new TemplateMarkerInfo
                    {
                        Marker = fullMarker,
                        CleanName = cleanName,
                        IsInTable = false,
                        TableIndex = -1
                    });
                }
            }

            return markers;
        }

        public List<TableInfo> GetTableInfos(string templatePath)
        {
            var tables = new List<TableInfo>();
            Document document = new Document();
            document.LoadFromFile(templatePath);

            int index = 0;
            foreach (Section section in document.Sections)
            {
                foreach (var obj in section.Body.ChildObjects)
                {
                    if (obj is Table table)
                    {
                        string firstCellText = table.Rows.Count > 0 && table.Rows[0].Cells.Count > 0
                            ? table.Rows[0].Cells[0].Paragraphs[0].Text.Trim() : "";
                        tables.Add(new TableInfo
                        {
                            Index = index,
                            RowCount = table.Rows.Count,
                            ColCount = table.Rows.Count > 0 ? table.Rows[0].Cells.Count : 0,
                            FirstCellText = firstCellText
                        });
                        index++;
                    }
                }
            }
            return tables;
        }

        private void FixParagraphIndents(Table table)
        {
            foreach (TableRow row in table.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    foreach (Paragraph para in cell.Paragraphs)
                    {
                        para.Format.FirstLineIndent = 0;
                        para.Format.LeftIndent = 0;
                        para.Format.RightIndent = 0;
                    }
                }
            }
        }
    }

    public class TemplateMarkerInfo
    {
        public string Marker { get; set; }
        public string CleanName { get; set; }
        public bool IsInTable { get; set; }
        public int TableIndex { get; set; } = -1;
    }
}