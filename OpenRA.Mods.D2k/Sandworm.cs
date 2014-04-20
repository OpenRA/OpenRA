#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	class SandwormInfo : ITraitInfo, Requires<RenderUnitInfo>
	{
		public readonly string MouthSequence = "mouth";
		public readonly string SandSequence = "sand";
		
		public object Create(ActorInitializer init) { return new Sandworm(init.self, this); }
	}

	class Sandworm : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly SandwormInfo info;
		readonly RenderUnit renderUnit;

		public Sandworm(Actor self, SandwormInfo info)
		{
			this.info = info;
			renderUnit = self.Trait<RenderUnit>();
			Sound.PlayNotification(self.Owner, "Speech", "WormSign", self.Owner.Country.Race);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "Detonate")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return "Attack";
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Detonate")
			{
				if (info.SandSequence != null)
					renderUnit.PlayCustomAnim(self, info.SandSequence);

				if (info.MouthSequence != null)
					renderUnit.PlayCustomAnim(self, info.MouthSequence);

				Sound.PlayNotification(self.Owner, "Speech", "WormAttack", self.Owner.Country.Race);
			}
		}
	}
}
