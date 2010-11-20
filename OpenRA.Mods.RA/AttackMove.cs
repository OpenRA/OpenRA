#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackMoveInfo : TraitInfo<AttackMove>
	{
		public readonly bool JustMove = false;
	}

	class AttackMove : IResolveOrder, IOrderVoice, ITick
	{
		[Sync] public int2 TargetLocation = int2.Zero; 

		[Sync] public bool AttackMoving = false;

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
				return "AttackMove";

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
			{
				
				self.CancelActivity();
				//if we are just moving, we don't turn on attackmove and this becomes a regular move order
				if (self.Info.Traits.Get<AttackMoveInfo>().JustMove)
				{
					self.QueueActivity(self.Trait<Mobile>().MoveTo(order.TargetLocation, 1));
					AttackMoving = false;
				}
				else
				{
					self.QueueActivity(new AttackMoveActivity(order.TargetLocation));
					AttackMoving = true;
					TargetLocation = order.TargetLocation;
				}

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						if (order.TargetActor != null)
							w.Add(new FlashTarget(order.TargetActor));

						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							if (order.TargetActor != null) line.SetTarget(self, Target.FromOrder(order), Color.Red);
							else line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});
			}
			else
			{
				AttackMoving = false;
			}
		}

		class AttackMoveActivity : CancelableActivity
		{
			readonly int2 target;
			IActivity inner;
			public AttackMoveActivity( int2 target ) { this.target = target; }

			public override IActivity Tick( Actor self )
			{
				self.Trait<AttackBase>().ScanAndAttack(self, true);

				if( inner == null )
				{
					if( IsCanceled )
						return NextActivity;
					inner = self.Trait<Mobile>().MoveTo( target, 1 );
				}
				inner = Util.RunActivity( self, inner );
				return this;
			}

			protected override bool OnCancel( Actor self )
			{
				if( inner != null )
					inner.Cancel( self );
				return base.OnCancel( self );
			}
		}

		public void Tick(Actor self)
		{
			if (!self.IsInWorld) return;

			if (AttackMoving && self.IsIdle)
			{
				self.CancelActivity();
				self.QueueActivity(new AttackMoveActivity(TargetLocation));
			}

		}
	}
}
