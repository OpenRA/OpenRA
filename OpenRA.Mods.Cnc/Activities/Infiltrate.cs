#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	class Infiltrate : Enter
	{
		readonly Actor target;
		readonly Infiltrates infiltrates;
		readonly Cloak cloak;

		public Infiltrate(Actor self, Actor target, Infiltrates infiltrate)
			: base(self, target, infiltrate.Info.EnterBehaviour)
		{
			this.target = target;
			infiltrates  = infiltrate;
			cloak = self.TraitOrDefault<Cloak>();
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead)
				return;

			var stance = self.Owner.Stances[target.Owner];
			if (!infiltrates.Info.ValidStances.HasStance(stance))
				return;

			if (cloak != null && cloak.Info.UncloakOn.HasFlag(UncloakType.Infiltrate))
				cloak.Uncloak();

			foreach (var t in target.TraitsImplementing<INotifyInfiltrated>())
				t.Infiltrated(target, self);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null)
				exp.GiveExperience(infiltrates.Info.PlayerExperience);

			if (!string.IsNullOrEmpty(infiltrates.Info.Notification))
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					infiltrates.Info.Notification, self.Owner.Faction.InternalName);
		}

		public override Activity Tick(Actor self)
		{
			if (infiltrates.IsTraitDisabled)
				Cancel(self);

			return base.Tick(self);
		}

		public override TargetLineNode TargetLineNode(Actor self)
		{
			return new TargetLineNode(Target, Color.Yellow, NextActivity);
		}
	}
}
