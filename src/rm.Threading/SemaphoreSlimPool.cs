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
		private readonly SemaphoreSlim[] pool;

		/// <summary>
		/// Size of the pool.
		/// <para></para>
		/// Environment.ProcessorCount is not always correct so use more slots as buffer,
		/// with a minimum of 32 slots.
		/// <para></para>
		/// Note: Trick borrowed from LazyCache impl.
		/// </summary>
		private readonly int poolSize = Math.Max(Environment.ProcessorCount << 3, 32);

		private const int NoMaximum = int.MaxValue;

		/// <summary>
		/// Ctor.
		/// </summary>
		public SemaphoreSlimPool(int initialCount)
			: this(initialCount, NoMaximum)
		{ }

		/// <summary>
		/// Ctor.
		/// </summary>
		public SemaphoreSlimPool(int initialCount, int maxCount)
		{
			pool = new SemaphoreSlim[poolSize];
			for (int i = 0; i < poolSize; i++)
			{
				pool[i] = new SemaphoreSlim(initialCount, maxCount);
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
			return pool[GetIndex(key)];
		}

		private uint GetIndex(object key)
		{
			return unchecked((uint)key.GetHashCode()) % (uint)poolSize;
		}

		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					if (pool != null)
					{
						for (int i = 0; i < poolSize; i++)
						{
							pool[i].Dispose();
						}
					}
				}

				disposed = true;
			}
		}
	}
}
