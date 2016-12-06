using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Distributed.Redis.Lock.Core
{
    public class RedisConnection
    {
        public IConnectionMultiplexer connectionMultiplexer { get; set; }

        public int DataBase { get; set; }
    }
}
