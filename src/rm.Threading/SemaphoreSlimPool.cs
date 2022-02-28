using System;
using System.Threading;

namespace rm.Threading
{
	/// <summary>
	/// Provides a pool of SemaphoreSlim objects for keyed usage.
	/// </summary>
	public class SemaphoreSlimPool : IDisposable
	{
		/// <summary>
		/// Pool of SemaphoreSlim objects.
		/// </summary>
		private readonly SemaphoreSlim[] _pool;

		/// <summary>
		/// Size of the pool.
		/// <para></para>
		/// Environment.ProcessorCount is not always correct so use more slots as buffer,
		/// with a minimum of 32 slots.
		/// </summary>
		private readonly int _poolSize = Math.Max(Environment.ProcessorCount << 3, 32);

		/// <summary>
		/// Ctor.
		/// </summary>
		public SemaphoreSlimPool(int initialCount, int maxCount)
		{
			_pool = new SemaphoreSlim[_poolSize];
			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new SemaphoreSlim(initialCount, maxCount);
			}
		}

		/// <summary>
		/// Ctor.
		/// </summary>
		public SemaphoreSlimPool(int initialCount)
		{
			_pool = new SemaphoreSlim[_poolSize];
			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i] = new SemaphoreSlim(initialCount);
			}
		}

		/// <inheritdoc cref="Get(object)" />
		public SemaphoreSlim this[object key] => Get(key);

		/// <summary>
		/// Returns a <see cref="SemaphoreSlim"/> from the pool that the <paramref name="key"/> maps to.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public SemaphoreSlim Get(object key)
		{
			_ = key ?? throw new ArgumentNullException(nameof(key));
			return _pool[GetIndex(key)];
		}

		private int GetIndex(object key)
		{
			return (int)(unchecked((uint)key.GetHashCode()) % _pool.Length);
		}

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					if (_pool != null)
					{
						for (int i = 0; i < _pool.Length; i++)
						{
							_pool[i].Dispose();
						}
					}
				}

				_disposed = true;
			}
		}
	}
}
