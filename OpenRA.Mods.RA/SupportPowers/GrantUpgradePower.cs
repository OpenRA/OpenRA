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
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GrantUpgradePowerInfo : SupportPowerInfo
	{
		[Desc("The upgrades to apply.")]
		public readonly string[] Upgrades = { };

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		[Desc("Cells")]
		public readonly int Range = 1;
		public readonly string GrantUpgradeSound = "ironcur9.aud";

		public override object Create(ActorInitializer init) { return new GrantUpgradePower(init.self, this); }
	}

	class GrantUpgradePower : SupportPower
	{
		GrantUpgradePowerInfo info;

		public GrantUpgradePower(Actor self, GrantUpgradePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override IOrderGenerator OrderGenerator(OrderCode order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.Trait<RenderBuilding>().PlayCustomAnim(self, "active");

			Sound.Play(info.GrantUpgradeSound, self.World.Map.CenterOfCell(order.TargetLocation));

			foreach (var a in UnitsInRange(order.TargetLocation))
			{
				var um = a.TraitOrDefault<UpgradeManager>();
				if (um == null)
					continue;

				foreach (var u in info.Upgrades)
				{
					if (!um.AcceptsUpgrade(a, u))
						continue;

					if (info.Duration > 0)
						um.GrantTimedUpgrade(a, u, info.Duration);
					else
						um.GrantUpgrade(a, u, this);
				}
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = info.Range;
			var tiles = self.World.Map.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(self.World.ActorMap.GetUnitsAt(t));

			return units.Distinct().Where(a =>
			{
				if (!a.Owner.IsAlliedWith(self.Owner))
					return false;
	
				var um = a.TraitOrDefault<UpgradeManager>();
				return um != null && info.Upgrades.Any(u => um.AcceptsUpgrade(a, u));
			});
		}

		class SelectTarget : IOrderGenerator
		{
			readonly GrantUpgradePower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly OrderCode order;

			public SelectTarget(World world, OrderCode order, SupportPowerManager manager, GrantUpgradePower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.range = power.info.Range;
				tile = world.Map.SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.UnitsInRange(xy).Any())
					yield return new Order(order, manager.self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order.ToString()))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				foreach (var unit in power.UnitsInRange(xy))
					yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var pal = wr.Palette("terrain");

				foreach (var t in world.Map.FindTilesInCircle(xy, range))
					yield return new SpriteRenderable(tile, wr.world.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, true);
			}

			public string GetCursor(World world, CPos xy, MouseInput mi)
			{
				return power.UnitsInRange(xy).Any() ? "ability" : "move-blocked";
			}
		}
	}
}
