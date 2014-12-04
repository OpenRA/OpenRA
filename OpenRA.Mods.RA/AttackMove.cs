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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	class AttackMoveInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new AttackMove(init.self, this); }
	}

	class AttackMove : IResolveOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public CPos _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : CPos.Zero; } }
		public CPos? TargetLocation = null;

		readonly IMove move;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
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
			self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(TargetLocation.Value, 1)));
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
				TargetLocation = move.NearestMoveableCell(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(self.World, TargetLocation.Value), Color.Red);
				Activate(self);
			}
		}

		public class AttackMoveActivity : Activity
		{
			const int ScanInterval = 7;

			int scanTicks;
			bool hasMoved;
			Activity inner;
			AutoTarget autoTarget;

			public AttackMoveActivity(Actor self, Activity inner)
			{
				this.inner = inner;
				autoTarget = self.TraitOrDefault<AutoTarget>();
				hasMoved = false;
			}

			public override Activity Tick(Actor self)
			{
				if (autoTarget != null)
				{
					// If the actor hasn't moved since the activity was issued
					if (!hasMoved)
						autoTarget.ResetScanTimer();

					if (--scanTicks <= 0)
					{
						var attackActivity = autoTarget.ScanAndAttack(self);
						if (attackActivity != null)
						{
							if (!hasMoved)
								return attackActivity;

							self.QueueActivity(false, attackActivity);
						}
						scanTicks = ScanInterval;
					}
				}

				hasMoved = true;

				if (inner == null)
					return NextActivity;

				inner = Util.RunActivity(self, inner);

				return this;
			}

			public override void Cancel(Actor self)
			{
				if (inner != null)
					inner.Cancel(self);

				base.Cancel(self);
			}

			public override IEnumerable<Target> GetTargets(Actor self)
			{
				if (inner != null)
					return inner.GetTargets(self);

				return Target.None;
			}
		}
	}
}
