using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.yupgi_alert.Traits
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
			// Can't live without master, examine if I can live on.
			self.World.AddFrameEndTask(w =>
			{
				if (harv.Master.Owner != self.Owner)
					self.Kill(newOwner.PlayerActor);
			});
		}
	}
}
