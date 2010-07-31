#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
			var move = self.traits.Get<IMove>();
			var info = self.Info.Traits.Get<RenderUnitSpinnerInfo>();

			var spinnerAnim = new Animation(GetImage(self));
			spinnerAnim.PlayRepeating("spinner");
			anims.Add("spinner", new AnimationWithOffset(
				spinnerAnim,
				() => Combat.GetTurretPosition(self, move, new Turret(info.Offset)),
				null) { ZOffset = 1 });
		}
	}
}
