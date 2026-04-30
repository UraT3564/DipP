using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using ДипП.Models;
using System.Drawing;

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
        public string GenerateTestReport(TemplateConfig config, Dictionary<string, string> simpleFields,List<Dictionary<string, string>> tableData)
        {
            try
            {

                // 2. Пути к файлам, название
                string orgName = simpleFields.ContainsKey("OrganizationName") ? simpleFields["OrganizationName"] : "отчет";
                string month = simpleFields.ContainsKey("Month") ? simpleFields["Month"] : DateTime.Now.ToString("MMMM");
                string year = simpleFields.ContainsKey("Year") ? simpleFields["Year"] : DateTime.Now.Year.ToString();

                // Очищаем название организации от недопустимых символов
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    orgName = orgName.Replace(c, '_');
                }

                // Укорачиваем, если слишком длинное
                if (orgName.Length > 50)
                    orgName = orgName.Substring(0, 50) + "...";

                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res", "Templates", config.TemplateFile);
                string outputFileName = $"Отчет_{orgName}_{month}_{year}.docx";
                string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outputFileName);

                Console.WriteLine($"[DocumentService] Шаблон: {templatePath}");
                Console.WriteLine($"[DocumentService] Выходной файл: {outputPath}");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Шаблон не найден: {templatePath}");
                }

                // 3. Загрузка шаблона
                Document document = new Document();
                document.LoadFromFile(templatePath);
                Console.WriteLine($"[DocumentService] Шаблон загружен");

                // 4. Замена простых полей
                Console.WriteLine("[DocumentService] Заменяем простые поля...");
                foreach (var field in simpleFields)
                {
                    string value = field.Value;

                    // Специальная обработка для месяца
                    if (field.Key == "Month")
                    {
                        if (_monthTranslations.ContainsKey(value))
                        {
                            string translatedMonth = _monthTranslations[value];
                            Console.WriteLine($"  - Перевод месяца: '{value}' → '{translatedMonth}'");
                            value = translatedMonth;
                        }
                    }

                    string placeholder = "{" + field.Key + "}";
                    string placeholderWithSpace = "{ " + field.Key + "}";
                    string placeholderLower = "{" + field.Key.ToLower() + "}";
                    string placeholderUpper = "{" + field.Key.ToUpper() + "}";

                    int count = 0;
                    count += document.Replace(placeholder, value, false, true);
                    count += document.Replace(placeholderWithSpace, value, false, true);
                    count += document.Replace(placeholderLower, value, false, true);
                    count += document.Replace(placeholderUpper, value, false, true);

                    if (count > 0)
                    {
                        Console.WriteLine($"  ✓ {field.Key}: {value} ({count} замен)");
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ {field.Key}: не найден");
                    }
                }

                // 5. Работа с таблицей
                Console.WriteLine("[DocumentService] Ищем таблицу в документе...");
                Table table = FindAndPrepareTable(document);

                if (table != null)
                {
                    Console.WriteLine("[DocumentService] Найдена таблица, обрабатываем...");
                    ProcessTable(table, tableData, config.TableColumns, config);
                }
                else
                {
                    Console.WriteLine("[DocumentService] Таблица не найдена в шаблоне");
                }


                // 6. Сохранение
                document.SaveToFile(outputPath, FileFormat.Docx);
                Console.WriteLine($"[DocumentService] Документ сохранен: {outputPath}");

                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentService] Ошибка: {ex.Message}");
                Console.WriteLine($"[DocumentService] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ProcessTable(Table table, List<Dictionary<string, string>> tableData, List<string> columnOrder, TemplateConfig config)
        {
            if (table == null)
            {
                Console.WriteLine("[DocumentService] Таблица не передана для обработки");
                return;
            }

            Console.WriteLine($"[DocumentService] Обработка таблицы: {table.Rows.Count} строк, {table.Rows[0].Cells.Count} колонок");

            // Проверяем, есть ли в таблице заголовки
            bool hasHeaders = false;
            if (table.Rows.Count > 0)
            {
                // Проверяем, содержит ли первая строка заголовки таблицы
                TableRow firstRow = table.Rows[0];
                foreach (TableCell cell in firstRow.Cells)
                {
                    if (cell.Paragraphs.Count > 0)
                    {
                        string cellText = cell.Paragraphs[0].Text.Trim();
                        if (!string.IsNullOrEmpty(cellText) &&
                            (cellText.Contains("№") ||
                             cellText.Contains("Мероприятия") ||
                             cellText.Contains("дата") ||
                             cellText.Contains("Ссылка") ||
                             cellText.Contains("Участники") ||
                             cellText.Contains("место") ||
                             cellText.Contains("Ответственные")))
                        {
                            hasHeaders = true;
                            break;
                        }
                    }
                }
            }

            if (!hasHeaders)
            {
                Console.WriteLine("[DocumentService] ВНИМАНИЕ: В таблице не найдены заголовки!");
                Console.WriteLine("[DocumentService] Проверьте, что заголовки находятся в первой строке таблицы");

                // Создаем заголовки, если их нет
                CreateTableHeaders(table, columnOrder);
                hasHeaders = true;
            }

            // Удаляем строки с данными (все кроме заголовков)
            Console.WriteLine("[DocumentService] Удаляем старые строки данных...");
            while (table.Rows.Count > 1) // Оставляем только первую строку (заголовки)
            {
                table.Rows.RemoveAt(1);
            }

            // Добавляем новые строки с данными
            Console.WriteLine("[DocumentService] Добавляем строки с данными...");
            for (int i = 0; i < tableData.Count; i++)
            {
                AddDataRowToTable(table, tableData[i], columnOrder, i + 1);
            }

            if (config.SummaryColumns != null && config.SummaryColumns.Count > 0 && tableData.Count > 0)
            {
                var totals = CalculateTotals(tableData, config.SummaryColumns);
                AddTotalRowToTable(table, totals, columnOrder);
            }

            Console.WriteLine($"[DocumentService] Обработка завершена. Таблица: {table.Rows.Count} строк");
        }

        private void CreateTableHeaders(Table table, List<string> columnOrder)
        {
            Console.WriteLine("[DocumentService] Создаем заголовки таблицы...");

            // Если в таблице нет строк, создаем новую
            if (table.Rows.Count == 0)
            {
                table.Rows.Add(new TableRow(table.Document));
            }

            TableRow headerRow = table.Rows[0];

            // Маппинг названий колонок на русские заголовки
            Dictionary<string, string> russianHeaders = new Dictionary<string, string>
            {
                ["Number"] = "№",
                ["EventName"] = "Мероприятия",
                ["EventDate"] = "дата",
                ["EventLink"] = "Ссылка",
                ["Participants"] = "Участники",
                ["Location"] = "место",
                ["Responsible"] = "Ответственные"
            };

            // Очищаем существующие ячейки
            headerRow.Cells.Clear();

            // Создаем ячейки с заголовками
            foreach (string column in columnOrder)
            {
                TableCell cell = headerRow.AddCell();
                Paragraph para = cell.AddParagraph();

                string headerText = russianHeaders.ContainsKey(column) ? russianHeaders[column] : column;
                AddTextWithFont(para, headerText);

                // Делаем заголовки жирными
                if (para.ChildObjects.Count > 0 && para.ChildObjects[0] is TextRange textRange)
                {
                    textRange.CharacterFormat.Bold = true;
                }

                Console.WriteLine($"  - Добавлен заголовок: '{headerText}'");
            }
        }

        private void AddDataRowToTable(Table table, Dictionary<string, string> rowData, List<string> columnOrder, int rowNumber)
        {
            TableRow newRow = new TableRow(table.Document);

            // Добавляем ячейки
            for (int i = 0; i < table.Rows[0].Cells.Count; i++)
            {
                newRow.AddCell();
            }

            // Заполняем ячейки данными
            for (int colIndex = 0; colIndex < Math.Min(columnOrder.Count, newRow.Cells.Count); colIndex++)
            {
                string columnName = columnOrder[colIndex];
                TableCell cell = newRow.Cells[colIndex];
                Paragraph para = cell.AddParagraph();

                // Проверяем, содержит ли название колонки слово Number
                if (columnName.IndexOf("Number", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Это колонка для номера - вставляем номер строки
                    AddTextWithFont(para, rowNumber.ToString());
                }
                else if (rowData.ContainsKey(columnName))
                {
                    // Обычная колонка с данными
                    if (columnName.IndexOf("Link", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string url = rowData[columnName];
                        if (!string.IsNullOrEmpty(url))
                        {
                            AddHyperlinkWithFont(para, url, "Перейти");
                        }
                        else
                        {
                            AddTextWithFont(para, "");
                        }
                    }
                    else
                    {
                        AddTextWithFont(para, rowData[columnName]);
                    }
                }

                Console.WriteLine($"  - Строка {rowNumber}, колонка '{columnName}': {(rowData.ContainsKey(columnName) ? rowData[columnName] : "номер")}");
            }
            table.Rows.Add(newRow);
        }

        public bool CheckTemplate(string templatePath)
        {
            try
            {
                Document doc = new Document();
                doc.LoadFromFile(templatePath);

                string content = doc.GetText();
                var matches = System.Text.RegularExpressions.Regex.Matches(content, @"\{([^}]+)\}");

                Console.WriteLine($"[DocumentService] Найдено полей в шаблоне: {matches.Count}");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    Console.WriteLine($"  - {match.Value}");
                }

                // Ищем таблицы
                int tableCount = 0;
                foreach (Section section in doc.Sections)
                {
                    foreach (var obj in section.Body.ChildObjects)
                    {
                        if (obj is Table)
                            tableCount++;
                    }
                }

                Console.WriteLine($"[DocumentService] Найдено таблиц: {tableCount}");

                if (tableCount > 0)
                {
                    // Получаем первую таблицу
                    Table firstTable = null;
                    foreach (Section section in doc.Sections)
                    {
                        foreach (var obj in section.Body.ChildObjects)
                        {
                            if (obj is Table table)
                            {
                                firstTable = table;
                                break;
                            }
                        }
                        if (firstTable != null) break;
                    }

                    if (firstTable != null)
                    {
                        Console.WriteLine($"[DocumentService] Первая таблица: {firstTable.Rows.Count} строк, {firstTable.Rows[0].Cells.Count} колонок");

                        // Проверяем первые 3 строки
                        for (int i = 0; i < Math.Min(3, firstTable.Rows.Count); i++)
                        {
                            Console.WriteLine($"  Строка {i + 1}: {firstTable.Rows[i].Cells.Count} ячеек");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentService] Ошибка проверки шаблона: {ex.Message}");
                return false;
            }
        }

        private TextRange AddTextWithFont(Paragraph paragraph, string text)
        {
            TextRange textRange = paragraph.AppendText(text);
            textRange.CharacterFormat.FontName = "Times New Roman";
            textRange.CharacterFormat.FontSize = 14;
            return textRange;
        }

        private Dictionary<string, string> CalculateTotals(List<Dictionary<string, string>> rows, List<string> columnsToSum)
        {
            var totals = new Dictionary<string, string>();

            foreach (var col in columnsToSum)
            {
                decimal sum = 0;
                foreach (var row in rows)
                {
                    if (row.ContainsKey(col) && decimal.TryParse(row[col], out decimal value))
                    {
                        sum += value;
                    }
                }
                totals[col] = sum.ToString();
            }

            return totals;
        }

        private void AddTotalRowToTable(Table table, Dictionary<string, string> totals, List<string> columnOrder)
        {
            TableRow totalRow = new TableRow(table.Document);

            // Добавляем ячейки
            for (int i = 0; i < table.Rows[0].Cells.Count; i++)
            {
                totalRow.AddCell();
            }

            // Заполняем ячейки
            for (int colIndex = 0; colIndex < Math.Min(columnOrder.Count, totalRow.Cells.Count); colIndex++)
            {
                string columnName = columnOrder[colIndex];
                TableCell cell = totalRow.Cells[colIndex];
                Paragraph para = cell.AddParagraph();

                if (colIndex == 0)
                {
                    // Делаем жирным
                    if (para.ChildObjects.Count > 0 && para.ChildObjects[0] is TextRange textRange)
                    {
                        textRange.CharacterFormat.Bold = true;
                    }
                }
                else if (totals.ContainsKey(columnName))
                {
                    AddTextWithFont(para, totals[columnName]);
                    // Делаем жирным
                    if (para.ChildObjects.Count > 0 && para.ChildObjects[0] is TextRange textRange)
                    {
                        textRange.CharacterFormat.Bold = true;
                    }
                }
            }

            table.Rows.Add(totalRow);
        }
        
        private Table FindAndPrepareTable(Document document)
        {
            Console.WriteLine("[DocumentService] Ищем таблицу в документе...");

            foreach (Section section in document.Sections)
            {
                foreach (var obj in section.Body.ChildObjects)
                {
                    if (obj is Table table)
                    {
                        Console.WriteLine($"[DocumentService] Найдена таблица: {table.Rows.Count} строк");

                        // Проверяем структуру таблицы
                        if (table.Rows.Count > 0)
                        {
                            Console.WriteLine("[DocumentService] Заголовки таблицы:");
                            for (int i = 0; i < Math.Min(3, table.Rows.Count); i++)
                            {
                                Console.WriteLine($"  Строка {i + 1}: {table.Rows[i].Cells.Count} ячеек");

                                // Выводим текст из первых ячеек для диагностики
                                if (table.Rows[i].Cells.Count > 0 && table.Rows[i].Cells[0].Paragraphs.Count > 0)
                                {
                                    string text = table.Rows[i].Cells[0].Paragraphs[0].Text;
                                    Console.WriteLine($"    Первая ячейка: '{text}'");
                                }
                            }
                        }

                        return table;
                    }
                }
            }

            Console.WriteLine("[DocumentService] Таблица не найдена!");
            return null;
        }

        private void AddHyperlinkWithFont(Paragraph paragraph, string url, string displayText = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                // Если ссылки нет, добавляем пустой текст
                AddTextWithFont(paragraph, "");
                return;
            }

            try
            {
                // Используем displayText или сам url
                string text = string.IsNullOrEmpty(displayText) ? url : displayText;

                // В Spire.Doc AppendHyperlink возвращает Field
                Field field = paragraph.AppendHyperlink(url, text, HyperlinkType.WebLink);

                // Настраиваем шрифт для всего параграфа или для содержимого поля
                foreach (DocumentObject obj in field.ChildObjects)
                {
                    if (obj is TextRange textRange)
                    {
                        textRange.CharacterFormat.FontName = "Times New Roman";
                        textRange.CharacterFormat.FontSize = 14;
                        textRange.CharacterFormat.TextColor = Color.Blue;
                        textRange.CharacterFormat.UnderlineStyle = UnderlineStyle.Single;
                    }
                }

                Console.WriteLine($"[DocumentService] Добавлена гиперссылка: {text} -> {url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocumentService] Ошибка создания гиперссылки: {ex.Message}");
                // Если не получилось, вставляем как текст
                AddTextWithFont(paragraph, url);
            }
        }
    }
}