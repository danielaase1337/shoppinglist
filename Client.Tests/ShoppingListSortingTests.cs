using BlazorApp.Client.Common;
using Shared.HandlelisteModels;
using Xunit;

namespace Client.Tests
{
    public class ShoppingListSortingTests
    {
        [Fact]
        public void ShoppingListSorting_ActiveListsFirst_CompletedListsLast()
        {
            // Arrange
            var naturalComparer = new NaturalSortComparer();
            var unsortedLists = new List<ShoppingListModel>
            {
                new ShoppingListModel 
                { 
                    Id = "1", 
                    Name = "Uke 43", 
                    IsDone = true,  // Completed
                    LastModified = DateTime.UtcNow.AddDays(-1)
                },
                new ShoppingListModel 
                { 
                    Id = "2", 
                    Name = "Uke 42", 
                    IsDone = false,  // Active
                    LastModified = DateTime.UtcNow.AddDays(-2)
                },
                new ShoppingListModel 
                { 
                    Id = "3", 
                    Name = "Uke 41", 
                    IsDone = false,  // Active
                    LastModified = DateTime.UtcNow
                },
                new ShoppingListModel 
                { 
                    Id = "4", 
                    Name = "Julebord", 
                    IsDone = true,  // Completed
                    LastModified = DateTime.UtcNow.AddDays(-3)
                }
            };

            // Act - Apply the same sorting as SortShoppingLists()
            var sorted = unsortedLists
                .OrderBy(f => f.IsDone)                                        // Active first
                .ThenByDescending(f => f.LastModified ?? DateTime.MinValue)   // Newest first
                .ThenBy(f => f.Name, naturalComparer)                          // Natural sort
                .ToList();

            // Assert
            // Active lists should come first (IsDone = false)
            Assert.False(sorted[0].IsDone);
            Assert.False(sorted[1].IsDone);
            
            // Completed lists should come last (IsDone = true)
            Assert.True(sorted[2].IsDone);
            Assert.True(sorted[3].IsDone);
            
            // Within active lists: newest first
            Assert.Equal("Uke 41", sorted[0].Name);  // Today (newest)
            Assert.Equal("Uke 42", sorted[1].Name);  // 2 days ago
            
            // Within completed lists: newest first
            Assert.Equal("Uke 43", sorted[2].Name);  // 1 day ago (newest completed)
            Assert.Equal("Julebord", sorted[3].Name);  // 3 days ago
        }

        [Fact]
        public void ShoppingListSorting_SameDate_UsesNaturalSort()
        {
            // Arrange
            var naturalComparer = new NaturalSortComparer();
            var sameDate = DateTime.UtcNow;
            var unsortedLists = new List<ShoppingListModel>
            {
                new ShoppingListModel 
                { 
                    Id = "1", 
                    Name = "Uke 10", 
                    IsDone = false,
                    LastModified = sameDate
                },
                new ShoppingListModel 
                { 
                    Id = "2", 
                    Name = "Uke 2", 
                    IsDone = false,
                    LastModified = sameDate
                },
                new ShoppingListModel 
                { 
                    Id = "3", 
                    Name = "Uke 41", 
                    IsDone = false,
                    LastModified = sameDate
                }
            };

            // Act
            var sorted = unsortedLists
                .OrderBy(f => f.IsDone)
                .ThenByDescending(f => f.LastModified ?? DateTime.MinValue)
                .ThenBy(f => f.Name, naturalComparer)
                .ToList();

            // Assert - Natural sort: Uke 2, Uke 10, Uke 41
            Assert.Equal("Uke 2", sorted[0].Name);
            Assert.Equal("Uke 10", sorted[1].Name);
            Assert.Equal("Uke 41", sorted[2].Name);
        }

        [Fact]
        public void ShoppingListSorting_MixedActiveAndCompleted_CorrectGrouping()
        {
            // Arrange
            var naturalComparer = new NaturalSortComparer();
            var now = DateTime.UtcNow;
            
            var lists = new List<ShoppingListModel>
            {
                new ShoppingListModel { Name = "Active New", IsDone = false, LastModified = now },
                new ShoppingListModel { Name = "Completed Old", IsDone = true, LastModified = now.AddDays(-5) },
                new ShoppingListModel { Name = "Active Old", IsDone = false, LastModified = now.AddDays(-3) },
                new ShoppingListModel { Name = "Completed New", IsDone = true, LastModified = now.AddDays(-1) },
            };

            // Act
            var sorted = lists
                .OrderBy(f => f.IsDone)
                .ThenByDescending(f => f.LastModified ?? DateTime.MinValue)
                .ThenBy(f => f.Name, naturalComparer)
                .ToList();

            // Assert
            // First two should be active (false)
            Assert.Equal("Active New", sorted[0].Name);
            Assert.Equal("Active Old", sorted[1].Name);
            Assert.False(sorted[0].IsDone);
            Assert.False(sorted[1].IsDone);
            
            // Last two should be completed (true)
            Assert.Equal("Completed New", sorted[2].Name);
            Assert.Equal("Completed Old", sorted[3].Name);
            Assert.True(sorted[2].IsDone);
            Assert.True(sorted[3].IsDone);
        }
    }
}
