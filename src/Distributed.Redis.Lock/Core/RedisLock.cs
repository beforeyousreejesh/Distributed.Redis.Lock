using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Distributed.Redis.Lock.Core
{
    public class RedisLock : IDisposable
    {
        public TimeSpan WaitingTime;

        public TimeSpan RetryTime;

        public string RedisLockKey;

        public bool IsAcquired;

        public TimeSpan ExpiryTime;

        public int TotalReNewCount;

        private IConnectionMultiplexer _connectionMultiplexer;

        private Timer keepLockBeforeDispose;

        private bool _isDisposed = false;

        private int ReNewCount;

        private int _database;

        public RedisLock(string redisLockKey, TimeSpan expiryTime, IConnectionMultiplexer connectionMultiplexer, TimeSpan waitingTime, int renewCount, TimeSpan retryTime,int database)
        {
            if (redisLockKey == null)
            {
                throw new ArgumentNullException(nameof(redisLockKey));
            }

            if (expiryTime == null)
            {
                throw new ArgumentNullException(nameof(expiryTime));
            }

            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            if (waitingTime == null)
            {
                throw new ArgumentNullException(nameof(waitingTime));
            }

            if (retryTime == null)
            {
                throw new ArgumentNullException(nameof(retryTime));
            }

            RedisLockKey = redisLockKey;
            ExpiryTime = expiryTime;
            _connectionMultiplexer = connectionMultiplexer;
            WaitingTime = waitingTime;
            RetryTime = retryTime;
            TotalReNewCount = renewCount;
            _database = database;
            IsAcquired = false;
        }

        internal async static Task<RedisLock> CreateAsync(RedisLock redisLock, CancellationToken cancellationToken)
        {
            if (redisLock == null)
            {
                throw new ArgumentNullException(nameof(redisLock));
            }

            cancellationToken.ThrowIfCancellationRequested();

            await redisLock.CreateAsync().ConfigureAwait(false);

            return redisLock;
        }

        private async Task<bool> CreateAsync()
        {
            int retryCount = 0;

            IsAcquired = await LockAsync().ConfigureAwait(false);

            if (!IsAcquired)
            {
                var stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed <= WaitingTime && !IsAcquired)
                {
                    IsAcquired = await LockAsync().ConfigureAwait(false);

                    if (!IsAcquired)
                    {
                        await Task.Delay(RetryTime).ConfigureAwait(false);
                        retryCount++;
                    }
                    else
                    {
                        KeepLockAliveBeforeDispose();
                        return IsAcquired;
                    }
                }
            }
            else
            {
                KeepLockAliveBeforeDispose();

                return IsAcquired;
            }

            return false;
        }

        private async Task UnLockAsync()
        {
            await _connectionMultiplexer
                    .GetDatabase(_database)
                    .KeyDeleteAsync(RedisLockKey, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        private void UnLock()
        {
            _connectionMultiplexer
                .GetDatabase(_database)
                .KeyDeleteAsync(RedisLockKey, CommandFlags.FireAndForget);
        }

        private async Task<bool> LockAsync()
        {
            return await _connectionMultiplexer
                    .GetDatabase(_database)
                    .StringSetAsync(RedisLockKey, 1, ExpiryTime, When.NotExists, CommandFlags.DemandMaster).ConfigureAwait(false);
        }

        private void KeepLockAliveBeforeDispose()
        {

            keepLockBeforeDispose = new Timer(
               state =>
                {
                    try
                    {
                        if (!_isDisposed && ReNewCount < TotalReNewCount)
                        {
                            _connectionMultiplexer.GetDatabase(_database).KeyExpire(RedisLockKey, ExpiryTime, CommandFlags.DemandMaster);
                            ReNewCount++;
                        }
                    }
                    catch (Exception)
                    {
                    }
                },
                null,
                (int)ExpiryTime.TotalMilliseconds / 2,
                (int)ExpiryTime.TotalMilliseconds / 2);
        }

        public void Dispose()
        {
            UnLock();

            _isDisposed = true;

            // prevent from hitting timer again
            keepLockBeforeDispose?.Change(Timeout.Infinite, Timeout.Infinite);
            keepLockBeforeDispose?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
