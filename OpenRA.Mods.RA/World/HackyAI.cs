using System.Linq;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA
{
	class HackyAIInfo : TraitInfo<HackyAI> { }

	/* a pile of hacks, which control the local player on the host. */

	class HackyAI : IGameStarted, ITick
	{
		bool enabled;
		int ticks;
		Player p;
		ProductionQueue pq;
		bool isBuildingStuff;

		enum BuildState
		{
			ChooseItem,
			WaitForProduction,
			WaitForFeedback,
		}

		int lastThinkTick = 0;

		BuildState state = BuildState.WaitForFeedback;

		public void GameStarted(World w)
		{
			p = Game.world.LocalPlayer;
			enabled = Game.IsHost && p != null; 
			if (enabled)
				pq = p.PlayerActor.traits.Get<ProductionQueue>();
		}

		string ChooseItemToBuild()
		{
			return "powr";		// LOTS OF POWER
		}

		int2? ChooseBuildLocation(ProductionItem item)
		{
			return null;		// i don't know where to put it.
		}

		const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

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

			var currentBuilding = pq.CurrentItem("Building");
			switch (state)
			{
				case BuildState.ChooseItem:
					{
						var item = ChooseItemToBuild();
						if (item == null)
						{
							state = BuildState.WaitForFeedback;
							lastThinkTick = ticks;
						}
						else
						{
							state = BuildState.WaitForProduction;
							Game.IssueOrder(Order.StartProduction(p, item, 1));
						}
					}
					break;

				case BuildState.WaitForProduction:
					if (currentBuilding == null) return;	/* let it happen.. */

					else if (currentBuilding.Paused)
						Game.IssueOrder(Order.PauseProduction(p, currentBuilding.Item, false));
					else if (currentBuilding.Done)
					{
						state = BuildState.WaitForFeedback;
						lastThinkTick = ticks;

						/* place the building */
						var location = ChooseBuildLocation(currentBuilding);
						if (location == null)
							Game.IssueOrder(Order.CancelProduction(p, currentBuilding.Item));
						else
						{
							// todo: place the building!
							throw new NotImplementedException();
						}
					}
					break;

				case BuildState.WaitForFeedback:
					if (ticks - lastThinkTick > feedbackTime)
						state = BuildState.ChooseItem;
					break;
			}
		}
	}
}
