#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	[FieldLoader.Foo()]
	public class MusicInfo
	{
		public readonly string Filename = null;
		public readonly string Title = null;
		public readonly int Length = 0; // seconds

		public MusicInfo( string key, MiniYaml value )
		{
			FieldLoader.Load(this, value);
			if (Filename == null)
				Filename = key+".aud";
		}
	}
}
