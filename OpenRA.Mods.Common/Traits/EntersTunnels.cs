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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can interact with TunnelEntrances to move through TerrainTunnels.")]
	public class EntersTunnelsInfo : ITraitInfo, Requires<IMoveInfo>
	{
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new EntersTunnels(init.Self, this); }
	}

	public class EntersTunnels : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly EntersTunnelsInfo info;
		readonly IMove move;

		public EntersTunnels(Actor self, EntersTunnelsInfo info)
		{
			this.info = info;
			move = self.Trait<IMove>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterTunnelOrderTargeter(info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "EnterTunnel")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID, SuppressVisualFeedback = true };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor, SuppressVisualFeedback = true };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterTunnel" ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTunnel")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			var tunnel = target.Actor.TraitOrDefault<TunnelEntrance>();
			if (!tunnel.Exit.HasValue)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(Target.FromCell(self.World, tunnel.Exit.Value), Color.Green);
			self.QueueActivity(move.MoveTo(tunnel.Entrance, tunnel.NearEnough));
			self.QueueActivity(move.MoveTo(tunnel.Exit.Value, tunnel.NearEnough));
		}

		class EnterTunnelOrderTargeter : UnitOrderTargeter
		{
			readonly EntersTunnelsInfo info;

			public EnterTunnelOrderTargeter(EntersTunnelsInfo info)
				: base("EnterTunnel", 6, info.EnterCursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target == null || target.IsDead)
					return false;

				var tunnel = target.TraitOrDefault<TunnelEntrance>();
				if (tunnel == null)
					return false;

				// HACK: The engine does not support HiddenUnderFog combined with buildings that use the "_" footprint
				// We therefore have to use AlwaysVisible and then force-disable interacting with the entrance under shroud
				var buildingInfo = target.Info.TraitInfoOrDefault<BuildingInfo>();
				if (buildingInfo != null)
				{
					var footprint = FootprintUtils.PathableTiles(target.Info.Name, buildingInfo, target.Location);
					if (footprint.All(c => self.World.ShroudObscures(c)))
						return false;
				}

				if (!tunnel.Exit.HasValue)
				{
					cursor = info.EnterBlockedCursor;
					return false;
				}

				cursor = info.EnterCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return CanTargetActor(self, target.Actor, modifiers, ref cursor);
			}
		}
	}
}
