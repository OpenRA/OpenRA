#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Infiltrate : Enter
	{
		readonly Actor target;
		readonly Stance validStances;
		readonly Cloak cloak;
		readonly string notification;

		public Infiltrate(Actor self, Actor target, EnterBehaviour enterBehaviour, Stance validStances, string notification)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
			this.validStances = validStances;
			this.notification = notification;
			cloak = self.TraitOrDefault<Cloak>();
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead)
				return;

			var stance = self.Owner.Stances[target.Owner];
			if (!validStances.HasStance(stance))
				return;

			if (cloak != null && cloak.Info.UncloakOn.HasFlag(UncloakType.Infiltrate))
				cloak.Uncloak();

			foreach (var t in target.TraitsImplementing<INotifyInfiltrated>())
				t.Infiltrated(target, self);

			if (!string.IsNullOrEmpty(notification))
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					notification, self.Owner.Faction.InternalName);
		}
	}
}
