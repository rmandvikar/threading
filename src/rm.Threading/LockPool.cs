using static rm.Threading.PoolHelper;

namespace rm.Threading;

/// <summary>
/// Provides a pool of lock objects for keyed usage.
/// </summary>
public class LockPool
{
	/// <summary>
	/// Pool of objects.
	/// </summary>
	private readonly object[] pool;

	/// <inheritdoc cref="PoolSize" />
	private static readonly int poolSize = PoolSize;

	/// <summary>
	/// Ctor.
	/// </summary>
	public LockPool()
	{
		pool = new object[poolSize];
		for (int i = 0; i < poolSize; i++)
		{
			pool[i] = new object();
		}
	}

	/// <inheritdoc cref="Get(object)" />
	public object this[object key] => Get(key);

	/// <summary>
	/// Returns an <see cref="object"/> from the pool that the <paramref name="key"/> maps to.
	/// </summary>
	public object Get(object key)
	{
		return pool[GetIndex(key)];
	}
}
