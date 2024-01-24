using System.Globalization;

namespace BetterFinds.Utils
{
    /// <summary>
    /// Provides utility functions for handling currency-related operations.
    /// </summary>
    public class Currency
    {
        /// <summary>
        /// Formats a decimal value as a string with two decimal places.
        /// </summary>
        /// <param name="price">The decimal value to format.</param>
        /// <returns>A string representation of the formatted decimal value.</returns>
        public static string FormatDecimal(decimal price)
        {
            NumberFormatInfo nfi = new()
            {
                NumberDecimalSeparator = "."
            };

            return price.ToString("0.00", nfi);
        }

        /// <summary>
        /// Formats an object representing a decimal value as a string with two decimal places.
        /// If the provided object is null, it defaults to formatting zero.
        /// </summary>
        /// <param name="price">The object representing the decimal value to format.</param>
        /// <returns>A string representation of the formatted decimal value.</returns>
        public static string FormatDecimalObject(object? price) =>
            FormatDecimal((decimal?)price ?? 0);
    }
}