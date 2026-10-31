using System;
using System.Globalization;
using UnityEngine;

namespace GameUP.Contracts
{
    /// <summary>
    /// Oyun içi tarih modeli (ör. 01.11.2026).
    /// IMPLEMENTATION.md Bölüm 9: ExpiryDate &lt; GameDate ise ürün geçersizdir;
    /// ExpiryDate == GameDate ise geçerlidir (tarih sınırı kuralı).
    /// </summary>
    [Serializable]
    public struct GameDate : IComparable<GameDate>, IEquatable<GameDate>
    {
        [Range(1, 31)] public int Day;
        [Range(1, 12)] public int Month;
        [Range(2000, 2100)] public int Year;

        public GameDate(int day, int month, int year)
        {
            Day = day;
            Month = month;
            Year = year;
        }

        public bool IsValid => Year >= 2000 && Month >= 1 && Month <= 12 && Day >= 1 && Day <= 31;

        public int CompareTo(GameDate other)
        {
            int yearComparison = Year.CompareTo(other.Year);
            if (yearComparison != 0) return yearComparison;

            int monthComparison = Month.CompareTo(other.Month);
            if (monthComparison != 0) return monthComparison;

            return Day.CompareTo(other.Day);
        }

        public bool Equals(GameDate other)
        {
            return Day == other.Day && Month == other.Month && Year == other.Year;
        }

        public override bool Equals(object obj)
        {
            return obj is GameDate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Day, Month, Year);
        }

        public static bool operator ==(GameDate left, GameDate right) => left.Equals(right);
        public static bool operator !=(GameDate left, GameDate right) => !left.Equals(right);
        public static bool operator <(GameDate left, GameDate right) => left.CompareTo(right) < 0;
        public static bool operator <=(GameDate left, GameDate right) => left.CompareTo(right) <= 0;
        public static bool operator >(GameDate left, GameDate right) => left.CompareTo(right) > 0;
        public static bool operator >=(GameDate left, GameDate right) => left.CompareTo(right) >= 0;

        public override string ToString()
        {
            return $"{Day:00}.{Month:00}.{Year:0000}";
        }

        public static GameDate Parse(string dateText)
        {
            if (TryParse(dateText, out GameDate result))
            {
                return result;
            }
            throw new FormatException($"Geçersiz GameDate formatı: '{dateText}'. Beklenen: GG.AA.YYYY");
        }

        public static bool TryParse(string dateText, out GameDate result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(dateText))
                return false;

            string[] parts = dateText.Trim().Split('.', '/', '-');
            if (parts.Length != 3)
                return false;

            if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) &&
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
            {
                result = new GameDate(day, month, year);
                return result.IsValid;
            }

            return false;
        }
    }
}

