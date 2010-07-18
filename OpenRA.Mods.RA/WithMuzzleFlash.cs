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

namespace OpenRA.Mods.RA
{
	class WithMuzzleFlashInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(ActorInitializer init) { return new WithMuzzleFlash(init.self); }
	}

	class WithMuzzleFlash : INotifyAttack
	{
		Animation muzzleFlash;
		bool isShowing;

		public WithMuzzleFlash(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			var attackInfo = self.Info.Traits.Get<AttackBaseInfo>();
			var render = self.traits.Get<RenderSimple>();

			muzzleFlash = new Animation(render.GetImage(self), () => unit.Facing);
			muzzleFlash.Play("muzzle");

			render.anims.Add("muzzle", new RenderSimple.AnimationWithOffset(
				muzzleFlash,
				() => attackInfo.PrimaryOffset.AbsOffset(),
				() => !isShowing));
		}

		public void Attacking(Actor self)
		{
			isShowing = true;
			muzzleFlash.PlayThen("muzzle", () => isShowing = false);
		}
	}
}
