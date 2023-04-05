#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders the Tiberian Sun Tiberium resources.", "Attach this to the world actor")]
	public class TSTiberiumRendererInfo : ResourceRendererInfo
	{
		[Desc("Sequences to use for ramp type 1.", "Dictionary of [resource type]: [list of sequences].")]
		public readonly Dictionary<string, string[]> Ramp1Sequences = new();

		[Desc("Sequences to use for ramp type 2.", "Dictionary of [resource type]: [list of sequences].")]
		public readonly Dictionary<string, string[]> Ramp2Sequences = new();

		[Desc("Sequences to use for ramp type 3.", "Dictionary of [resource type]: [list of sequences].")]
		public readonly Dictionary<string, string[]> Ramp3Sequences = new();

		[Desc("Sequences to use for ramp type 4.", "Dictionary of [resource type]: [list of sequences].")]
		public readonly Dictionary<string, string[]> Ramp4Sequences = new();

		public override object Create(ActorInitializer init) { return new TSTiberiumRenderer(init.Self, this); }
	}

	public class TSTiberiumRenderer : ResourceRenderer
	{
		readonly TSTiberiumRendererInfo info;
		readonly World world;
		readonly Dictionary<string, Dictionary<string, ISpriteSequence>> ramp1Variants = new();
		readonly Dictionary<string, Dictionary<string, ISpriteSequence>> ramp2Variants = new();
		readonly Dictionary<string, Dictionary<string, ISpriteSequence>> ramp3Variants = new();
		readonly Dictionary<string, Dictionary<string, ISpriteSequence>> ramp4Variants = new();

		public TSTiberiumRenderer(Actor self, TSTiberiumRendererInfo info)
			: base(self, info)
		{
			this.info = info;
			world = self.World;
		}

		void LoadVariants(Dictionary<string, string[]> rampSequences, Dictionary<string, Dictionary<string, ISpriteSequence>> rampVariants)
		{
			var sequences = world.Map.Sequences;
			foreach (var kv in rampSequences)
			{
				if (!Info.ResourceTypes.TryGetValue(kv.Key, out var resourceInfo))
					continue;

				var resourceVariants = kv.Value
					.ToDictionary(v => v, v => sequences.GetSequence(resourceInfo.Image, v));
				rampVariants.Add(kv.Key, resourceVariants);
			}
		}

		protected override void WorldLoaded(World w, WorldRenderer wr)
		{
			LoadVariants(info.Ramp1Sequences, ramp1Variants);
			LoadVariants(info.Ramp2Sequences, ramp2Variants);
			LoadVariants(info.Ramp3Sequences, ramp3Variants);
			LoadVariants(info.Ramp4Sequences, ramp4Variants);

			base.WorldLoaded(w, wr);
		}

		protected override ISpriteSequence ChooseVariant(string resourceType, CPos cell)
		{
			Dictionary<string, Dictionary<string, ISpriteSequence>> variants;
			switch (world.Map.Ramp[cell])
			{
				case 1: variants = ramp1Variants; break;
				case 2: variants = ramp2Variants; break;
				case 3: variants = ramp3Variants; break;
				case 4: variants = ramp4Variants; break;
				default: variants = Variants; break;
			}

			return variants[resourceType].Values.Random(world.LocalRandom);
		}
	}
}
