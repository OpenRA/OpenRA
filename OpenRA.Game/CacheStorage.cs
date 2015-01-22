namespace OpenRA
{
	public interface ICacheStorage<T>
	{
		void Remove(string key);
		void Store(string key, T data);
		T Retrieve(string key);
	}
}