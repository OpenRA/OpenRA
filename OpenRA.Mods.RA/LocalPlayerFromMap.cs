#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class LocalPlayerFromMapInfo : TraitInfo<LocalPlayerFromMap>, ITraitPrerequisite<CreateMapPlayersInfo>
	{
		public readonly string Player = "GoodGuy";
	}

	class LocalPlayerFromMap: ICreatePlayers
	{			
		public void CreatePlayers(World w)
		{
			var name = w.WorldActor.Info.Traits.Get<LocalPlayerFromMapInfo>().Player;
			var player = w.WorldActor.Trait<CreateMapPlayers>().Players[name];

			// Hack: the player *must* be keyed by LocalClientId for orders to be processed correctly
			var local = w.players.FirstOrDefault(p => p.Value == player);
			w.players.Remove(local.Key);
			w.players.Add(Game.LocalClientId,local.Value);
			w.SetLocalPlayer(Game.LocalClientId);
		}
	}
}
