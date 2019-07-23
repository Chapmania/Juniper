namespace Juniper.Units
{
    /// <summary>
    /// Conversions from kilopascals
    /// </summary>
    public static class Kilopascals
    {
        /// <summary>
        /// Conversion factor from pascals to kilopascals.
        /// </summary>
        public const float PER_PASCAL = 1 / Units.Pascals.PER_KILOPASCAL;

        /// <summary>
        /// Conversion factor from hectopascals to kilopascals.
        /// </summary>
        public const float PER_HECTOPASCAL = 1 / Units.Hectopascals.PER_KILOPASCAL;

        /// <summary>
        /// Conversion factor from millibars to kilopascals.
        /// </summary>
        public const float PER_MILLIBAR = PER_HECTOPASCAL;

        /// <summary>
        /// Conversion factor from pounds per square inch to kilopascals.
        /// </summary>
        public const float PER_POUND_PER_SQUARE_INCH = PER_HECTOPASCAL * Units.Hectopascals.PER_POUND_PER_SQUARE_INCH;

        /// <summary>
        /// Convert from kilopascals to pascals.
        /// </summary>
        /// <param name="kilopascals">The number of kilopascals</param>
        /// <returns>The number of pascals</returns>
        public static float Pascals(float kilopascals)
        {
            return kilopascals * Units.Pascals.PER_KILOPASCAL;
        }

        /// <summary>
        /// Convert from kilopascals to hectopascals.
        /// </summary>
        /// <param name="kilopascals">The number of kilopascals</param>
        /// <returns>The number of hectopascals</returns>
        public static float Hectopascals(float kilopascals)
        {
            return kilopascals * Units.Hectopascals.PER_KILOPASCAL;
        }

        /// <summary>
        /// Convert from kilopascals to millibars.
        /// </summary>
        /// <param name="kilopascals">The number of kilopascals</param>
        /// <returns>The number of millibars</returns>
        public static float Millibars(float kilopascals)
        {
            return kilopascals * Units.Millibars.PER_KILOPASCAL;
        }

        /// <summary>
        /// Convert from kilopascals to pounds per square inch.
        /// </summary>
        /// <param name="kilopascals">The number of kilopascals</param>
        /// <returns>The number of pounds per square inch</returns>
        public static float PoundsPerSquareInch(float kilopascals)
        {
            return kilopascals * Units.PoundsPerSquareInch.PER_KILOPASCAL;
        }
    }
}