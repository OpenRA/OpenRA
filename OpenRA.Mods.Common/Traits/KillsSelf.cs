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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class KillsSelfInfo : ConditionalTraitInfo
	{
		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		[Desc("The amount of time (in ticks) before the actor dies. Two values indicate a range between which a random value is chosen.")]
		public readonly int[] Delay = { 0 };

		[GrantedConditionReference]
		[Desc("The condition to grant moments before suiciding.")]
		public readonly string GrantsCondition = null;

		public override object Create(ActorInitializer init) { return new KillsSelf(init.Self, this); }
	}

	class KillsSelf : ConditionalTrait<KillsSelfInfo>, INotifyCreated, INotifyAddedToWorld, ITick
	{
		int lifetime;
		ConditionManager conditionManager;

		public KillsSelf(Actor self, KillsSelfInfo info)
			: base(info)
		{
			lifetime = Util.RandomDelay(self.World, info.Delay);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Actors can be created without being added to the world
			// We want to make sure that this only triggers once they are inserted into the world
			if (lifetime == 0 && self.IsInWorld)
				Kill(self);
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitEnabled(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || self.IsDead || IsTraitDisabled)
				return;

			if (!self.World.Map.Contains(self.Location))
				return;

			if (lifetime-- <= 0)
				Kill(self);
		}

		void Kill(Actor self)
		{
			if (self.IsDead)
				return;

			if (conditionManager != null && !string.IsNullOrEmpty(Info.GrantsCondition))
				conditionManager.GrantCondition(self, Info.GrantsCondition);

			if (Info.RemoveInstead || !self.Info.HasTraitInfo<HealthInfo>())
				self.Dispose();
			else
				self.Kill(self);
		}
	}
}
