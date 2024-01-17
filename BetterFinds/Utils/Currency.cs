using System.Globalization;

namespace BetterFinds.Utils
{
    public class Currency
    {
        public static string FormatDecimal(decimal price)
        {
            NumberFormatInfo nfi = new()
            {
                NumberDecimalSeparator = "."
            };

            return price.ToString("0.00", nfi);
        }

        public static string FormatDecimalObject(object? price)
        {
            return FormatDecimal((decimal?)price ?? 0);
        }
    }
}
