#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : Enter
	{
		readonly Actor target;

		readonly Cloak cloak;

		readonly Infiltrates infiltrates;

		public Infiltrate(Actor self, Actor target)
			: base(self, target)
		{
			this.target = target;

			cloak = self.TraitOrDefault<Cloak>();

			infiltrates = self.TraitOrDefault<Infiltrates>();
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead)
				return;

			var stance = self.Owner.Stances[target.Owner];
			if (!infiltrates.Info.ValidStances.HasStance(stance))
				return;

			if (cloak != null && cloak.Info.UncloakOnInfiltrate)
				cloak.Uncloak();

			foreach (var t in target.TraitsImplementing<INotifyInfiltrated>())
				t.Infiltrated(target, self);

			self.Dispose();

			if (target.Info.HasTraitInfo<BuildingInfo>())
				Game.Sound.PlayToPlayer(self.Owner, "bldginf1.aud");
		}
	}
}
