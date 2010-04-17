using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	class SpawnMapActors : IGameStarted
	{
		public Dictionary<string, Actor> MapActors = new Dictionary<string, Actor>();

		public void GameStarted(World world)
		{
			Game.skipMakeAnims = true;		// rude hack

			foreach (var actorReference in world.Map.Actors)
				MapActors[actorReference.Key] = world.CreateActor(actorReference.Value.Name, actorReference.Value.Location,
					world.players.Values.FirstOrDefault(p => p.InternalName == actorReference.Value.Owner)
					?? world.NeutralPlayer);

			Game.skipMakeAnims = false;
		}
	}
}
