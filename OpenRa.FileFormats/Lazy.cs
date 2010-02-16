using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.FileFormats
{
	public class Lazy<T>
	{
		Func<T> p;
		T value;

		public Lazy(Func<T> p)
		{
			if (p == null)
				throw new ArgumentNullException();

			this.p = p;
		}

		public T Value
		{
			get
			{
				if (p == null)
					return value;

				value = p();
				p = null;
				return value;
			}
		}
	}

	public static class Lazy
	{
		public static Lazy<T> New<T>(Func<T> p) { return new Lazy<T>(p); }
	}
}
