#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CapturesInfo : ITraitInfo
	{
		public string[] CaptureTypes = {"building"};
		public object Create(ActorInitializer init) { return new Captures(this); }
	}

	class Captures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CapturesInfo Info;
		public Captures(CapturesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new CaptureOrderTargeter(Info.CaptureTypes);
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "CaptureActor" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "CaptureActor") ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CaptureActor")
			{
				self.SetTargetLine(Target.FromOrder(order), Color.Red);

				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor));
				self.QueueActivity(new CaptureActor(order.TargetActor));
			}
		}
	}

	class CaptureOrderTargeter : UnitTraitOrderTargeter<Capturable>
	{
		readonly string[] captureTypes;
		public CaptureOrderTargeter(string[] captureTypes)
			: base( "CaptureActor", 6, "enter", true, true )
		{
			this.captureTypes = captureTypes;
		}

		public override bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
		{
			if( !base.CanTargetActor( self, target, forceAttack, forceQueued, ref cursor ) ) return false;

			var ci = target.Info.Traits.Get<CapturableInfo>();
			var playerRelationship = self.Owner.Stances[ target.Owner ];

			if( playerRelationship == Stance.Ally && !ci.AllowAllies ) return false;
			if( playerRelationship == Stance.Enemy && !ci.AllowEnemies ) return false;
			if( playerRelationship == Stance.Neutral && !ci.AllowNeutral ) return false;

			IsQueued = forceQueued;
			if (captureTypes.Contains(ci.Type))
			{
				cursor = "enter";
				return true;
			}

			return false;
		}
	}
}
