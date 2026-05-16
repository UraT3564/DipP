using System;

namespace ДипП.Services
{
    public static class DateHelper
    {
        
        /// Форматирует значение даты согласно выбранному формату
        
        /// <param name="value">Строковое значение даты</param>
        /// <param name="format">Формат: "ДД.ММ.ГГГГ", "ММ.ГГГГ", "ГГГГ", "Месяц (словом)"</param>
        /// <returns>Отформатированная строка или исходное значение, если не удалось распарсить</returns>
        public static string FormatDateValue(string value, string format)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (DateTime.TryParse(value, out DateTime date))
            {
                switch (format)
                {
                    case "ДД.ММ.ГГГГ":
                        return date.ToString("dd.MM.yyyy");
                    case "ММ.ГГГГ":
                        return date.ToString("MM.yyyy");
                    case "ГГГГ":
                        return date.Year.ToString();
                    case "Месяц (словом)":
                        return date.ToString("MMMM");
                    default:
                        return value;
                }
            }
            return value;
        }

        /// Получает только год из даты
        public static string GetYear(string value)
        {
            return FormatDateValue(value, "ГГГГ");
        }

        /// Получает только месяц (словом) из даты
        public static string GetMonthName(string value)
        {
            return FormatDateValue(value, "Месяц (словом)");
        }
    }
}