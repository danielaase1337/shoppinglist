using System;

namespace Shared
{
    public static class MealUnitExtensions
    {
        public static string ToNorwegian(this MealUnit unit)
        {
            return unit switch
            {
                MealUnit.Gram             => "gram",
                MealUnit.Kilogram         => "kg",
                MealUnit.Liter            => "liter",
                MealUnit.Deciliter        => "dl",
                MealUnit.Tablespoon       => "ss",
                MealUnit.TablespoonHalf   => "1/2 ss",
                MealUnit.TablespoonQuarter => "1/4 ss",
                MealUnit.Teaspoon         => "ts",
                MealUnit.TeaspoonHalf     => "1/2 ts",
                MealUnit.TeaspoonQuarter  => "1/4 ts",
                MealUnit.Piece            => "stk",
                MealUnit.PieceHalf        => "1/2 stk",
                MealUnit.PieceQuarter     => "1/4 stk",
                MealUnit.Pinch            => "klype",
                MealUnit.Package          => "pakke",
                _                         => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
            };
        }

        /// <summary>
        /// Returns true when the MealUnit and the purchase unit string measure the same physical dimension,
        /// meaning a quantity conversion between them is possible.
        /// Supported: g/kg (weight), dl/l (volume), stk/pakke/pcs/pk (count).
        /// Unknown purchase units → false (caller falls back to raw Math.Ceiling).
        /// </summary>
        public static bool IsCompatibleWith(this MealUnit unit, string purchaseUnit)
        {
            if (string.IsNullOrWhiteSpace(purchaseUnit)) return false;
            var pu = purchaseUnit.Trim().ToLowerInvariant();

            return unit switch
            {
                MealUnit.Gram or MealUnit.Kilogram =>
                    pu is "g" or "gram" or "kg" or "kilogram",

                MealUnit.Liter or MealUnit.Deciliter =>
                    pu is "l" or "liter" or "dl" or "deciliter",

                MealUnit.Piece or MealUnit.PieceHalf or MealUnit.PieceQuarter or MealUnit.Package =>
                    pu is "stk" or "pcs" or "pakke" or "pk",

                _ => false  // tablespoon, teaspoon, pinch — not mapped to purchase units
            };
        }

        /// <summary>
        /// Converts a MealUnit quantity to its base unit:
        ///   weight → grams, volume → decilitres, count → whole pieces.
        /// </summary>
        public static double NormalizeToBaseUnit(this MealUnit unit, double quantity)
        {
            return unit switch
            {
                MealUnit.Gram          => quantity,
                MealUnit.Kilogram      => quantity * 1000,
                MealUnit.Deciliter     => quantity,
                MealUnit.Liter         => quantity * 10,
                MealUnit.Piece         => quantity,
                MealUnit.PieceHalf     => quantity * 0.5,
                MealUnit.PieceQuarter  => quantity * 0.25,
                MealUnit.Package       => quantity,
                _                      => quantity  // unknown units pass through unchanged
            };
        }

        /// <summary>
        /// Converts a purchase unit string quantity to the same base unit used by NormalizeToBaseUnit.
        /// Unknown strings pass through unchanged (caller decides what to do).
        /// </summary>
        public static double NormalizePurchaseUnitToBase(string purchaseUnit, double quantity)
        {
            if (string.IsNullOrWhiteSpace(purchaseUnit)) return quantity;
            return purchaseUnit.Trim().ToLowerInvariant() switch
            {
                "g" or "gram"           => quantity,
                "kg" or "kilogram"      => quantity * 1000,
                "dl" or "deciliter"     => quantity,
                "l" or "liter"          => quantity * 10,
                "stk" or "pcs" or "pakke" or "pk" => quantity,
                _                       => quantity
            };
        }

        /// <summary>
        /// Calculates how many whole packages are needed to satisfy totalDemand.
        /// Returns null when:
        ///   - packageSize is 0 or negative (not configured)
        ///   - packageUnit is null/empty
        ///   - demand and package units are incompatible dimensions
        /// The caller should fall back to <c>(int)Math.Ceiling(totalDemand)</c> on null.
        /// </summary>
        public static int? CalculatePackagesNeeded(
            double totalDemand,
            MealUnit demandUnit,
            double packageSize,
            string packageUnit)
        {
            if (packageSize <= 0) return null;
            if (string.IsNullOrWhiteSpace(packageUnit)) return null;
            if (!demandUnit.IsCompatibleWith(packageUnit)) return null;

            var normalizedDemand  = demandUnit.NormalizeToBaseUnit(totalDemand);
            var normalizedPackage = NormalizePurchaseUnitToBase(packageUnit, packageSize);

            if (normalizedPackage <= 0) return null;

            return (int)Math.Ceiling(normalizedDemand / normalizedPackage);
        }
    }
}
