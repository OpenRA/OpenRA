#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Orders;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.yupgi_alert.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;
using static OpenRA.Mods.Common.Traits.Harvester;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Slaved to SpawnerHarvester")]
	class SpawnedHarvesterInfo : ITraitInfo, Requires<HarvesterInfo>
	{
		public object Create(ActorInitializer init) { return new SpawnedHarvester(init, this); }
	}

	class SpawnedHarvester : IIssueOrder, IResolveOrder, INotifyKilled, INotifyBecomingIdle
	{
		readonly SpawnedHarvesterInfo info;
		public Actor Master = null;

		public SpawnedHarvester(ActorInitializer init, SpawnedHarvesterInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield break; }
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// If killed, I tell my master that I'm gone.
			if (Master == null || Master.Disposed || Master.IsDead)
				// Can happen, when built from build palette (w00t)
				return;
			var spawner = Master.Trait<SpawnerHarvester>();
			spawner.SlaveKilled(Master, self);
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			// don't mind too much about this part.
			// Everything is (or, should be.) automatically ordered properly by the master.
			if (order.OrderID != "SpawnedReturn")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return null;

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			var spawned = self.Trait<Spawned>();
			return order.TargetActor == spawned.Master;
		}

		public void ResolveOrder(Actor self, Order order)
		{
		}

		public virtual void OnBecomingIdle(Actor self)
		{
		}
	}
}