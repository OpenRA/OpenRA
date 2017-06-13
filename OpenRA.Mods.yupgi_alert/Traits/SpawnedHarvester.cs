using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
Works without base engine modification...
But the docking procedure may need to change to fit your needs.
In OP Mod, docking changed for Harvester.cs and related files to that
these slaves can "dock" to any adjacent cells near the master.
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public class SpawnedHarvesterInfo : ITraitInfo, Requires<HarvesterInfo>
	{
		[GrantedConditionReference]
		[Desc("The condition to grant when freed")]
		public readonly string FreedCondition = null;

		public object Create(ActorInitializer init)
		{
			return new SpawnedHarvester(init, this);
		}
	}

	class SpawnedHarvester : INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing, INotifyCreated
	{
		readonly SpawnedHarvesterInfo info;

		Harvester harv;
		ConditionManager conditionManager;
		int freedToken = ConditionManager.InvalidConditionToken;
		bool freed = false; // When ownership changed, is it freed or is it MC or what?

		public SpawnedHarvester(ActorInitializer init, SpawnedHarvesterInfo spawnedHarvesterInfo)
		{
			harv = init.Self.Trait<Harvester>();
			info = spawnedHarvesterInfo;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
		}

		void NotifyRemoval(Actor self)
		{
			var master = harv.Master;
			if (master != null && !master.IsDead && !master.Disposed)
				master.Trait<SpawnerHarvester>().SpawnedRemoved(master, self);
		}

		public void Disposing(Actor self)
		{
			NotifyRemoval(self);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			NotifyRemoval(self);
		}

		public void Unslave(Actor self, Actor attacker)
		{
			self.CancelActivity();

			// Transfer ownership.
			freed = true; // set this flag to true before changing owner so that slave won't self destruct.
			self.ChangeOwner(attacker.Owner);

			// Give condition.
			if (conditionManager != null && freedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.FreedCondition))
				freedToken = conditionManager.GrantCondition(self, info.FreedCondition);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (freed)
				return;

			/*
			Here's what happens.
			1. Slave gets MC'ed : dies.
			2. Master gets MC'ed : master's OnOwnerChanged changes owner of this so we get controlled too. Nothing happens.
			3. We get freed. Nothing happens.
			*/

			// Can't live without master... TODO: Maybe get them controlled too?
			self.World.AddFrameEndTask(w =>
			{
				if (harv.Master.Owner != self.Owner)
					self.Kill(newOwner.PlayerActor);
			});
		}
	}
}
