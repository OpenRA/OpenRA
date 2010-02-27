#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;

namespace OpenRA.FileFormats
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
