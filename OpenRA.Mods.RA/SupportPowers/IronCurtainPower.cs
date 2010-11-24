#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using TUtil = OpenRA.Traits.Util;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		public readonly float Duration = 0f;
		public readonly int Range = 2; // Range in cells

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init.self, this); }
	}

	class IronCurtainPower : SupportPower, IResolveOrder
	{
		public IronCurtainPower(Actor self, IronCurtainPowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "ironchg1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "ironrdy1.aud"); }
		protected override void OnActivate()
		{
			Self.World.OrderGenerator = new SelectTarget(this);
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!IsReady) return;

			if (order.OrderString == "IronCurtain")
			{
				var curtain = self.World.Queries
					.OwnedBy[self.Owner]
					.WithTrait<IronCurtain>()
					.Select(x => x.Actor).FirstOrDefault();
				
				if (curtain != null)
					curtain.Trait<RenderBuilding>().PlayCustomAnim(curtain, "active");

				Sound.Play("ironcur9.aud", Game.CellSize * order.TargetLocation);
				foreach (var target in UnitsInRange(order.TargetLocation))
					target.Trait<IronCurtainable>().Activate(target, (int)((Info as IronCurtainPowerInfo).Duration * 25 * 60));

				FinishActivate();
			}

		}

		public IEnumerable<Actor> UnitsInRange(int2 xy)
		{
			int range = (Info as IronCurtainPowerInfo).Range;
			var uim = Self.World.WorldActor.Trait<UnitInfluence>();
			var tiles = Self.World.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(uim.GetUnitsAt(t));
			
			return units.Distinct().Where(a => a.HasTrait<IronCurtainable>());
		}
		
		class SelectTarget : IOrderGenerator
		{
			IronCurtainPower power;
			int range;
			Sprite tile;
			
			public SelectTarget(IronCurtainPower power)
			{
				this.power = power;
				this.range = (power.Info as IronCurtainPowerInfo).Range;
				tile = UiOverlay.SynthesizeTile(0x04);
			}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					world.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
				{
					var targetUnits = power.UnitsInRange(xy);

					if( targetUnits.Any() )
					{
						world.CancelInputMode();
						yield return new Order("IronCurtain", world.LocalPlayer.PlayerActor, false) { TargetLocation = xy };
					}
				}
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<IronCurtain>()
					.Any();

				if (!hasStructure)
					world.CancelInputMode();
			}

			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				var targetUnits = power.UnitsInRange(xy);
				foreach (var unit in power.UnitsInRange(xy))
					wr.DrawSelectionBox(unit, Color.Red);
			}

			public void RenderBeforeWorld(WorldRenderer wr, World world)
			{
				var xy = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
				var tiles = world.FindTilesInCircle(xy, range);
				foreach (var t in tiles)
					tile.DrawAt( wr, Game.CellSize * t, "terrain" );
			}

			public string GetCursor(World world, int2 xy, MouseInput mi)
			{
				return power.UnitsInRange(xy).Any()	? "ability" : "move-blocked";
			}
		}
	}

	// tag trait for the building
	class IronCurtainInfo : TraitInfo<IronCurtain> { }
	class IronCurtain { }
}
