#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Bot that uses BotModules.")]
	public sealed class ModularBotInfo : IBotInfo, ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Internal id for this bot.")]
		public readonly string Type = null;

		[Desc("Human-readable name this bot uses.")]
		public readonly string Name = "Unnamed Bot";

		[Desc("Minimum portion of pending orders to issue each tick (e.g. 5 issues at least 1/5th of all pending orders). Excess orders remain queued for subsequent ticks.")]
		public readonly int MinOrderQuotientPerTick = 5;

		string IBotInfo.Type { get { return Type; } }

		string IBotInfo.Name { get { return Name; } }

		public object Create(ActorInitializer init) { return new ModularBot(this, init); }
	}

	public sealed class ModularBot : ITick, IBot, INotifyDamage
	{
		public bool IsEnabled;

		readonly ModularBotInfo info;
		readonly World world;
		readonly Queue<Order> orders = new Queue<Order>();

		Player player;

		IBotTick[] tickModules;
		IBotRespondToAttack[] attackResponseModules;

		IBotInfo IBot.Info { get { return info; } }
		Player IBot.Player { get { return player; } }

		public ModularBot(ModularBotInfo info, ActorInitializer init)
		{
			this.info = info;
			this.world = init.World;
		}

		// Called by the host's player creation code
		public void Activate(Player p)
		{
			IsEnabled = true;
			player = p;
			tickModules = p.PlayerActor.TraitsImplementing<IBotTick>().ToArray();
			attackResponseModules = p.PlayerActor.TraitsImplementing<IBotRespondToAttack>().ToArray();
		}

		void IBot.QueueOrder(Order order)
		{
			orders.Enqueue(order);
		}

		void ITick.Tick(Actor self)
		{
			if (!IsEnabled)
				return;

			using (new PerfSample("bot_tick"))
			{
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, world, () =>
				{
					foreach (var t in tickModules)
						if (t.IsTraitEnabled())
							t.BotTick(this);
				});
			}

			var ordersToIssueThisTick = Math.Min((orders.Count + info.MinOrderQuotientPerTick - 1) / info.MinOrderQuotientPerTick, orders.Count);
			for (var i = 0; i < ordersToIssueThisTick; i++)
				world.IssueOrder(orders.Dequeue());
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!IsEnabled)
				return;

			using (new PerfSample("bot_attack_response"))
			{
				Sync.RunUnsynced(Game.Settings.Debug.SyncCheckBotModuleCode, world, () =>
				{
					foreach (var t in attackResponseModules)
						if (t.IsTraitEnabled())
							t.RespondToAttack(this, self, e);
				});
			}
		}
	}
}
