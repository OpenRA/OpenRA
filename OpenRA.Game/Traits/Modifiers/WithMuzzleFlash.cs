#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA.Traits
{
	class WithMuzzleFlashInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(Actor self) { return new WithMuzzleFlash(self); }
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
			//var len = muzzleFlash.CurrentSequence.Length;

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
