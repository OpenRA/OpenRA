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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows the player to issue the orders from actors with " + nameof(IssueOrderToBot) + ".")]
	public class ExternalBotOrdersManagerInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ExternalBotOrdersManager(init.Self, this); }
	}

	public class ExternalBotOrdersManager : ConditionalTrait<ExternalBotOrdersManagerInfo>, IBotTick
	{
		readonly List<(Actor Actor, string Order, int Chance)> entries = new();
		readonly World world;

		public bool ManagerRunning { get; private set; }

		public ExternalBotOrdersManager(Actor self, ExternalBotOrdersManagerInfo info)
			: base(info)
		{
			world = self.World;
			ManagerRunning = false;
		}

		public void AddEntry(Actor issuer, string order, int chance)
		{
			entries.Add(new(issuer, order, chance));
		}

		void IBotTick.BotTick(IBot bot)
		{
			// "ManagerRunning = true" means IBotTick is running, and the game is
			// 1. not a replay
			// 2. not saved game still loading
			// 3. the game running on the host where AI is enabled
			ManagerRunning = true;

			foreach (var entry in entries)
			{
				if (entry.Actor.IsDead || !entry.Actor.IsInWorld || entry.Actor.Owner != bot.Player)
					continue;

				if (world.LocalRandom.Next(100) > entry.Chance)
					continue;

				bot.QueueOrder(new(entry.Order, entry.Actor, false));
			}

			entries.Clear();
		}
	}
}
