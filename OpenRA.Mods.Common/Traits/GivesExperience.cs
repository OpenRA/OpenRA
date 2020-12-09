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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor.")]
		public readonly int ActorExperienceModifier = 10000;

		[Desc("Percentage of the `Experience` value that is being granted to the player owning the killing actor.")]
		public readonly int PlayerExperienceModifier = 0;

		public override object Create(ActorInitializer init) { return new GivesExperience(init.Self, this); }
	}

	class GivesExperience : INotifyKilled, INotifyCreated
	{
		readonly GivesExperienceInfo info;

		int exp;
		IEnumerable<int> experienceModifiers;

		public GivesExperience(Actor self, GivesExperienceInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			exp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			experienceModifiers = self.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier());
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (exp == 0 || e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidRelationships.HasStance(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			exp = Util.ApplyPercentageModifiers(exp, experienceModifiers);

			var killer = e.Attacker.TraitOrDefault<GainsExperience>();
			if (killer != null)
			{
				var killerExperienceModifier = e.Attacker.TraitsImplementing<IGainsExperienceModifier>()
					.Select(x => x.GetGainsExperienceModifier()).Append(info.ActorExperienceModifier);
				killer.GiveExperience(Util.ApplyPercentageModifiers(exp, killerExperienceModifier));
			}

			e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()
				?.GiveExperience(Util.ApplyPercentageModifiers(exp, new[] { info.PlayerExperienceModifier }));
		}
	}
}
