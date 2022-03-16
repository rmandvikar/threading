using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	public interface IMemoryCacheImpl
	{
		IMemoryCache Value { get; }
	}
}
