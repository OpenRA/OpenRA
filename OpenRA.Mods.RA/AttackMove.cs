﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackMoveInfo : ITraitInfo
	{
		public readonly bool JustMove = false;

		public object Create(ActorInitializer init) { return new AttackMove(init.self, this); }
	}

	class AttackMove : IResolveOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public CPos _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : CPos.Zero; } }
		public CPos? TargetLocation = null;

		readonly Mobile mobile;
		readonly AttackMoveInfo Info;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			Info = info;
			mobile = self.Trait<Mobile>();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
				return "AttackMove";
			return null;
		}

		void Activate(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new AttackMoveActivity(self, mobile.MoveTo(TargetLocation.Value, 1)));
			self.SetTargetLine(Target.FromCell(TargetLocation.Value), Color.Red);
		}

		public void TickIdle(Actor self)
		{
			if (TargetLocation.HasValue)
				Activate(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			TargetLocation = null;

			if (order.OrderString == "AttackMove")
			{
				if (Info.JustMove)
					mobile.ResolveOrder(self, new Order("Move", order));
				else
				{
					TargetLocation = mobile.NearestMoveableCell(order.TargetLocation);
					Activate(self);
				}
			}
		}

		public class AttackMoveActivity : Activity
		{
			Activity inner;
			int scanTicks;
			AutoTarget autoTarget;

			const int ScanInterval = 7;

			public AttackMoveActivity( Actor self, Activity inner )
			{
				this.inner = inner;
				this.autoTarget = self.TraitOrDefault<AutoTarget>();
			}

			public override Activity Tick( Actor self )
			{
				if (autoTarget != null && --scanTicks <= 0)
				{
					autoTarget.ScanAndAttack(self);
					scanTicks = ScanInterval;
				}

				if( inner == null )
					return NextActivity;

				inner = Util.RunActivity( self, inner );

				return this;
			}

			public override void Cancel( Actor self )
			{
				if( inner != null )
					inner.Cancel( self );

				base.Cancel( self );
			}
		}
	}
}
