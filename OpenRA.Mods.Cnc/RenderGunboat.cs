#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderGunboatInfo : RenderSpritesInfo, Requires<IBodyOrientationInfo>
	{
		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		public override object Create(ActorInitializer init) { return new RenderGunboat(init.self, this); }
	}

	class RenderGunboat : RenderSprites, INotifyDamageStateChanged
	{
		Animation left, right;

		public RenderGunboat(Actor self, RenderGunboatInfo info)
			: base(self)
		{
			var name = GetImage(self);
			var facing = self.Trait<IFacing>();
			var turret = self.TraitsImplementing<Turreted>()
				.First(t => t.Name == info.Turret);

			left = new Animation(name, () => turret.turretFacing);
			left.Play("left");
			anims.Add("left", new AnimationWithOffset(left, null, () => facing.Facing > 128, 0));

			right = new Animation(name, () => turret.turretFacing);
			right.Play("right");
			anims.Add("right", new AnimationWithOffset(right, null, () => facing.Facing <= 128, 0));

			var leftWake = new Animation(name);
			leftWake.Play("wake-left");
			anims.Add("wake-left", new AnimationWithOffset(leftWake, null, () => facing.Facing > 128, -87));

			var rightWake = new Animation(name);
			rightWake.Play("wake-right");
			anims.Add("wake-right", new AnimationWithOffset(rightWake, null, () => facing.Facing <= 128, -87));

			self.Trait<IBodyOrientation>().SetAutodetectedFacings(2);
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			left.ReplaceAnim(NormalizeSequence(left, e.DamageState, "left"));
			right.ReplaceAnim(NormalizeSequence(right, e.DamageState, "right"));
		}
	}
}
