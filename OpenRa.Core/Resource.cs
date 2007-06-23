using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Core
{
	public interface IResource { }

	public abstract class Resource<T> : IResource
		where T : Resource<T>
	{
		public static T Get(string filename)
		{
			return (T)ResourceCache.Get(filename);
		}
	}
}
