using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Distributed.Redis.Lock.Exceptions
{
    public class RedisTransactionCommitException : Exception
    {
        public RedisTransactionCommitException(string message) : base(message)
        {
        }
    }
}
