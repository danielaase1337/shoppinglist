using Shared;
using Xunit;

namespace Api.Tests
{
    /// <summary>
    /// Unit tests for MealUnitExtensions package-size calculation methods.
    /// These tests exercise IsCompatibleWith, NormalizeToBaseUnit,
    /// NormalizePurchaseUnitToBase, and CalculatePackagesNeeded in isolation.
    /// </summary>
    public class MealUnitExtensionsTests
    {
        // ── CalculatePackagesNeeded — compatible units ────────────────────────────

        [Fact]
        public void CalculatePackagesNeeded_ExactFit_Returns1Package()
        {
            // 400 g demand, 400 g package → exactly 1 package
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 400, "g");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_DemandLessThanPackage_Returns1Package()
        {
            // 400 g demand, 500 g package → 1 package (can't buy 0.8 of a bag)
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 500, "g");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_DemandExceedsOnePackage_RoundsUp()
        {
            // 600 g demand, 500 g package → ceil(600/500) = ceil(1.2) = 2 packages
            var result = MealUnitExtensions.CalculatePackagesNeeded(600, MealUnit.Gram, 500, "g");
            Assert.Equal(2, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_CrossWeightUnits_KgDemandGramPackage()
        {
            // 1.5 kg demand, 500 g package → normalize: 1500 g / 500 g = 3 packages
            var result = MealUnitExtensions.CalculatePackagesNeeded(1.5, MealUnit.Kilogram, 500, "g");
            Assert.Equal(3, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_CrossWeightUnits_GramDemandKgPackage()
        {
            // 400 g demand, 1 kg package → 400 g / 1000 g = 0.4 → ceil = 1 package
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 1, "kg");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_VolumeUnits_LiterDemandDlPackage()
        {
            // 0.6 l demand, 5 dl package → normalize: 6 dl / 5 dl = 1.2 → ceil = 2 packages
            var result = MealUnitExtensions.CalculatePackagesNeeded(0.6, MealUnit.Liter, 5, "dl");
            Assert.Equal(2, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_PieceUnits_StkPackage()
        {
            // 5 pieces demand, 3-piece pack → ceil(5/3) = 2 packages
            var result = MealUnitExtensions.CalculatePackagesNeeded(5, MealUnit.Piece, 3, "stk");
            Assert.Equal(2, result);
        }

        [Fact]
        public void CalculatePackagesNeeded_PakkePackageUnit_Works()
        {
            // 4 pieces demand, 2-piece "pakke" → 2 packages
            var result = MealUnitExtensions.CalculatePackagesNeeded(4, MealUnit.Piece, 2, "pakke");
            Assert.Equal(2, result);
        }

        // ── CalculatePackagesNeeded — fallback cases (must return null) ───────────

        [Fact]
        public void CalculatePackagesNeeded_ZeroPackageSize_ReturnsNull()
        {
            // packageSize = 0 → not configured, fallback expected
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 0, "g");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_NegativePackageSize_ReturnsNull()
        {
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, -100, "g");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_NullPackageUnit_ReturnsNull()
        {
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 500, null);
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_EmptyPackageUnit_ReturnsNull()
        {
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 500, "");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_UnknownPackageUnit_ReturnsNull()
        {
            // "cups" is not a supported purchase unit — return null so caller falls back
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 500, "cups");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_IncompatibleUnits_WeightDemandVolumePackage_ReturnsNull()
        {
            // Gram demand vs liter package — incompatible dimensions
            var result = MealUnitExtensions.CalculatePackagesNeeded(400, MealUnit.Gram, 1, "l");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_IncompatibleUnits_VolumeVsCount_ReturnsNull()
        {
            // Deciliter demand vs stk package — incompatible
            var result = MealUnitExtensions.CalculatePackagesNeeded(3, MealUnit.Deciliter, 1, "stk");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_TablespoonDemand_ReturnsNull()
        {
            // Tablespoon has no compatible purchase unit dimension → always null
            var result = MealUnitExtensions.CalculatePackagesNeeded(3, MealUnit.Tablespoon, 500, "g");
            Assert.Null(result);
        }

        [Fact]
        public void CalculatePackagesNeeded_PinchDemand_ReturnsNull()
        {
            // Pinch has no compatible purchase unit → always null
            var result = MealUnitExtensions.CalculatePackagesNeeded(1, MealUnit.Pinch, 100, "g");
            Assert.Null(result);
        }

        // ── IsCompatibleWith — spot checks ───────────────────────────────────────

        [Theory]
        [InlineData(MealUnit.Gram, "g", true)]
        [InlineData(MealUnit.Gram, "kg", true)]
        [InlineData(MealUnit.Gram, "gram", true)]
        [InlineData(MealUnit.Kilogram, "kg", true)]
        [InlineData(MealUnit.Kilogram, "g", true)]
        [InlineData(MealUnit.Gram, "l", false)]
        [InlineData(MealUnit.Gram, "stk", false)]
        [InlineData(MealUnit.Liter, "l", true)]
        [InlineData(MealUnit.Liter, "dl", true)]
        [InlineData(MealUnit.Deciliter, "liter", true)]
        [InlineData(MealUnit.Liter, "g", false)]
        [InlineData(MealUnit.Piece, "stk", true)]
        [InlineData(MealUnit.Piece, "pcs", true)]
        [InlineData(MealUnit.Piece, "pakke", true)]
        [InlineData(MealUnit.Piece, "pk", true)]
        [InlineData(MealUnit.Piece, "g", false)]
        [InlineData(MealUnit.Tablespoon, "g", false)]
        [InlineData(MealUnit.Teaspoon, "l", false)]
        [InlineData(MealUnit.Pinch, "stk", false)]
        public void IsCompatibleWith_ReturnsExpected(MealUnit unit, string purchaseUnit, bool expected)
        {
            Assert.Equal(expected, unit.IsCompatibleWith(purchaseUnit));
        }

        // ── NormalizeToBaseUnit — spot checks ─────────────────────────────────────

        [Theory]
        [InlineData(MealUnit.Gram, 500, 500)]
        [InlineData(MealUnit.Kilogram, 1.5, 1500)]
        [InlineData(MealUnit.Deciliter, 3, 3)]
        [InlineData(MealUnit.Liter, 0.5, 5)]
        [InlineData(MealUnit.Piece, 4, 4)]
        [InlineData(MealUnit.PieceHalf, 2, 1)]
        [InlineData(MealUnit.PieceQuarter, 4, 1)]
        [InlineData(MealUnit.Package, 3, 3)]
        public void NormalizeToBaseUnit_ReturnsExpected(MealUnit unit, double quantity, double expected)
        {
            Assert.Equal(expected, unit.NormalizeToBaseUnit(quantity), precision: 6);
        }
    }
}
