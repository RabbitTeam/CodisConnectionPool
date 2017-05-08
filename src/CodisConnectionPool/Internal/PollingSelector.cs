using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;

namespace CodisConnectionPool.Internal
{
    internal class PollingSelector : IDisposable
    {
        private readonly Lazy<ConnectionMultiplexer>[] _pools;

        private int _index;
        private int _lock;
        private readonly int _maxIndex;

        public PollingSelector(Lazy<ConnectionMultiplexer>[] pools)
        {
            _pools = pools;
            _maxIndex = pools.Length - 1;
        }

        public Lazy<ConnectionMultiplexer> GetConnectionMultiplexer()
        {
            while (true)
            {
                //如果无法得到锁则等待
                if (Interlocked.Exchange(ref _lock, 1) != 0)
                {
                    default(SpinWait).SpinOnce();
                    continue;
                }

                var item = _pools.ElementAt(_index);

                //设置为下一个
                if (_maxIndex > _index)
                    _index++;
                else
                    _index = 0;

                //释放锁
                Interlocked.Exchange(ref _lock, 0);

                return item;
            }
        }

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            foreach (var pool in _pools)
            {
                if (!pool.IsValueCreated)
                    continue;
                try
                {
                    pool.Value.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
        }

        #endregion IDisposable
    }
}