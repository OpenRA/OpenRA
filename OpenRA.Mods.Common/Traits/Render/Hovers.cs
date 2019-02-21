#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		[Desc("Amount of Z axis changes in world units.")]
		public readonly int OffsetModifier = -43;

		public readonly int MinHoveringAltitude = 0;

		[Desc("Amount of ticks it takes to reach OffsetModifier.")]
		public readonly int Ticks = 25;

		[Desc("Amount of ticks it takes to fall to the ground from the highest point when disabled.")]
		public readonly int FallTicks = 12;

		[Desc("Amount of ticks it takes to rise from the ground to InitialHeight.")]
		public readonly int RiseTicks = 17;

		public readonly int InitialHeight = 384;

		public override object Create(ActorInitializer init) { return new Hovers(this, init.Self); }
	}

	public class Hovers : ConditionalTrait<HoversInfo>, IRenderModifier, ITick
	{
		readonly HoversInfo info;
		readonly int stepPercentage;
		readonly int fallTickHeight;

		int ticks = 0;
		WVec worldVisualOffset = WVec.Zero;

		public Hovers(HoversInfo info, Actor self)
			: base(info)
		{
			this.info = info;
			this.stepPercentage = 256 / info.Ticks;
			this.fallTickHeight = (info.InitialHeight + info.OffsetModifier) / info.FallTicks;
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
				var visualOffset = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length >= info.MinHoveringAltitude
					? new WAngle(ticks % (info.Ticks * 4) * stepPercentage).Sin() : 0;
				var currentHeight = info.OffsetModifier * visualOffset / 1024 + info.InitialHeight;

				// This part rises the actor up from disabled state
				if (worldVisualOffset.Z < currentHeight)
					currentHeight = Math.Min(worldVisualOffset.Z + info.InitialHeight / info.RiseTicks, currentHeight);

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
