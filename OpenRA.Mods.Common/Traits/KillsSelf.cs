#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class KillsSelfInfo : ConditionalTraitInfo
	{
		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		[Desc("The amount of time (in ticks) before the actor dies. Two values indicate a range between which a random value is chosen.")]
		public readonly int[] Delay = { 0 };

		[Desc("Types of damage that this trait causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[GrantedConditionReference]
		[Desc("The condition to grant moments before suiciding.")]
		public readonly string GrantsCondition = null;

		public override object Create(ActorInitializer init) { return new KillsSelf(init.Self, this); }
	}

	class KillsSelf : ConditionalTrait<KillsSelfInfo>, INotifyAddedToWorld, ITick
	{
		int lifetime;

		public KillsSelf(Actor self, KillsSelfInfo info)
			: base(info)
		{
			lifetime = Util.RandomInRange(self.World.SharedRandom, info.Delay);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Actors can be created without being added to the world
			// We want to make sure that this only triggers once they are inserted into the world
			if (lifetime == 0 && self.IsInWorld)
				self.World.AddFrameEndTask(w => Kill(self));
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
				self.World.AddFrameEndTask(w => Kill(self));
		}

		void Kill(Actor self)
		{
			if (self.IsDead)
				return;

			self.GrantCondition(Info.GrantsCondition);

			if (Info.RemoveInstead || !self.Info.HasTraitInfo<IHealthInfo>())
				self.Dispose();
			else
				self.Kill(self, Info.DamageTypes);
		}
	}
}
