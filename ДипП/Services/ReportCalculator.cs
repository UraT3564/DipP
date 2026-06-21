using System;
using System.Collections.Generic;
using System.Linq;
using ДипП.Models;

namespace ДипП.Services
{
    public class ReportCalculator
    {
        private bool IsNumericColumn(string columnId, StorageConfig storageConfig)
        {
            if (int.TryParse(columnId, out int id))
            {
                var field = storageConfig.Fields.FirstOrDefault(f => f.Id == id);
                return field?.Type == "number";
            }
            return false;
        }

        public decimal SumColumn(List<Dictionary<string, string>> rows, string columnName, StorageConfig storageConfig)
        {
            if (!IsNumericColumn(columnName, storageConfig)) return 0;

            decimal sum = 0;
            foreach (var row in rows)
            {
                if (row.ContainsKey(columnName) && decimal.TryParse(row[columnName], out decimal value))
                {
                    sum += value;
                }
            }
            return sum;
        }

        public decimal SumColumn(List<Dictionary<string, string>> rows, string columnName)
        {
            decimal sum = 0;
            foreach (var row in rows)
            {
                if (row.ContainsKey(columnName) && decimal.TryParse(row[columnName], out decimal value))
                {
                    sum += value;
                }
            }
            return sum;
        }

        public Dictionary<string, decimal> SumColumns(List<Dictionary<string, string>> rows, List<string> columnNames)
        {
            var result = new Dictionary<string, decimal>();
            foreach (var col in columnNames)
            {
                result[col] = SumColumn(rows, col);
            }
            return result;
        }

        public decimal CalculatePercentage(decimal part, decimal total)
        {
            if (total == 0) return 0;
            return Math.Round(part / total * 100, 1);
        }

        public decimal AverageColumn(List<Dictionary<string, string>> rows, string columnName)
        {
            decimal sum = SumColumn(rows, columnName);
            int count = rows.Count(r => r.ContainsKey(columnName) && decimal.TryParse(r[columnName], out _));
            return count == 0 ? 0 : Math.Round(sum / count, 1);
        }

        public Dictionary<string, string> CreateTotalRow(List<Dictionary<string, string>> rows, List<string> columnsToSum)
        {
            var totalRow = new Dictionary<string, string>();
            var sums = SumColumns(rows, columnsToSum);

            foreach (var col in columnsToSum)
            {
                totalRow[col] = sums[col].ToString();
            }

            return totalRow;
        }

        public Dictionary<string, Dictionary<string, decimal>> GroupByWithSum(
            List<Dictionary<string, string>> rows,
            string groupByColumn,
            List<string> sumColumns)
        {
            var result = new Dictionary<string, Dictionary<string, decimal>>();

            foreach (var row in rows)
            {
                if (!row.ContainsKey(groupByColumn)) continue;

                string groupKey = row[groupByColumn];

                if (!result.ContainsKey(groupKey))
                {
                    result[groupKey] = new Dictionary<string, decimal>();
                    foreach (var col in sumColumns)
                    {
                        result[groupKey][col] = 0;
                    }
                }

                foreach (var col in sumColumns)
                {
                    if (row.ContainsKey(col) && decimal.TryParse(row[col], out decimal value))
                    {
                        result[groupKey][col] += value;
                    }
                }
            }

            return result;
        }

        public string GetStatisticsText(List<Dictionary<string, string>> rows, List<string> summaryColumns, StorageConfig storageConfig)
        {
            if (rows.Count == 0) return "Нет данных для статистики";

            var sums = SumColumns(rows, summaryColumns);
            decimal total = summaryColumns.Count > 0 ? sums.Values.Sum() : 0;

            var result = new System.Text.StringBuilder();
            result.AppendLine("Статистика:");
            result.AppendLine($"Всего строк: {rows.Count}");

            foreach (var col in summaryColumns)
            {
                decimal colSum = sums[col];
                decimal percent = CalculatePercentage(colSum, total);
                result.AppendLine($"{GetDisplayName(col, storageConfig)}: {colSum} ({percent}%)");
            }

            return result.ToString();
        }

        private string GetDisplayName(string columnId, StorageConfig storageConfig)
        {
            if (int.TryParse(columnId, out int id))
            {
                var field = storageConfig.Fields.FirstOrDefault(f => f.Id == id);
                return field?.Display ?? columnId;
            }
            return columnId;
        }
    }
}