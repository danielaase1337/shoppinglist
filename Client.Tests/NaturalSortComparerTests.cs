using BlazorApp.Client.Common;
using Xunit;

namespace Client.Tests
{
    public class NaturalSortComparerTests
    {
        private readonly NaturalSortComparer _comparer;

        public NaturalSortComparerTests()
        {
            _comparer = new NaturalSortComparer();
        }

        [Theory]
        [InlineData("Uke 1", "Uke 2", -1)] // 1 comes before 2
        [InlineData("Uke 2", "Uke 1", 1)]  // 2 comes after 1
        [InlineData("Uke 10", "Uke 2", 1)] // 10 comes after 2
        [InlineData("Uke 2", "Uke 10", -1)] // 2 comes before 10
        [InlineData("Uke 41", "Uke 42", -1)] // 41 comes before 42
        [InlineData("Uke 42", "Uke 41", 1)] // 42 comes after 41
        public void NaturalSort_WeekNumbers_SortsCorrectly(string x, string y, int expected)
        {
            // Act
            var result = _comparer.Compare(x, y);
            
            // Assert
            Assert.Equal(expected, Math.Sign(result));
        }

        [Theory]
        [InlineData("A", "B", -1)]
        [InlineData("B", "A", 1)]
        [InlineData("Alpha", "Beta", -1)]
        [InlineData("Julebord", "Middag", -1)]
        public void NaturalSort_TextOnly_SortsAlphabetically(string x, string y, int expected)
        {
            // Act
            var result = _comparer.Compare(x, y);
            
            // Assert
            Assert.Equal(expected, Math.Sign(result));
        }

        [Theory]
        [InlineData("Item1", "Item10", -1)]
        [InlineData("Item10", "Item1", 1)]
        [InlineData("Item2", "Item10", -1)]
        [InlineData("Item10", "Item2", 1)]
        [InlineData("File1.txt", "File10.txt", -1)]
        [InlineData("File100.txt", "File20.txt", 1)]
        public void NaturalSort_MixedTextAndNumbers_SortsNaturally(string x, string y, int expected)
        {
            // Act
            var result = _comparer.Compare(x, y);
            
            // Assert
            Assert.Equal(expected, Math.Sign(result));
        }

        [Theory]
        [InlineData("same", "same", 0)]
        [InlineData("Uke 42", "Uke 42", 0)]
        [InlineData("", "", 0)]
        public void NaturalSort_IdenticalStrings_ReturnsZero(string x, string y, int expected)
        {
            // Act
            var result = _comparer.Compare(x, y);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NaturalSort_NullValues_HandlesCorrectly()
        {
            // Act & Assert
            Assert.True(_comparer.Compare(null, "test") < 0); // null comes first
            Assert.True(_comparer.Compare("test", null) > 0); // non-null comes after
            Assert.Equal(0, _comparer.Compare(null, null)); // both null are equal
        }

        [Theory]
        [InlineData("uke 41", "Uke 41", 0)] // Case insensitive
        [InlineData("UKE 41", "uke 41", 0)]
        [InlineData("Uke 41", "UKE 42", -1)]
        public void NaturalSort_CaseInsensitive_SortsCorrectly(string x, string y, int expected)
        {
            // Act
            var result = _comparer.Compare(x, y);
            
            // Assert
            Assert.Equal(expected, Math.Sign(result));
        }

        [Fact]
        public void NaturalSort_RealWorldScenario_WeeklyShoppingLists()
        {
            // Arrange
            var unsortedLists = new List<string>
            {
                "Uke 43",
                "Uke 2",
                "Uke 10",
                "Julebord 2025",
                "Uke 1",
                "Uke 42",
                "Middag fredag"
            };

            var expected = new List<string>
            {
                "Julebord 2025",
                "Middag fredag",
                "Uke 1",
                "Uke 2",
                "Uke 10",
                "Uke 42",
                "Uke 43"
            };

            // Act
            var sorted = unsortedLists.OrderBy(s => s, _comparer).ToList();

            // Assert
            Assert.Equal(expected, sorted);
        }

        [Fact]
        public void NaturalSort_ComplexNumbers_SortsCorrectly()
        {
            // Arrange
            var unsortedItems = new List<string>
            {
                "Version 2.10",
                "Version 2.2",
                "Version 2.1",
                "Version 10.1",
                "Version 1.9"
            };

            var expected = new List<string>
            {
                "Version 1.9",
                "Version 2.1",
                "Version 2.2",
                "Version 2.10",
                "Version 10.1"
            };

            // Act
            var sorted = unsortedItems.OrderBy(s => s, _comparer).ToList();

            // Assert
            Assert.Equal(expected, sorted);
        }

        [Fact]
        public void NaturalSort_OnlyNumbers_SortsNumerically()
        {
            // Arrange
            var unsortedNumbers = new List<string> { "100", "2", "10", "1", "20" };
            var expected = new List<string> { "1", "2", "10", "20", "100" };

            // Act
            var sorted = unsortedNumbers.OrderBy(s => s, _comparer).ToList();

            // Assert
            Assert.Equal(expected, sorted);
        }
    }
}
