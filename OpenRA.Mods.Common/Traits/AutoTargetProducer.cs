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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The actor can produce units with modified AutoTarget stance.")]
	public class AutoTargetProducerInfo : ConditionalTraitInfo, Requires<ProductionInfo>
	{
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;
		public override object Create(ActorInitializer init) { return new AutoTargetProducer(this); }
	}

	public class AutoTargetProducer : ConditionalTrait<AutoTargetProducerInfo>, IResolveOrder, IActorStanceSelector
	{
		public UnitStance Stance { get; set; }

		bool IActorStanceSelector.Enabled {	get	{ return !IsTraitDisabled; } }

		UnitStance IActorStanceSelector.Stance { get { return PredictedStance; } }

		bool IActorStanceSelector.CanBeDisabled { get {	return true; } }

		public AutoTargetProducer(AutoTargetProducerInfo info) : base(info)
		{
			Stance = UnitStance.NoStance;
			PredictedStance = UnitStance.NoStance;
		}

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetBuildingProductionStance")
				Stance = (UnitStance)order.ExtraData;
		}

		void IActorStanceSelector.SetStance(Actor self, UnitStance stance)
		{
			if (IsTraitDisabled)
				return;
			PredictedStance = stance;
			self.World.IssueOrder(new Order("SetBuildingProductionStance", self, false) { ExtraData = (uint)stance });
		}
	}
}
