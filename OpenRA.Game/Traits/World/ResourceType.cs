#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
		[Desc("Sequence image that holds the different variants.")]
		public readonly string Image = "resources";

		[FieldLoader.Require]
		[SequenceReference("Image")]
		[Desc("Randomly chosen image sequences.")]
		public readonly string[] Sequences = { };

		[PaletteReference]
		[Desc("Palette used for rendering the resource sprites.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Resource index used in the binary map data.")]
		public readonly int ResourceType = 1;

		[Desc("Credit value of a single resource unit.")]
		public readonly int ValuePerUnit = 0;

		[Desc("Maximum number of resource units allowed in a single cell.")]
		public readonly int MaxDensity = 10;

		[FieldLoader.Require]
		[Desc("Resource identifier used by other traits.")]
		public readonly string Type = null;

		[FieldLoader.Require]
		[Desc("Resource name used by tooltips.")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Terrain type used to determine unit movement and minimap colors.")]
		public readonly string TerrainType = null;

		[Desc("Terrain types that this resource can spawn on.")]
		public readonly HashSet<string> AllowedTerrainTypes = new HashSet<string>();

		[Desc("Allow resource to spawn under Mobile actors.")]
		public readonly bool AllowUnderActors = false;

		[Desc("Allow resource to spawn under Buildings.")]
		public readonly bool AllowUnderBuildings = false;

		[Desc("Allow resource to spawn on ramp tiles.")]
		public readonly bool AllowOnRamps = false;

		[Desc("Harvester content pip color.")]
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
			foreach (var v in info.Sequences)
			{
				var seq = world.Map.Rules.Sequences.GetSequence(Info.Image, v);
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
