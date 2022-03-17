using System;

namespace rm.Threading
{
	/// <summary>
	/// Defines pool helper methods.
	/// </summary>
	public static class PoolHelper
	{
		/// <summary>
		/// Size of the pool.
		/// <para></para>
		/// Environment.ProcessorCount is not always correct so use more slots as buffer,
		/// with a minimum of 64 slots.
		/// <para></para>
		/// Note: Trick borrowed from LazyCache impl.
		/// </summary>
		public static readonly int PoolSize = Math.Max(Environment.ProcessorCount << 6, 64);

		/// <summary>
		/// Returns an index from the pool that the <paramref name="key"/> maps to.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static uint GetIndex(object key)
		{
			_ = key ?? throw new ArgumentNullException(nameof(key));
			return unchecked((uint)key.GetHashCode()) % (uint)PoolSize;
		}
	}
}
