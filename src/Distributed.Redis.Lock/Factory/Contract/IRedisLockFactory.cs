using Distributed.Redis.Lock.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Distributed.Redis.Lock.Factory.Contract
{
    public interface IRedisLockFactory
    {
        Task<RedisLock> CreateAsync(string resource, TimeSpan expiryTime, TimeSpan waitingTime, int renewCount, TimeSpan retryTime, CancellationToken cancellationToken);
    }
}
