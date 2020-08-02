#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can interact with TunnelEntrances to move through TerrainTunnels.")]
	public class EntersTunnelsInfo : TraitInfo, Requires<IMoveInfo>, IObservesVariablesInfo
	{
		[Desc("Cursor to display when able to enter target tunnel.")]
		public readonly string EnterCursor = "enter";

		[Desc("Cursor to display when unable to enter target tunnel.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line while in tunnels.")]
		public readonly Color TargetLineColor = Color.Green;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		public override object Create(ActorInitializer init) { return new EntersTunnels(init.Self, this); }
	}

	public class EntersTunnels : IIssueOrder, IResolveOrder, IOrderVoice, IObservesVariables
	{
		readonly EntersTunnelsInfo info;
		readonly IMove move;
		readonly IMoveInfo moveInfo;
		bool requireForceMove;

		public EntersTunnels(Actor self, EntersTunnelsInfo info)
		{
			this.info = info;
			move = self.Trait<IMove>();
			moveInfo = self.Info.TraitInfo<IMoveInfo>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterTunnelOrderTargeter(info.EnterCursor, info.EnterBlockedCursor, CanEnterTunnel, _ => true);
			}
		}

		bool CanEnterTunnel(Actor target, TargetModifiers modifiers)
		{
			return !requireForceMove || modifiers.HasModifier(TargetModifiers.ForceMove);
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "EnterTunnel")
				return null;

			return new Order(order.OrderID, self, target, queued) { SuppressVisualFeedback = true };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterTunnel" ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterTunnel" || order.Target.Type != TargetType.Actor)
				return;

			var tunnel = order.Target.Actor.TraitOrDefault<TunnelEntrance>();
			if (tunnel == null || !tunnel.Exit.HasValue)
				return;

			self.QueueActivity(order.Queued, move.MoveTo(tunnel.Entrance, tunnel.NearEnough, targetLineColor: moveInfo.GetTargetLineColor()));
			self.QueueActivity(move.MoveTo(tunnel.Exit.Value, tunnel.NearEnough, targetLineColor: info.TargetLineColor));
			self.ShowTargetLines();
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = info.RequireForceMoveCondition.Evaluate(conditions);
		}

		public class EnterTunnelOrderTargeter : UnitOrderTargeter
		{
			readonly string enterCursor;
			readonly string enterBlockedCursor;
			readonly Func<Actor, TargetModifiers, bool> canTarget;
			readonly Func<Actor, bool> useEnterCursor;

			public EnterTunnelOrderTargeter(string enterCursor, string enterBlockedCursor,
				Func<Actor, TargetModifiers, bool> canTarget, Func<Actor, bool> useEnterCursor)
				: base("EnterTunnel", 6, enterCursor, true, true)
			{
				this.enterCursor = enterCursor;
				this.enterBlockedCursor = enterBlockedCursor;
				this.canTarget = canTarget;
				this.useEnterCursor = useEnterCursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target == null || target.IsDead || !canTarget(target, modifiers))
					return false;

				var tunnel = target.TraitOrDefault<TunnelEntrance>();
				if (tunnel == null)
					return false;

				// HACK: The engine does not support HiddenUnderFog combined with buildings that use the "_" footprint
				// We therefore have to use AlwaysVisible and then force-disable interacting with the entrance under shroud
				var buildingInfo = target.Info.TraitInfoOrDefault<BuildingInfo>();
				if (buildingInfo != null)
				{
					var footprint = buildingInfo.PathableTiles(target.Location);
					if (footprint.All(c => self.World.ShroudObscures(c)))
						return false;
				}

				if (!tunnel.Exit.HasValue)
				{
					cursor = enterBlockedCursor;
					return false;
				}

				cursor = useEnterCursor(target) ? enterCursor : enterBlockedCursor;
				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return CanTargetActor(self, target.Actor, modifiers, ref cursor);
			}
		}
	}
}
