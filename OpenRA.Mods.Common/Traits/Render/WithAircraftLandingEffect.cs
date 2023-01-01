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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Plays an animation on the ground position when the actor lands.")]
	public class WithAircraftLandingEffectInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string Image = null;

		[SequenceReference(nameof(Image))]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference]
		public readonly string Palette = "effect";

		[Desc("Should the sprite effect be visible through fog.")]
		public readonly bool VisibleThroughFog = false;

		[Desc("Height at which to play the animation when descending.")]
		public readonly WDist DistanceAboveTerrain = new WDist(756);

		[Desc("Only play on these terrain types.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new WithAircraftLandingEffect(this); }
	}

	public class WithAircraftLandingEffect : ConditionalTrait<WithAircraftLandingEffectInfo>, INotifyLanding, ITick
	{
		bool shouldAddEffect;

		public WithAircraftLandingEffect(WithAircraftLandingEffectInfo info)
			: base(info) { }

		void AddEffect(Actor self)
		{
			var position = self.CenterPosition - new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(self.CenterPosition));
			self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(position, self.World, Info.Image,
				Info.Sequences.Random(Game.CosmeticRandom), Info.Palette, Info.VisibleThroughFog)));
		}

		bool ShouldAddEffect(Map map, CPos cell)
		{
			if (Info.TerrainTypes.Count == 0)
				return true;

			return map.Contains(cell) && Info.TerrainTypes.Contains(map.GetTerrainInfo(cell).Type);
		}

		void INotifyLanding.Landing(Actor self)
		{
			shouldAddEffect = ShouldAddEffect(self.World.Map, self.Location);
		}

		void ITick.Tick(Actor self)
		{
			if (!shouldAddEffect)
				return;

			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition) > Info.DistanceAboveTerrain)
				return;

			AddEffect(self);
			shouldAddEffect = false;
		}
	}
}
