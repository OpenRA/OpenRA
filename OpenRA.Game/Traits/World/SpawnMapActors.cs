using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	class SpawnMapActors : IGameStarted
	{
		public void GameStarted(World world)
		{
			Game.skipMakeAnims = true;		// rude hack
			// TODO: Keep a dictionary of actor reference -> actor somewhere for scripting purposes
			foreach (var actorReference in world.Map.Actors)
				world.CreateActor(actorReference.Value.Name, actorReference.Value.Location,
					world.players.Values.FirstOrDefault(p => p.InternalName == actorReference.Value.Owner)
					?? world.NeutralPlayer);

			Game.skipMakeAnims = false;
		}
	}
}
