using Distributed.Redis.Lock.Core;
using Distributed.Redis.Lock.Factory.Contract;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Distributed.Redis.Lock.Factory
{
    public class RedisLockFactory : IRedisLockFactory
    {
        private readonly RedisConnection redisConnection;

        private string RedisLockKey = "distributed-redislock:{0}";

        public RedisLockFactory(IConnectionMultiplexer connectionMultiplexer, int? database)
        {
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            redisConnection = new RedisConnection { connectionMultiplexer = connectionMultiplexer, DataBase = database ?? 0 };
        }

        /// <summary>
        /// Create redis distributed lock
        /// </summary>
        /// <param name="resource">redis resource key. Base namespace would be journal-redislock </param>
        /// <param name="expiryTime">Totoal expiration time for the lock.</param>
        /// <param name="waitingTime">Total wait time if could not acquire lock</param>
        /// <param name="renewCount">Renew lock count if the object not disposed.</param>
        /// <param name="retryTime">Delay between each retry.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<RedisLock> CreateAsync(string resource, TimeSpan expiryTime, TimeSpan waitingTime, int renewCount, TimeSpan retryTime, CancellationToken cancellationToken)
        {
            var redisLock = new RedisLock(string.Format(RedisLockKey, resource), expiryTime, redisConnection, waitingTime, renewCount, retryTime);

            return await RedisLock.CreateAsync(redisLock, cancellationToken).ConfigureAwait(false);
        }
    }
}
