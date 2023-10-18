#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	sealed class AttackOrderPowerInfo : SupportPowerInfo, Requires<AttackBaseInfo>
	{
		[Desc("Range circle color.")]
		public readonly Color CircleColor = Color.Red;

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Range circle border color.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		[Desc("Condition set to the unit that will execute the attack while the support power is in targeting mode.")]
		[GrantedConditionReference]
		public readonly string SupportPowerTargetingCondition = "support-targeting";

		[Desc("Condition set to the unit is executing the attack while the support power attack is in progress.")]
		[GrantedConditionReference]
		public readonly string SupportPowerAttackingCondition = "support-attacking";

		public override object Create(ActorInitializer init) { return new AttackOrderPower(init.Self, this); }
	}

	sealed class AttackOrderPower : SupportPower, INotifyCreated, INotifyBurstComplete, INotifyBecomingIdle
	{
		readonly AttackOrderPowerInfo info;
		AttackBase attack;
		int attackingToken = 0;

		public AttackOrderPower(Actor self, AttackOrderPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectAttackPowerTarget(self, order, manager, info.Cursor, MouseButton.Left, attack);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			attackingToken = self.GrantCondition(info.SupportPowerAttackingCondition);
			attack.AttackTarget(order.Target, AttackSource.Default, false, false, true);
		}

		protected override void Created(Actor self)
		{
			attack = self.Trait<AttackBase>();

			base.Created(self);
		}

		void INotifyBurstComplete.FiredBurst(Actor self, in Target target, Armament a)
		{
			self.World.IssueOrder(new Order("Stop", self, false));
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			self.RevokeCondition(attackingToken);
		}
	}

	public class SelectAttackPowerTarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly SupportPowerInstance instance;
		readonly string order;
		readonly string cursor;
		readonly string cursorBlocked;
		readonly MouseButton expectedButton;
		readonly AttackBase attack;
		readonly Actor self;
		readonly int targetingToken = 0;
		bool isFiring;

		public SelectAttackPowerTarget(Actor self, string order, SupportPowerManager manager, string cursor, MouseButton button, AttackBase attack)
		{
			this.self = self;

			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			instance = manager.GetPowersForActor(self).FirstOrDefault();
			targetingToken = self.GrantCondition((instance.Info as AttackOrderPowerInfo).SupportPowerTargetingCondition);

			this.manager = manager;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
			this.attack = attack;
			cursorBlocked = cursor + "-blocked";
		}

		bool IsValidTarget(World world, CPos cell)
		{
			var pos = world.Map.CenterOfCell(cell);
			var range = attack.GetMaximumRange().LengthSquared;

			return world.Map.Contains(cell) && instance.Instances.Any(a => !a.IsTraitPaused && (a.Self.CenterPosition - pos).HorizontalLengthSquared < range);
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// CancelInputMode will call Deactivate before we could validate the target, so we need to delay clearing the targeting flag
			isFiring = true;
			world.CancelInputMode();
			if (mi.Button == expectedButton && IsValidTarget(world, cell))
			{
				self.RevokeCondition(targetingToken);
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false)
				{
					SuppressVisualFeedback = true
				};
			}
			else
			{
				self.RevokeCondition(targetingToken);
			}
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
				world.CancelInputMode();
		}

		protected override void Deactivate()
		{
			if (!isFiring)
			{
				self.RevokeCondition(targetingToken);
			}
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var info = instance.Info as AttackOrderPowerInfo;
			foreach (var a in instance.Instances.Where(i => !i.IsTraitPaused))
			{
				yield return new RangeCircleAnnotationRenderable(
					a.Self.CenterPosition,
					attack.GetMinimumRange(),
					0,
					info.CircleColor,
					info.CircleWidth,
					info.CircleBorderColor,
					info.CircleBorderWidth);

				yield return new RangeCircleAnnotationRenderable(
					a.Self.CenterPosition,
					attack.GetMaximumRange(),
					0,
					info.CircleColor,
					info.CircleWidth,
					info.CircleBorderColor,
					info.CircleBorderWidth);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return IsValidTarget(world, cell) ? cursor : cursorBlocked;
		}
	}
}
