#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.FileFormats
{
	public struct TileReference<T, U>
	{
		public T type;
		public U index;

		public TileReference(T t, U i)
		{
			type = t;
			index = i;
		}

		public override int GetHashCode() { return type.GetHashCode() ^ index.GetHashCode(); }
	}
}
