using Distributed.Redis.Lock.Factory;
using Distributed.Redis.Lock.Factory.Contract;
using StackExchange.Redis;

namespace Frontiers.Journal.Redis.Lock.UnitTests
{
    public abstract partial class RedisLockTests
    {
        protected internal IRedisLockFactory _redisLockfactory;

        protected internal string ConnectionString = "192.168.0.56:6379,defaultDatabase=0,abortConnect=false";

        protected internal int Db = 0;

        public RedisLockTests()
        {
            _redisLockfactory = new RedisLockFactory(ConnectionMultiplexer.Connect(ConnectionString), Db);
        }
    }
}
