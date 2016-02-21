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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	[Desc("Draw a colored contrail behind this actor when they move.")]
	class ContrailInfo : ITraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Offset for Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Length of the trail (in ticks).")]
		public readonly int TrailLength = 25;

		[Desc("Width of the trail.")]
		public readonly WDist TrailWidth = new WDist(64);

		[Desc("RGB color of the contrail.")]
		public readonly Color Color = Color.White;

		[Desc("Use player remap color instead of a custom color?")]
		public readonly bool UsePlayerColor = true;

		public object Create(ActorInitializer init) { return new Contrail(init.Self, this); }
	}

	class Contrail : ITick, IRender
	{
		readonly ContrailInfo info;
		readonly BodyOrientation body;

		// This is a mutable struct, so it can't be readonly.
		ContrailRenderable trail;

		public Contrail(Actor self, ContrailInfo info)
		{
			this.info = info;

			var color = info.UsePlayerColor ? ContrailRenderable.ChooseColor(self) : info.Color;
			trail = new ContrailRenderable(self.World, color, info.TrailWidth, info.TrailLength, 0, info.ZOffset);

			body = self.Trait<BodyOrientation>();
		}

		public void Tick(Actor self)
		{
			var local = info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation));
			trail.Update(self.CenterPosition + body.LocalToWorld(local));
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return new IRenderable[] { trail };
		}
	}
}
