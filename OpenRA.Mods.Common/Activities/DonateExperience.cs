#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class DonateExperience : LegacyEnter
	{
		readonly Actor target;
		readonly GainsExperience targetGainsExperience;
		readonly int level;
		readonly int playerExperience;

		public DonateExperience(Actor self, Actor target, int level, int playerExperience, GainsExperience targetGainsExperience)
			: base(self, target, EnterBehaviour.Dispose, targetLineColor: Color.Yellow)
		{
			this.target = target;
			this.level = level;
			this.playerExperience = playerExperience;
			this.targetGainsExperience = targetGainsExperience;
		}

		protected override void OnInside(Actor self)
		{
			if (target.IsDead)
				return;

			targetGainsExperience.GiveLevels(level);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null && target.Owner != self.Owner)
				exp.GiveExperience(playerExperience);
		}

		public override Activity Tick(Actor self)
		{
			if (target.IsDead || targetGainsExperience.Level == targetGainsExperience.MaxLevel)
				Cancel(self);

			return base.Tick(self);
		}
	}
}
