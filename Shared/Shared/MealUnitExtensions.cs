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
    }
}
