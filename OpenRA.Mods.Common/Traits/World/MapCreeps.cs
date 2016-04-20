#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the 'Creeps' checkbox in the lobby options.")]
	public class MapCreepsInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the creeps checkbox in the lobby.")]
		public readonly bool Enabled = true;

		[Desc("Prevent the creeps state from being changed in the lobby.")]
		public readonly bool Locked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("creeps", "Worms", Enabled, Locked);
		}

		public object Create(ActorInitializer init) { return new MapCreeps(this); }
	}

	public class MapCreeps : INotifyCreated
	{
		readonly MapCreepsInfo info;
		public bool Enabled { get; private set; }

		public MapCreeps(MapCreepsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("creeps", info.Enabled);
		}
	}
}
