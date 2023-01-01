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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A placeholder bot that doesn't do anything.")]
	[TraitLocation(SystemActors.Player)]
	public sealed class DummyBotInfo : TraitInfo, IBotInfo
	{
		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		string IBotInfo.Type => Type;

		string IBotInfo.Name => Name;

		public override object Create(ActorInitializer init) { return new DummyBot(this); }
	}

	public sealed class DummyBot : IBot
	{
		readonly DummyBotInfo info;
		public bool IsEnabled { get; private set; }
		Player player;

		public DummyBot(DummyBotInfo info)
		{
			this.info = info;
		}

		void IBot.Activate(Player p)
		{
			IsEnabled = true;
			player = p;
		}

		void IBot.QueueOrder(Order order) { }

		IBotInfo IBot.Info => info;
		Player IBot.Player => player;
	}
}
