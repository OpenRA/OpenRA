#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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
		public bool WastedAfterwards = true;
		public bool Sabotage = false;
		public object Create(ActorInitializer init) { return new Captures(init.self, this); }
	}

	class Captures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CapturesInfo Info;
		readonly Actor self;

		public Captures(Actor self, CapturesInfo info)
		{
			this.self = self;
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new CaptureOrderTargeter(Info.CaptureTypes, target => CanCapture(target));
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
			return (order.OrderString == "CaptureActor"
					&& CanCapture(order.TargetActor)) ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CaptureActor")
			{
				if (!CanCapture(order.TargetActor)) return;

				self.SetTargetLine(Target.FromOrder(order), Color.Red);

				self.CancelActivity();
				self.QueueActivity(new CaptureActor(order.TargetActor));
			}
		}

		bool CanCapture(Actor target)
		{
			var c = target.TraitOrDefault<Capturable>();
			return c != null && ( !c.CaptureInProgress || c.Captor.Owner.Stances[self.Owner] != Stance.Ally );
		}
	}

	class CaptureOrderTargeter : UnitTraitOrderTargeter<Capturable>
	{
		readonly string[] captureTypes;
		readonly Func<Actor, bool> useEnterCursor;

		public CaptureOrderTargeter(string[] captureTypes, Func<Actor, bool> useEnterCursor)
			: base( "CaptureActor", 6, "enter", true, true)
		{
			this.captureTypes = captureTypes;
			this.useEnterCursor = useEnterCursor;
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

			var Info = self.Info.Traits.Get<CapturesInfo>();

			if (captureTypes.Contains(ci.Type))
			{
				cursor = (Info.WastedAfterwards) ? (useEnterCursor(target) ? "enter" : "enter-blocked") : "attack";
				return true;
			}

			return false;
		}
	}
}
