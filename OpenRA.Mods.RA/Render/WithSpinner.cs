#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class WithSpinnerInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "spinner";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithSpinner(init.self, this); }
	}

	class WithSpinner
	{
		public WithSpinner(Actor self, WithSpinnerInfo info)
		{
			var rs = self.Trait<RenderSimple>();
			var spinner = new Animation(rs.GetImage(self));
			spinner.PlayRepeating(info.Sequence);
			rs.anims.Add("spinner_{0}".F(info.Sequence), new AnimationWithOffset(
				spinner,
				wr => wr.ScreenPxOffset(rs.LocalToWorld(info.Offset.Rotate(rs.QuantizeOrientation(self, self.Orientation)))),
				null ) { ZOffset = 1 } );
		}
	}
}
