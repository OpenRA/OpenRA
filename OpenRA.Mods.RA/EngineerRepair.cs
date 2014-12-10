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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Can instantly repair other actors, but gets consumed afterwards.")]
	class EngineerRepairInfo : ITraitInfo
	{
		[Desc("Target's owner's stance with respect to engineer.")]
		public readonly Stance TargetPlayers = Stance.Ally | Stance.Player;

		public object Create(ActorInitializer init) { return new EngineerRepair(this); }
	}

	class EngineerRepair : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly EngineerRepairInfo info;

		public EngineerRepair(EngineerRepairInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new EngineerRepairOrderTargeter(info); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "EngineerRepair")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				return frozen.DamageState > DamageState.Undamaged;
			}

			return order.TargetActor.GetDamageState() > DamageState.Undamaged;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EngineerRepair" && IsValidOrder(self, order)
				? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EngineerRepair" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new RepairBuilding(self, target.Actor));
		}

		class EngineerRepairOrderTargeter : UnitOrderTargeter
		{
			readonly EngineerRepairInfo info;

			public EngineerRepairOrderTargeter(EngineerRepairInfo info)
				: base("EngineerRepair", 6, "goldwrench", info.TargetPlayers)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.HasTrait<EngineerRepairable>())
					return false;

				if (self.Owner.Stances[target.Owner].Intersects(info.TargetPlayers))
					return false;

				if (target.GetDamageState() == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.Info.Traits.Contains<EngineerRepairable>())
					return false;

				if (!self.Owner.Stances[target.Owner].Intersects(info.TargetPlayers))
					return false;

				if (target.DamageState == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}
		}
	}

	[Desc("Eligible for instant repair.")]
	class EngineerRepairableInfo : TraitInfo<EngineerRepairable> { }

	class EngineerRepairable { }
}
