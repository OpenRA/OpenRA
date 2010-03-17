using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class SpawnMapActorsInfo : StatelessTraitInfo<SpawnMapActors> { }

	class SpawnMapActors : IGameStarted
	{
		public void GameStarted(World world)
		{
			Game.skipMakeAnims = true;		// rude hack

			foreach (var actorReference in world.Map.Actors)
				world.CreateActor(actorReference.Name, actorReference.Location,
					world.players.Values.FirstOrDefault(p => p.InternalName == actorReference.Owner)
					?? world.NeutralPlayer);

			Game.skipMakeAnims = false;
		}
	}
}
