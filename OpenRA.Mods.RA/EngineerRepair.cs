#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class EngineerRepairInfo : TraitInfo<EngineerRepair> {}

	class EngineerRepair : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new EngineerRepairOrderTargeter(); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "EngineerRepair" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "EngineerRepair"
			        && order.TargetActor.GetDamageState() > DamageState.Undamaged) ? "Attack" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EngineerRepair"
			    && order.TargetActor.GetDamageState() > DamageState.Undamaged)
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Yellow);
					});
				
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, order.TargetActor));
				self.QueueActivity(new RepairBuilding(order.TargetActor));
			}
		}

		class EngineerRepairOrderTargeter : UnitTraitOrderTargeter<Building>
		{
			public EngineerRepairOrderTargeter()
				: base( "EngineerRepair", 6, "goldwrench", false, true )
			{
			}

			public override bool CanTargetUnit( Actor self, Actor target, bool forceAttack, bool forceMove, ref string cursor )
			{
				if( !base.CanTargetUnit( self, target, forceAttack, forceMove, ref cursor ) ) return false;

				if( target.GetDamageState() == DamageState.Undamaged )
					cursor = "goldwrench-blocked";
				return true;
			}
		}
	}
}
