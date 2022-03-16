namespace rm.ThreadingTest
{
	public interface ICacheSettings
	{
		TimeSpan Ttl { get; }
	}

	public class CacheSettings : ICacheSettings
	{
		public TimeSpan Ttl { get; init; }
	}
}
