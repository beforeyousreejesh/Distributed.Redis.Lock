using Distributed.Redis.Lock.Factory;
using Distributed.Redis.Lock.Factory.Contract;
using Moq;
using StackExchange.Redis;

namespace Frontiers.Journal.Redis.Lock.UnitTests
{
    public abstract partial class RedisLockTests
    {
        protected internal IRedisLockFactory _redisLockfactory;

        protected internal Mock<IConnectionMultiplexer> _connectionMultiplexer;

        protected internal Mock<IDatabase> _mockDatabase;

        protected internal int Db = 0;

        public RedisLockTests()
        {
            _mockDatabase = new Mock<IDatabase>();


            _connectionMultiplexer = new Mock<IConnectionMultiplexer>();

            _redisLockfactory = new RedisLockFactory(_connectionMultiplexer.Object, Db);

            _connectionMultiplexer.Setup(x =>
                        x.GetDatabase(
                            It.IsAny<int>(),
                            It.IsAny<object>())).Returns(_mockDatabase.Object);
        }
    }
}
