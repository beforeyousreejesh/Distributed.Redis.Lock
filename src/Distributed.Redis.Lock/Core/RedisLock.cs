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

        private RedisConnection _redisConnection;

        private Timer keepLockBeforeDispose;

        private bool _isDisposed = false;

        private int ReNewCount;

        public RedisLock(string redisLockKey, TimeSpan expiryTime, RedisConnection redisConnection, TimeSpan waitingTime, int renewCount, TimeSpan retryTime)
        {
            if (redisLockKey == null)
            {
                throw new ArgumentNullException(nameof(redisLockKey));
            }

            if (expiryTime == null)
            {
                throw new ArgumentNullException(nameof(expiryTime));
            }

            if (redisConnection == null)
            {
                throw new ArgumentNullException(nameof(redisConnection));
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
            _redisConnection = redisConnection;
            WaitingTime = waitingTime;
            RetryTime = retryTime;
            TotalReNewCount = renewCount;
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
            await _redisConnection
                    .connectionMultiplexer
                    .GetDatabase(_redisConnection.DataBase)
                    .KeyDeleteAsync(RedisLockKey, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        private void UnLock()
        {
            _redisConnection
                   .connectionMultiplexer
                   .GetDatabase(_redisConnection.DataBase)
                   .KeyDeleteAsync(RedisLockKey, CommandFlags.FireAndForget);
        }

        private async Task<bool> LockAsync()
        {
            return await _redisConnection
                 .connectionMultiplexer
                 .GetDatabase(_redisConnection.DataBase)
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
                            _redisConnection.connectionMultiplexer.GetDatabase(_redisConnection.DataBase).KeyExpire(RedisLockKey, ExpiryTime, CommandFlags.DemandMaster);
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
