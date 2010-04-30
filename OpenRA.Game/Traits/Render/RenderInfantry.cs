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

using OpenRA.Effects;

namespace OpenRA.Traits
{
	public class RenderInfantryInfo : RenderSimpleInfo
	{
		public override object Create(Actor self) { return new RenderInfantry(self); }
	}

	public class RenderInfantry : RenderSimple, INotifyAttack, INotifyDamage
	{
		public RenderInfantry(Actor self)
			: base(self, () => self.traits.Get<Unit>().Facing)
		{
			anim.Play("stand");
		}

		bool ChooseMoveAnim(Actor self)
		{
			if (!(self.GetCurrentActivity() is Activities.Move))
				return false;

			var mobile = self.traits.Get<Mobile>();
			if (float2.WithinEpsilon(self.CenterLocation, Util.CenterOfCell(mobile.toCell), 2)) return false;

			var seq = IsProne(self) ? "crawl" : "run";

			if (anim.CurrentSequence.Name != seq)
				anim.PlayRepeating(seq);

			return true;
		}

		bool inAttack = false;
		bool IsProne(Actor self)
		{
			var takeCover = self.traits.GetOrDefault<TakeCover>();
			return takeCover != null && takeCover.IsProne;
		}

		public void Attacking(Actor self)
		{
			inAttack = true;

			var seq = IsProne(self) ? "prone-shoot" : "shoot";

			if (anim.HasSequence(seq))
				anim.PlayThen(seq, () => inAttack = false);
			else if (anim.HasSequence("heal"))
				anim.PlayThen("heal", () => inAttack = false);
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (inAttack) return;
			if (ChooseMoveAnim(self)) return;

			/* todo: idle anims, etc */

			if (IsProne(self))
				anim.PlayFetchIndex("crawl", () => 0);			/* what a hack. */
			else
				anim.Play("stand");
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
			{
				var death = e.Warhead != null ? e.Warhead.InfDeath : 0;
				Sound.PlayVoice("Die", self);
				self.World.AddFrameEndTask(w => w.Add(new Corpse(self, death)));
			}
		}
	}
}
