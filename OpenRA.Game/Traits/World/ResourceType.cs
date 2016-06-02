#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ResourceTypeInfo : ITraitInfo
	{
		public readonly string Sequence = "resources";
		[SequenceReference("Sequence")] public readonly string[] Variants = { };
		[PaletteReference] public readonly string Palette = TileSet.TerrainPaletteInternalName;
		public readonly int ResourceType = 1;

		public readonly int ValuePerUnit = 0;
		public readonly int MaxDensity = 10;
		public readonly string Name = null;
		public readonly string TerrainType = "Ore";

		public readonly HashSet<string> AllowedTerrainTypes = new HashSet<string>();
		public readonly bool AllowUnderActors = false;
		public readonly bool AllowUnderBuildings = false;
		public readonly bool AllowOnRamps = false;

		public PipType PipColor = PipType.Yellow;

		public object Create(ActorInitializer init) { return new ResourceType(this, init.World); }
	}

	public class ResourceType : IWorldLoaded
	{
		public readonly ResourceTypeInfo Info;
		public PaletteReference Palette { get; private set; }
		public readonly Dictionary<string, Sprite[]> Variants;

		public ResourceType(ResourceTypeInfo info, World world)
		{
			Info = info;
			Variants = new Dictionary<string, Sprite[]>();
			foreach (var v in info.Variants)
			{
				var seq = world.Map.Rules.Sequences.GetSequence(Info.Sequence, v);
				var sprites = Exts.MakeArray(seq.Length, x => seq.GetSprite(x));
				Variants.Add(v, sprites);
			}
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			Palette = wr.Palette(Info.Palette);
		}
	}
}
