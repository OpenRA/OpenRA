#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ResourceTypeInfo : ITraitInfo
	{
		public readonly string[] SpriteNames = { };
		public readonly string Palette = "terrain";
		public readonly int ResourceType = 1;

		public readonly int ValuePerUnit = 0;
		public readonly string Name = null;
		public readonly string TerrainType = "Ore";

		public Sprite[][] Sprites;
		public int PaletteIndex;
		
		public object Create(ActorInitializer init) { return new ResourceType(this); }
	}

	public class ResourceType
	{
		public ResourceTypeInfo info;
		public ResourceType(ResourceTypeInfo info)
		{
			this.info = info;
		}
	}
}
