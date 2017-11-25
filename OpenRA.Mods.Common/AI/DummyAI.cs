#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public sealed class DummyAIInfo : ITraitInfo, IBotInfo
	{
		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		string IBotInfo.Type { get { return Type; } }

		string IBotInfo.Name { get { return Name; } }

		public object Create(ActorInitializer init) { return new DummyAI(this); }
	}

	public sealed class DummyAI : IBot
	{
		readonly DummyAIInfo info;
		public bool Enabled { get; private set; }

		public DummyAI(DummyAIInfo info)
		{
			this.info = info;
		}

		void IBot.Activate(Player p)
		{
			Enabled = true;
		}

		IBotInfo IBot.Info { get { return info; } }
	}
}
