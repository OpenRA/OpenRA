#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class InfiltrateHijackInfo : TraitInfo<InfiltrateHijack>, Requires<InfiltratableInfo> { }

	class InfiltrateHijack : IAcceptInfiltrator, INotifyKilled
	{
		Actor currentHijacker;

		public void OnInfiltrate(Actor self, Actor infiltrator)
		{
			if (currentHijacker != null && !currentHijacker.IsDead())
				currentHijacker.Kill(infiltrator);

			currentHijacker = infiltrator;

			var cargo = self.TraitOrDefault<Cargo>();
			if (cargo != null)
			{
				var fakeAttack = new AttackInfo
				{
					Attacker = infiltrator,
					Damage = 100,
					DamageState = self.GetDamageState(),
					PreviousDamageState = self.GetDamageState(),
					Warhead = null,
				};
				cargo.Killed(self, fakeAttack);
			}

			self.ChangeOwner(currentHijacker.Owner);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (currentHijacker != null && !currentHijacker.IsDead())
			{
				self.World.Add(currentHijacker);
				var mobile = currentHijacker.Trait<Mobile>();
				mobile.SetPosition(currentHijacker, self.Location);
				mobile.Nudge(currentHijacker, currentHijacker, true);
			}
		}
	}
}
