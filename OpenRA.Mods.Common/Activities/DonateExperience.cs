#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class DonateExperience : Enter
	{
		readonly int level;
		readonly int playerExperience;

		Actor enterActor;
		GainsExperience enterGainsExperience;

		public DonateExperience(Actor self, in Target target, int level, int playerExperience, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.level = level;
			this.playerExperience = playerExperience;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterGainsExperience = targetActor.TraitOrDefault<GainsExperience>();

			// Make sure the target actor is still owned by a player with eligible relationship
			var isTargetStillValid = true;
			var targetInfo = targetActor.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
			if (targetInfo == null || !targetInfo.ValidRelationships.HasRelationship(targetActor.Owner.RelationshipWith(self.Owner)))
				isTargetStillValid = false;

			if (enterGainsExperience == null || enterGainsExperience.Level == enterGainsExperience.MaxLevel || !isTargetStillValid)
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// Make sure the target hasn't changed while entering
			// OnEnterComplete is only called if targetActor is alive
			if (targetActor != enterActor)
				return;

			// Make sure the target actor is still owned by a player with eligible relationship
			var isTargetStillValid = true;
			var targetInfo = targetActor.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
			if (targetInfo == null || !targetInfo.ValidRelationships.HasRelationship(targetActor.Owner.RelationshipWith(self.Owner)))
				isTargetStillValid = false;

			if (enterGainsExperience.Level == enterGainsExperience.MaxLevel || !isTargetStillValid)
				return;

			enterGainsExperience.GiveLevels(level);

			var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
			if (exp != null && enterActor.Owner != self.Owner)
				exp.GiveExperience(playerExperience);

			self.Dispose();
		}
	}
}
