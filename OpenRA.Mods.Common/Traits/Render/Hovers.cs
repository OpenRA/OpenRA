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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Changes the visual Z position periodically.")]
	public class HoversInfo : ConditionalTraitInfo, Requires<IMoveInfo>
	{
		[Desc("Maximum visual Z axis distance relative to actual position + InitialHeight.")]
		public readonly WDist BobDistance = new WDist(-43);

		[Desc("Actual altitude of actor needs to be this or higher to enable hover effect.")]
		public readonly WDist MinHoveringAltitude = WDist.Zero;

		[Desc("Amount of ticks it takes to reach BobDistance.")]
		public readonly int Ticks = 6;

		[Desc("Amount of ticks it takes to fall to the ground from the highest point when disabled.")]
		public readonly int FallTicks = 10;

		[Desc("Amount of ticks it takes to rise from the ground to InitialHeight.")]
		public readonly int RiseTicks = 20;

		[Desc("Initial Z axis modifier relative to actual position.")]
		public readonly WDist InitialHeight = new WDist(43);

		public override object Create(ActorInitializer init) { return new Hovers(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (BobDistance.Length > -1)
				throw new YamlException("Hovers.BobDistance must be a negative value.");

			if (Ticks < 1)
				throw new YamlException("Hovers.Ticks must be higher than zero.");

			if (FallTicks < 1)
				throw new YamlException("Hovers.FallTicks must be higher than zero.");

			if (RiseTicks < 1)
				throw new YamlException("Hovers.RiseTicks must be higher than zero.");

			if (InitialHeight.Length < RiseTicks)
				throw new YamlException("Hovers.InitialHeight must be at least as high as RiseTicks.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class Hovers : ConditionalTrait<HoversInfo>, IRenderModifier, ITick
	{
		readonly HoversInfo info;
		readonly int stepPercentage;
		readonly int fallTickHeight;

		int ticks;
		WVec worldVisualOffset;

		public Hovers(HoversInfo info)
			: base(info)
		{
			this.info = info;
			stepPercentage = 256 / info.Ticks;

			// fallTickHeight must be at least 1 to avoid a DivideByZeroException and other potential problems when trait is disabled.
			fallTickHeight = (info.InitialHeight.Length + info.BobDistance.Length).Clamp(info.FallTicks, int.MaxValue) / info.FallTicks;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				if (worldVisualOffset.Z < 0)
					return;

				var fallTicks = worldVisualOffset.Z / fallTickHeight - 1;
				worldVisualOffset = new WVec(0, 0, fallTickHeight * fallTicks);
			}
			else
				ticks++;
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (!IsTraitDisabled)
			{
				var visualOffset = self.World.Map.DistanceAboveTerrain(self.CenterPosition) >= info.MinHoveringAltitude
					? new WAngle(ticks % (info.Ticks * 4) * stepPercentage).Sin() : 0;
				var currentHeight = info.BobDistance.Length * visualOffset / 1024 + info.InitialHeight.Length;

				// This part rises the actor up from disabled state
				if (worldVisualOffset.Z < currentHeight)
					currentHeight = Math.Min(worldVisualOffset.Z + info.InitialHeight.Length / info.RiseTicks, currentHeight);

				worldVisualOffset = new WVec(0, 0, currentHeight);
			}

			return r.Select(a => a.OffsetBy(worldVisualOffset));
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
