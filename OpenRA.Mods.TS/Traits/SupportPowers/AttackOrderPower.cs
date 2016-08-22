#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	class AttackOrderPowerInfo : SupportPowerInfo, Requires<AttackBaseInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackOrderPower(init.Self, this); }
	}

	class AttackOrderPower : SupportPower, INotifyCreated, INotifyBurstComplete
	{
		readonly AttackOrderPowerInfo info;
		AttackBase attack;

		public AttackOrderPower(Actor self, AttackOrderPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectAttackPowerTarget(self, order, manager, info.Cursor, MouseButton.Left, attack);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			attack.AttackTarget(Target.FromCell(self.World, order.TargetLocation), false, false, true);
		}

		void INotifyCreated.Created(Actor self)
		{
			attack = self.Trait<AttackBase>();
		}

		void INotifyBurstComplete.FiredBurst(Actor self, Target target, Armament a)
		{
			self.World.IssueOrder(new Order("Stop", self, false));
		}
	}

	public class SelectAttackPowerTarget : IOrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly SupportPowerInstance instance;
		readonly string order;
		readonly string cursor;
		readonly string cursorBlocked;
		readonly MouseButton expectedButton;
		readonly AttackBase attack;

		public SelectAttackPowerTarget(Actor self, string order, SupportPowerManager manager, string cursor, MouseButton button, AttackBase attack)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			instance = manager.GetPowersForActor(self).FirstOrDefault();
			this.manager = manager;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
			this.attack = attack;
			cursorBlocked = cursor + "-blocked";
		}

		Actor GetFiringActor(World world, CPos cell)
		{
			var pos = world.Map.CenterOfCell(cell);
			var range = attack.GetMaximumRange().LengthSquared;

			return instance.Instances.Where(i => !i.Self.IsDisabled()).MinByOrDefault(a => (a.Self.CenterPosition - pos).HorizontalLengthSquared).Self;
		}

		bool IsValidTarget(World world, CPos cell)
		{
			var pos = world.Map.CenterOfCell(cell);
			var range = attack.GetMaximumRange().LengthSquared;

			return world.Map.Contains(cell) && instance.Instances.Any(a => !a.Self.IsDisabled() && (a.Self.CenterPosition - pos).HorizontalLengthSquared < range);
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && IsValidTarget(world, cell))
				yield return new Order(order, manager.Self, false)
				{
					TargetActor = GetFiringActor(world, cell),
					TargetLocation = cell,
					SuppressVisualFeedback = true
				};
		}

		void IOrderGenerator.Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			foreach (var a in instance.Instances.Where(i => !i.Self.IsDisabled()))
			{
				yield return new RangeCircleRenderable(
					a.Self.CenterPosition,
					attack.GetMinimumRange(),
					0,
					Color.Red,
					Color.FromArgb(96, Color.Black));

				yield return new RangeCircleRenderable(
					a.Self.CenterPosition,
					attack.GetMaximumRange(),
					0,
					Color.Red,
					Color.FromArgb(96, Color.Black));
			}
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return IsValidTarget(world, cell) ? cursor : cursorBlocked;
		}
	}
}
