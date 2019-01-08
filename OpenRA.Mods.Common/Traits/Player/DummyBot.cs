#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public sealed class DummyBotInfo : ITraitInfo, IBotInfo
	{
		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		string IBotInfo.Type { get { return Type; } }

		string IBotInfo.Name { get { return Name; } }

		public object Create(ActorInitializer init) { return new DummyBot(this); }
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

		IBotInfo IBot.Info { get { return info; } }
		Player IBot.Player { get { return player; } }
	}
}
