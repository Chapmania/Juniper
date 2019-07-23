namespace Juniper.Units
{
    /// <summary>
    /// Conversions from tons
    /// </summary>
    public static class Tons
    {
        /// <summary>
        /// Conversion factor from grams to tons.
        /// </summary>
        public const float PER_GRAM = PER_KILOGRAM * Units.Kilograms.PER_GRAM;

        /// <summary>
        /// Conversion factor from ounces to tons.
        /// </summary>
        public const float PER_OUNCE = 1 / Units.Ounces.PER_TON;

        /// <summary>
        /// Conversion factor from pounds to tons.
        /// </summary>
        public const float PER_POUND = 1 / Units.Pounds.PER_TON;

        /// <summary>
        /// Conversion factor from kilograms to tons.
        /// </summary>
        public const float PER_KILOGRAM = 1 / Units.Kilograms.PER_TON;

        /// <summary>
        /// Convert from tons to grams.
        /// </summary>
        /// <param name="tons">The number of tons</param>
        /// <returns>The number of grams</returns>
        public static float Grams(float tons)
        {
            return tons * Units.Grams.PER_TON;
        }

        /// <summary>
        /// Convert from tons to ounces.
        /// </summary>
        /// <param name="tons">The number of tons</param>
        /// <returns>The number of ounces</returns>
        public static float Ounces(float tons)
        {
            return tons * Units.Ounces.PER_TON;
        }

        /// <summary>
        /// Convert from tons to pounds.
        /// </summary>
        /// <param name="tons">The number of tons</param>
        /// <returns>The number of pounds</returns>
        public static float Pounds(float tons)
        {
            return tons * Units.Pounds.PER_TON;
        }

        /// <summary>
        /// Convert from tons to kilograms.
        /// </summary>
        /// <param name="tons">The number of tons</param>
        /// <returns>The number of kilograms</returns>
        public static float Kilograms(float tons)
        {
            return tons * Units.Kilograms.PER_TON;
        }
    }
}