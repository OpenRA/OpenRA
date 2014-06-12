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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		[Desc("Seconds")]
		public readonly int Duration = 10;
		[Desc("Cells")]
		public readonly int Range = 1;
		public readonly string IronCurtainSound = "ironcur9.aud";

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower
	{
		IronCurtainPowerInfo info;

		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info)
		{
			this.info = info;
		}

		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

			Sound.Play(info.IronCurtainSound, order.TargetLocation.CenterPosition);

			foreach (var target in UnitsInRange(order.TargetLocation)
				.Where(a => a.Owner.Stances[self.Owner] == Stance.Ally))
				target.Trait<IronCurtainable>().Activate(target, ((IronCurtainPowerInfo)Info).Duration * 25);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			int range = ((IronCurtainPowerInfo)Info).Range;
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

			public SelectTarget(World world, string order, SupportPowerManager manager, IronCurtainPower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = ((IronCurtainPowerInfo)power.Info).Range;
				tile = world.Map.SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
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
				var xy = wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos();
				foreach (var unit in power.UnitsInRange(xy))
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos();
				var pal = wr.Palette("terrain");
				foreach (var t in world.FindTilesInCircle(xy, range))
					yield return new SpriteRenderable(tile, t.CenterPosition, WVec.Zero, -511, pal, 1f, true);
			}

			public string GetCursor(World world, CPos xy, MouseInput mi)
			{
				return power.UnitsInRange(xy).Any()	? "ability" : "move-blocked";
			}
		}
	}
}
