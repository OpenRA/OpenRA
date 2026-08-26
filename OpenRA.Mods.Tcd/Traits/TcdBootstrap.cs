#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Proves the Tactics & Command Dynamics assembly is loaded and its traits are",
		"constructible. Attach to the world actor. Superseded by SquadManager in sprint 02.")]
	public sealed class TcdBootstrapInfo : TraitInfo
	{
		[Desc("Maximum number of actors allowed in a single squad.")]
		public readonly int MaxSquadSize = 12;

		public override object Create(ActorInitializer init) { return new TcdBootstrap(this); }
	}

	public sealed class TcdBootstrap : INotifyCreated
	{
		readonly TcdBootstrapInfo info;

		public TcdBootstrap(TcdBootstrapInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Log.Write("debug", $"[TCD] OpenRA.Mods.Tcd loaded. MaxSquadSize={info.MaxSquadSize}");
		}
	}
}
