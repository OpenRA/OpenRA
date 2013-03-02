#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

		public readonly string[] AllowedTerrainTypes = { };
		public readonly bool AllowUnderActors = false;

		public Sprite[][] Sprites;
		public PaletteReference PaletteRef;

		public PipType PipColor = PipType.Yellow;

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
