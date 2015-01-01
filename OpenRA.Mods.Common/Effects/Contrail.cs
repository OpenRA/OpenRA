#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Effects
{
	[Desc("Draw a colored contrail behind this actor when they move.")]
	class ContrailInfo : ITraitInfo, Requires<IBodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Measured in pixels.")]
		public readonly int TrailLength = 25;

		[Desc("RGB color of the contrail.")]
		public readonly Color Color = Color.White;

		[Desc("Use player remap color instead of a custom color?")]
		public readonly bool UsePlayerColor = true;

		public object Create(ActorInitializer init) { return new Contrail(init.self, this); }
	}

	class Contrail : ITick, IRender
	{
		ContrailInfo info;
		ContrailRenderable trail;
		IBodyOrientation body;

		public Contrail(Actor self, ContrailInfo info)
		{
			this.info = info;

			var color = info.UsePlayerColor ? ContrailRenderable.ChooseColor(self) : info.Color;
			trail = new ContrailRenderable(self.World, color, info.TrailLength, 0, 0);

			body = self.Trait<IBodyOrientation>();
		}

		public void Tick(Actor self)
		{
			var local = info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation));
			trail.Update(self.CenterPosition + body.LocalToWorld(local));
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			yield return trail;
		}
	}
}
