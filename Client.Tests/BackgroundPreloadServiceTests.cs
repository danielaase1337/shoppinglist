using BlazorApp.Client.Services;
using Moq;
using System.Threading;
using Xunit;

namespace Client.Tests.Services
{
    public class BackgroundPreloadServiceTests
    {
        private readonly Mock<IDataCacheService> _dataCacheMock;
        private readonly BackgroundPreloadService _backgroundPreloadService;

        public BackgroundPreloadServiceTests()
        {
            _dataCacheMock = new Mock<IDataCacheService>();
            _backgroundPreloadService = new BackgroundPreloadService(_dataCacheMock.Object);
        }

        [Fact]
        public async Task StartFastCorePreloadAsync_CallsDataCachePreloadCoreData()
        {
            // Arrange
            _dataCacheMock.Setup(dc => dc.PreloadCoreDataAsync())
                         .Returns(Task.CompletedTask);

            // Act
            await _backgroundPreloadService.StartFastCorePreloadAsync();

            // Assert
            _dataCacheMock.Verify(dc => dc.PreloadCoreDataAsync(), Times.Once);
        }

        [Fact]
        public async Task StartFastCorePreloadAsync_DataCacheThrows_DoesNotPropagateException()
        {
            // Arrange
            _dataCacheMock.Setup(dc => dc.PreloadCoreDataAsync())
                         .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert - Should not throw
            await _backgroundPreloadService.StartFastCorePreloadAsync();

            // Verify the method was called despite the exception
            _dataCacheMock.Verify(dc => dc.PreloadCoreDataAsync(), Times.Once);
        }

        [Fact]
        public async Task StartPreloadingAsync_CallsDataCachePreloadActiveShoppingLists()
        {
            // Arrange
            var tcs = new TaskCompletionSource<bool>();
            _dataCacheMock.Setup(dc => dc.PreloadActiveShoppingListsAsync())
                         .Returns(async () =>
                         {
                             tcs.SetResult(true);
                             await Task.CompletedTask;
                         });

            // Act
            await _backgroundPreloadService.StartPreloadingAsync();

            // Wait for the background task to complete (with timeout)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(tcs.Task, completedTask);

            // Assert
            _dataCacheMock.Verify(dc => dc.PreloadActiveShoppingListsAsync(), Times.Once);
        }

        [Fact]
        public async Task StartPreloadingAsync_CalledMultipleTimes_OnlyStartsOnce()
        {
            // Arrange
            var callCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            _dataCacheMock.Setup(dc => dc.PreloadActiveShoppingListsAsync())
                         .Returns(async () =>
                         {
                             Interlocked.Increment(ref callCount);
                             tcs.SetResult(true);
                             await Task.CompletedTask;
                         });

            // Act
            await _backgroundPreloadService.StartPreloadingAsync();
            await _backgroundPreloadService.StartPreloadingAsync();
            await _backgroundPreloadService.StartPreloadingAsync();

            // Wait for the background task to complete (with timeout)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(tcs.Task, completedTask);

            // Assert
            _dataCacheMock.Verify(dc => dc.PreloadActiveShoppingListsAsync(), Times.Once);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task StartPreloadingAsync_DataCacheThrows_DoesNotPropagateException()
        {
            // Arrange
            var tcs = new TaskCompletionSource<bool>();
            _dataCacheMock.Setup(dc => dc.PreloadActiveShoppingListsAsync())
                         .Returns(async () =>
                         {
                             tcs.SetResult(true);
                             await Task.CompletedTask;
                             throw new Exception("Test exception");
                         });

            // Act & Assert - Should not throw
            await _backgroundPreloadService.StartPreloadingAsync();

            // Wait for the background task to complete (with timeout)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.Equal(tcs.Task, completedTask);

            // Verify the method was called despite the exception
            _dataCacheMock.Verify(dc => dc.PreloadActiveShoppingListsAsync(), Times.Once);
        }
    }
}