namespace rm.ThreadingTest
{
	public interface IValueProvider
	{
		Task<T> GetValueAsync<T>(object key);
	}
}
