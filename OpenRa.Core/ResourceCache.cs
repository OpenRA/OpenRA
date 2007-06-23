using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Core
{
	static class ResourceCache
	{
		static Dictionary<string, IResource> items = new Dictionary<string, IResource>();

		public static void Flush()
		{
			items.Clear();
		}

		public static IResource Get(string filename)
		{
			IResource r;
			if (!items.TryGetValue(filename, out r))
				items.Add(filename, r = Load(filename));
			return r;
		}

		static IResource Load(string filename)
		{
			Converter<Stream, IResource> loader = 
				ResourceLoader.GetLoader(Path.GetExtension(filename));

			if (loader == null)
				return null;

			Stream s = FileSystem.GetItem(filename);

			if (s == null)
				return null;

			return loader(s);
		}
	}
}
