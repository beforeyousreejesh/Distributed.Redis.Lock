using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Frontiers.Journal.Redis.Lock.UnitTests
{
    public class CreateAsync : RedisLockTests
    {
        private string _resourceName = "test:UniTest";

        [Theory]
        [InlineData(3, 3, 3, 1)]
        [InlineData(3, 4, 0, 1)]
        [InlineData(3, 5, 1, 1)]
        [InlineData(4, 5, 2, 1)]
        public async Task Createlock_Validate_NotNull(int expiryTimeSeconds, int waitingTimeSeconds, int renewCount, int retryTimeSeconds)
        {
            // Arrange

            var expiryTime = TimeSpan.FromSeconds(expiryTimeSeconds);
            var waitingTime = TimeSpan.FromSeconds(waitingTimeSeconds);
            var retryTime = TimeSpan.FromSeconds(retryTimeSeconds);

            // Act
            using (var redisLock = await _redisLockfactory.CreateAsync(_resourceName, expiryTime, waitingTime, renewCount, retryTime, CancellationToken.None))
            {
                // Assert
                Assert.NotNull(redisLock);
            }
        }

        [Theory]
        [InlineData(3, 3, 3, 1, "test:UniTest:1")]
        [InlineData(3, 4, 0, 1, "test:UniTest:2")]
        [InlineData(3, 5, 1, 1, "test:UniTest:3")]
        [InlineData(4, 5, 2, 1, "test:UniTest:4")]
        public async Task Createlock_Validate_AcquiredLock(int expiryTimeSeconds, int waitingTimeSeconds, int renewCount, int retryTimeSeconds, string resourceName)
        {
            // Arrange

            var expiryTime = TimeSpan.FromSeconds(expiryTimeSeconds);
            var waitingTime = TimeSpan.FromSeconds(waitingTimeSeconds);
            var retryTime = TimeSpan.FromSeconds(retryTimeSeconds);

            // Act
            using (var redisLock = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, renewCount, retryTime, CancellationToken.None))
            {
                // Assert
                Assert.True(redisLock.IsAcquired);
            }
        }

        [Fact]
        public async Task Createlock_Validate_NotAcquiredLock()
        {
            // Arrange

            var expiryTime = TimeSpan.FromSeconds(30);
            var waitingTime = TimeSpan.FromSeconds(3);
            var retryTime = TimeSpan.FromSeconds(5);
            string resourceName = "test:UniTest:1";

            // Act
            using (var redisLock = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, 0, retryTime, CancellationToken.None))
            {
                var redisLockAgain = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, 0, retryTime, CancellationToken.None);

                // Assert
                Assert.True(!redisLockAgain.IsAcquired);
            }
        }

        [Fact]
        public async Task Createlock_Validate_AcquiredLockExtend()
        {
            // Arrange

            var expiryTime = TimeSpan.FromSeconds(30);
            var waitingTime = TimeSpan.FromSeconds(3);
            var retryTime = TimeSpan.FromSeconds(5);
            string resourceName = "test:UniTest:1";

            // Act
            using (var redisLock = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, 5, retryTime, CancellationToken.None))
            {
                await Task.Delay(expiryTime.Milliseconds);

                // Assert
                Assert.True(redisLock.IsAcquired);
            }
        }

        [Fact]
        public async Task Createlock_Validate_ReAcquiredLock()
        {
            // Arrange

            var expiryTime = TimeSpan.FromSeconds(30);
            var waitingTime = TimeSpan.FromSeconds(3);
            var retryTime = TimeSpan.FromSeconds(5);
            string resourceName = "test:UniTest:1";

            // Act
            using (var redisLock = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, 5, retryTime, CancellationToken.None))
            {
                // Assert
                Assert.True(redisLock.IsAcquired);
            }

            using (var redisLock = await _redisLockfactory.CreateAsync(resourceName, expiryTime, waitingTime, 5, retryTime, CancellationToken.None))
            {
                // Assert
                Assert.True(redisLock.IsAcquired);
            }
        }
    }
}