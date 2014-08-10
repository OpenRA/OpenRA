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
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class IronCurtainPowerInfo : SupportPowerInfo
	{
		[Desc("Seconds")]
		public readonly int Duration = 10;
		[Desc("Cells")]
		public readonly int Range = 1;
		public readonly string IronCurtainSound = "ironcur9.aud";

		public override object Create(ActorInitializer init) { return new IronCurtainPower(init, this); }
	}

	class IronCurtainPower : SupportPower
	{
		IronCurtainPowerInfo info;

		public IronCurtainPower(ActorInitializer init, IronCurtainPowerInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public override IOrderGenerator OrderGenerator(string order)
		{
			Sound.PlayToPlayer(Self.Owner, info.SelectTargetSound);
			return new SelectTarget(Self.World, order, this);
		}

		public override void Activate(Order order)
		{
			base.Activate(order);

			Self.Trait<RenderBuilding>().PlayCustomAnim(Self, "active");

			Sound.Play(info.IronCurtainSound, Self.World.Map.CenterOfCell(order.TargetLocation));

			foreach (var target in UnitsInRange(order.TargetLocation)
				.Where(a => a.Owner.Stances[Self.Owner] == Stance.Ally))
				target.Trait<IronCurtainable>().Activate(target, ((IronCurtainPowerInfo)info).Duration * 25);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = ((IronCurtainPowerInfo)info).Range;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(Self.World.ActorMap.GetUnitsAt(t));

			return units.Distinct().Where(a => a.HasTrait<IronCurtainable>());
		}

		class SelectTarget : IOrderGenerator
		{
			readonly IronCurtainPower sp;
			readonly int range;
			readonly Sprite tile;
			readonly string order;

			public SelectTarget(World world, string order, IronCurtainPower sp)
			{
				this.order = order;
				this.sp = sp;
				this.range = ((IronCurtainPowerInfo)sp.info).Range;
				tile = world.Map.SequenceProvider.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && sp.UnitsInRange(xy).Any())
					yield return new Order(order, sp.Self, false) { TargetLocation = xy, SuppressVisualFeedback = true };
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (sp.Disabled)
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = sp.UnitsInRange(xy).Where(a => a.Owner.Stances[sp.Self.Owner] == Stance.Ally);
				foreach (var unit in targetUnits)
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
				return sp.UnitsInRange(xy).Any(a => a.Owner.Stances[sp.Self.Owner] == Stance.Ally) ? "ability" : "move-blocked";
			}
		}
	}
}
