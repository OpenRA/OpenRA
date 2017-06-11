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
		public object Create(ActorInitializer init)
		{
			return new SpawnedHarvester(init, this);
		}
	}

	class SpawnedHarvester : INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing
	{
		Harvester harv;

		public SpawnedHarvester(ActorInitializer init, SpawnedHarvesterInfo spawnedHarvesterInfo)
		{
			harv = init.Self.Trait<Harvester>();
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

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Can't live without master... TODO: Maybe get them controlled too?
			self.World.AddFrameEndTask(w =>
			{
				if (harv.Master.Owner != self.Owner)
					self.Kill(newOwner.PlayerActor);
			});
		}
	}
}
