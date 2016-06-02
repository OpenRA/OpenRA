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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverProductionIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		Dictionary<ProductionQueue, Animation> clocks;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 8;

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";

		[ObjectCreator.UseCtor]
		public ObserverProductionIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<ProductionQueue, Animation>();
		}

		protected ObserverProductionIconsWidget(ObserverProductionIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			world = other.world;
			worldRenderer = other.worldRenderer;
			clocks = other.clocks;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;

			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;
		}

		public override void Draw()
		{
			var player = GetPlayer();
			if (player == null)
				return;

			var queues = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player)
				.Select((a, i) => new { a.Trait, i });

			foreach (var queue in queues)
				if (!clocks.ContainsKey(queue.Trait))
					clocks.Add(queue.Trait, new Animation(world, ClockAnimation));

			var iconSize = new float2(IconWidth, IconHeight);
			foreach (var queue in queues)
			{
				var current = queue.Trait.CurrentItem();
				if (current == null)
					continue;

				var faction = queue.Trait.Actor.Owner.Faction.InternalName;
				var actor = queue.Trait.AllItems().FirstOrDefault(a => a.Name == current.Item);
				if (actor == null)
					continue;

				var rsi = actor.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(world, rsi.GetImage(actor, world.Map.Rules.Sequences, faction));
				icon.Play(actor.TraitInfo<TooltipInfo>().Icon);
				var bi = actor.TraitInfo<BuildableInfo>();
				var location = new float2(RenderBounds.Location) + new float2(queue.i * (IconWidth + IconSpacing), 0);
				WidgetUtils.DrawSHPCentered(icon.Image, location + 0.5f * iconSize, worldRenderer.Palette(bi.IconPalette), 0.5f);

				var pio = queue.Trait.Actor.Owner.PlayerActor.TraitsImplementing<IProductionIconOverlay>()
					.FirstOrDefault(p => p.IsOverlayActive(actor));
				if (pio != null)
					WidgetUtils.DrawSHPCentered(pio.Sprite, location + 0.5f * iconSize + pio.Offset(0.5f * iconSize),
						worldRenderer.Palette(pio.Palette), 0.5f);

				var clock = clocks[queue.Trait];
				clock.PlayFetchIndex(ClockSequence,
					() => current.TotalTime == 0 ? 0 : ((current.TotalTime - current.RemainingTime)
					* (clock.CurrentSequence.Length - 1) / current.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, location + 0.5f * iconSize, worldRenderer.Palette(ClockPalette), 0.5f);

				var tiny = Game.Renderer.Fonts["Tiny"];
				var text = GetOverlayForItem(current, world.Timestep);
				tiny.DrawTextWithContrast(text,
					location + new float2(16, 16) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);
			}
		}

		static string GetOverlayForItem(ProductionItem item, int timestep)
		{
			if (item.Paused) return "ON HOLD";
			if (item.Done) return "READY";
			return WidgetUtils.FormatTime(item.RemainingTimeActual, timestep);
		}

		public override Widget Clone()
		{
			return new ObserverProductionIconsWidget(this);
		}
	}
}
