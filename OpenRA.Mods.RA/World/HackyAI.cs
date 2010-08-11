using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class HackyAIInfo : TraitInfo<HackyAI> { }

	/* a pile of hacks, which control the local player on the host. */

	class HackyAI : IGameStarted, ITick
	{
		bool enabled;
		int ticks;
		Player p;

		public void GameStarted(World w)
		{
			enabled = Game.IsHost; 
			p = Game.world.LocalPlayer;
		}

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

			if (ticks == 10)
			{
				/* find our mcv and deploy it */
				var mcv = self.World.Queries.OwnedBy[p]
					.FirstOrDefault(a => a.Info == Rules.Info["mcv"]);

				if (mcv != null)
					Game.IssueOrder(new Order("DeployTransform", mcv));
				else
					Game.Debug("AI: Can't find the MCV.");
			}
		}
	}
}
