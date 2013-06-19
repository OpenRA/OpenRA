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
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly int Duration = 10; // Seconds
		public readonly int Range = 1; // Range in cells

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }
		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(order, manager, this);
		}

		public override void Activate(Actor self, Order order)
		{
			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

			Sound.Play("ironcur9.aud", order.TargetLocation.ToPPos());

			foreach (var target in UnitsInRange(order.TargetLocation)
				.Where(a => a.Owner.Stances[self.Owner] == Stance.Ally))
				target.Trait<IronCurtainable>().Activate(target, (Info as IronCurtainPowerInfo).Duration * 25);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			int range = (Info as IronCurtainPowerInfo).Range;
			var tiles = self.World.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(self.World.ActorMap.GetUnitsAt(t));

			return units.Distinct().Where(a => a.HasTrait<IronCurtainable>());
		}

		class SelectTarget : IOrderGenerator
		{
			readonly IronCurtainPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectTarget(string order, SupportPowerManager manager, IronCurtainPower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = (power.Info as IronCurtainPowerInfo).Range;
				tile = SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.UnitsInRange(xy).Any())
					yield return new Order(order, manager.self, false) { TargetLocation = xy };
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos);
				foreach (var unit in power.UnitsInRange(xy))
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos);
				var pal = wr.Palette("terrain");
				foreach (var t in world.FindTilesInCircle(xy, range))
					tile.DrawAt(t.ToPPos().ToFloat2(), pal);
			}

			public string GetCursor(World world, CPos xy, MouseInput mi)
			{
				return power.UnitsInRange(xy).Any()	? "ability" : "move-blocked";
			}
		}
	}
}
