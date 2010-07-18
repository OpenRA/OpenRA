#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.FileFormats
{
	public struct SmudgeReference
	{
		public readonly string Type;
		public readonly int2 Location;
		public readonly int Depth;
		public SmudgeReference( string type, int2 location, int depth )
		{
			Type = type;
			Location = location;
			Depth = depth;
		}
		
		public override string ToString ()
		{
			return string.Format("{0} {1},{2} {3}", Type, Location.X,Location.Y, Depth);
		}

	}
}
