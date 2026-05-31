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
        /// Converts a quantity in the given MealUnit to the canonical base unit for its dimension:
        /// Weight → grams, Volume → deciliters, Count → pieces.
        /// </summary>
        public static double NormalizeToBaseUnit(this MealUnit unit, double quantity)
        {
            return unit switch
            {
                MealUnit.Gram             => quantity,
                MealUnit.Kilogram         => quantity * 1000,
                MealUnit.Deciliter        => quantity,
                MealUnit.Liter            => quantity * 10,
                MealUnit.Piece            => quantity,
                MealUnit.PieceHalf        => quantity * 0.5,
                MealUnit.PieceQuarter     => quantity * 0.25,
                MealUnit.Package          => quantity,
                _                         => double.NaN  // unmappable units (Tablespoon, Teaspoon, Pinch, …)
            };
        }

        /// <summary>
        /// Converts a quantity expressed in the canonical base unit back to the given MealUnit.
        /// Weight base = grams, Volume base = deciliters, Count base = pieces.
        /// Returns double.NaN for unmappable units (Tablespoon, Teaspoon, Pinch, …).
        /// </summary>
        public static double FromBaseUnit(this MealUnit unit, double baseQuantity)
        {
            return unit switch
            {
                MealUnit.Gram             => baseQuantity,
                MealUnit.Kilogram         => baseQuantity / 1000.0,
                MealUnit.Deciliter        => baseQuantity,
                MealUnit.Liter            => baseQuantity / 10.0,
                MealUnit.Piece            => baseQuantity,
                MealUnit.PieceHalf        => baseQuantity / 0.5,
                MealUnit.PieceQuarter     => baseQuantity / 0.25,
                MealUnit.Package          => baseQuantity,
                _                         => double.NaN
            };
        }
 
        /// <summary>
        /// Converts a purchase quantity expressed in a string purchase unit to the canonical base:
        /// g/gram → grams, kg → grams, dl → deciliters, l/liter → deciliters, stk/pcs/pakke/pk → count.
        /// Returns double.NaN for unknown units.
        /// </summary>
        public static double NormalizePurchaseUnitToBase(double quantity, string purchaseUnit)
        {
            if (string.IsNullOrWhiteSpace(purchaseUnit)) return double.NaN;

            return purchaseUnit.ToLowerInvariant().Trim() switch
            {
                "g" or "gram"             => quantity,
                "kg"                      => quantity * 1000,
                "dl" or "deciliter"       => quantity,
                "l" or "liter"            => quantity * 10,
                "stk" or "pcs"
                    or "pakke" or "pk"    => quantity,
                _                         => double.NaN
            };
        }

        /// <summary>
        /// Returns true when the MealUnit dimension is compatible with the given purchase unit string.
        /// </summary>
        public static bool IsCompatibleWith(this MealUnit unit, string purchaseUnit)
        {
            if (string.IsNullOrWhiteSpace(purchaseUnit)) return false;

            var pu = purchaseUnit.ToLowerInvariant().Trim();
            var isWeight  = pu is "g" or "gram" or "kg";
            var isVolume  = pu is "dl" or "deciliter" or "l" or "liter";
            var isCount   = pu is "stk" or "pcs" or "pakke" or "pk";

            return unit switch
            {
                MealUnit.Gram or MealUnit.Kilogram                        => isWeight,
                MealUnit.Deciliter or MealUnit.Liter                      => isVolume,
                MealUnit.Piece or MealUnit.PieceHalf
                    or MealUnit.PieceQuarter or MealUnit.Package           => isCount,
                _                                                          => false
            };
        }

        /// <summary>
        /// Calculates how many purchase packages are needed to cover <paramref name="quantity"/> of
        /// <paramref name="unit"/>. Returns null when conversion is not possible (incompatible units,
        /// unknown purchase unit, or invalid package size).
        /// </summary>
        public static int? CalculatePackagesNeeded(
            double quantity, MealUnit unit,
            double packageSize, string packageUnit)
        {
            if (packageSize <= 0) return null;
            if (string.IsNullOrWhiteSpace(packageUnit)) return null;
            if (!unit.IsCompatibleWith(packageUnit)) return null;

            var demandInBase  = unit.NormalizeToBaseUnit(quantity);
            var packageInBase = NormalizePurchaseUnitToBase(packageSize, packageUnit);

            if (double.IsNaN(demandInBase) || double.IsNaN(packageInBase) || packageInBase <= 0)
                return null;

            return (int)Math.Ceiling(demandInBase / packageInBase);
        }
    }
}
