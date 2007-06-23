using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Core
{
	static class Reflect
	{
		public static T GetAttribute<T>(Type t) 
			where T : Attribute
		{
			T[] attribs = (T[])t.GetCustomAttributes(typeof(T), false);
			if (attribs == null || attribs.Length == 0)
				return null;
			return attribs[0];
		}
	}
}
