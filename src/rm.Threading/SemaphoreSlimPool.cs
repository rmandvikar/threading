using System;
using System.Threading;
using static rm.Threading.PoolHelper;

namespace rm.Threading;

/// <summary>
/// Provides a pool of SemaphoreSlim objects for keyed usage.
/// </summary>
public class SemaphoreSlimPool : IDisposable
{
	/// <summary>
	/// Pool of SemaphoreSlim objects.
	/// </summary>
	private readonly SemaphoreSlim[] pool;

	/// <inheritdoc cref="PoolSize" />
	private static readonly int poolSize = PoolSize;

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
	public SemaphoreSlim Get(object key)
	{
		return pool[GetIndex(key)];
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
