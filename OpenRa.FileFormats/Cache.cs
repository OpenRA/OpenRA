using System;
using System.Collections.Generic;
using System.Collections;

namespace OpenRa.FileFormats
{
	public class Cache<T, U> : IEnumerable<KeyValuePair<T, U>>
	{
		Dictionary<T, U> hax = new Dictionary<T, U>();
		Func<T,U> loader;

		public Cache(Func<T,U> loader)
		{
			if (loader == null)
				throw new ArgumentNullException();

			this.loader = loader;
		}

		public U this[T key]
		{
			get
			{
				U result;
				if (!hax.TryGetValue(key, out result))
					hax.Add(key, result = loader(key));

				return result;
			}
		}

		public IEnumerator<KeyValuePair<T, U>> GetEnumerator() { return hax.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public IEnumerable<T> Keys { get { return hax.Keys; } }
		public IEnumerable<U> Values { get { return hax.Values; } }
	}
}
