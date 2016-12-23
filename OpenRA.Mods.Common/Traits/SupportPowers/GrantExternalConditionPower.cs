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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class GrantExternalConditionPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[Desc("Cells - affects whole cells only")]
		public readonly int Range = 1;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[SequenceReference, Desc("Sequence to play for granting actor when activated.",
			"This requires the actor to have the WithSpriteBody trait or one of its derivatives.")]
		public readonly string Sequence = "active";

		public override object Create(ActorInitializer init) { return new GrantExternalConditionPower(init.Self, this); }
	}

	class GrantExternalConditionPower : SupportPower
	{
		readonly GrantExternalConditionPowerInfo info;

		public GrantExternalConditionPower(Actor self, GrantExternalConditionPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(SoundType.World, manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectUpgradeTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.Sequence))
				wsb.PlayCustomAnimation(self, info.Sequence, () => wsb.CancelCustomAnimation(self));

			Game.Sound.Play(SoundType.World, info.OnFireSound, self.World.Map.CenterOfCell(order.TargetLocation));

			foreach (var a in UnitsInRange(order.TargetLocation))
			{
				var um = a.TraitOrDefault<UpgradeManager>();

				// Condition token is ignored because we never revoke this condition.
				if (um != null)
					um.GrantCondition(a, info.Condition, true, info.Duration);
			}
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var range = info.Range;
			var tiles = Self.World.Map.FindTilesInCircle(xy, range);
			var units = new List<Actor>();
			foreach (var t in tiles)
				units.AddRange(Self.World.ActorMap.GetActorsAt(t));

			return units.Distinct().Where(a =>
			{
				if (!a.Owner.IsAlliedWith(Self.Owner))
					return false;

				var um = a.TraitOrDefault<UpgradeManager>();
				return um != null && um.AcceptsExternalCondition(a, info.Condition);
			});
		}

		class SelectUpgradeTarget : IOrderGenerator
		{
			readonly GrantExternalConditionPower power;
			readonly int range;
			readonly Sprite tile;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectUpgradeTarget(World world, string order, SupportPowerManager manager, GrantExternalConditionPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
				range = power.info.Range;
				tile = world.Map.Rules.Sequences.GetSequence("overlay", "target-select").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.UnitsInRange(cell).Any())
					yield return new Order(order, manager.Self, false) { TargetLocation = cell, SuppressVisualFeedback = true };
			}

			public void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				foreach (var unit in power.UnitsInRange(xy))
					yield return new SelectionBoxRenderable(unit, Color.Red);
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var pal = wr.Palette(TileSet.TerrainPaletteInternalName);

				foreach (var t in world.Map.FindTilesInCircle(xy, range))
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, true);
			}

			public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.UnitsInRange(cell).Any() ? "ability" : "move-blocked";
			}
		}
	}
}
