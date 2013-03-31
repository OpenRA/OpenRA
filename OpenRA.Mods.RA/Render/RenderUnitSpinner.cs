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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitSpinnerInfo : RenderUnitInfo
	{
		public readonly int[] Offset = { 0, 0 };
		public override object Create(ActorInitializer init) { return new RenderUnitSpinner(init.self); }
	}

	class RenderUnitSpinner : RenderUnit
	{
		public RenderUnitSpinner(Actor self)
			: base(self)
		{
			var info = self.Info.Traits.Get<RenderUnitSpinnerInfo>();

			var spinnerAnim = new Animation(GetImage(self));
			var facing = self.Trait<IFacing>();

			spinnerAnim.PlayRepeating("spinner");

			var turret = new Turret(info.Offset);
			anims.Add("spinner", new AnimationWithOffset(
				spinnerAnim,
				wr => turret.PxPosition(self, facing).ToFloat2(),
				null ) { ZOffset = 1 } );
		}
	}
}
