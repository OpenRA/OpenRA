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
		public readonly int[] Delay = { 250 };

		public override object Create(ActorInitializer init) { return new KillsSelf(init.Self, this); }
	}

	class KillsSelf : ConditionalTrait<KillsSelfInfo>, INotifyAddedToWorld, ITick
	{
		int lifetime;

		public KillsSelf(Actor self, KillsSelfInfo info)
			: base(info)
		{
			lifetime = Util.RandomDelay(self.World, info.Delay);
		}

		public void AddedToWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitEnabled(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (self.IsDead)
				return;

			if (lifetime > 0)
				return;

			if (Info.RemoveInstead || !self.Info.HasTraitInfo<HealthInfo>())
				self.Dispose();
			else
				self.Kill(self);
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			if (!self.World.Map.Contains(self.Location))
				return;

			if (lifetime-- == 0)
			{
				if (Info.RemoveInstead || !self.Info.HasTraitInfo<HealthInfo>())
					self.Dispose();
				else
					self.Kill(self);
			}
		}
	}
}
