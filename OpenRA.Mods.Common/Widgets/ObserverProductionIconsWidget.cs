#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverProductionIconsWidget : Widget
	{
		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";
		public readonly string TooltipContainer;
		public Func<Player> GetPlayer;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly int timestep;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 1;

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;

		Dictionary<ProductionQueue, Animation> clocks;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly List<ProductionIcon> productionIcons = new List<ProductionIcon>();
		readonly List<Rectangle> productionIconsBounds = new List<Rectangle>();

		float2 iconSize;
		int lastIconIdx;

		[ObjectCreator.UseCtor]
		public ObserverProductionIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<ProductionQueue, Animation>();
			timestep = world.IsReplay ? world.WorldActor.Trait<MapOptions>().GameSpeed.Timestep : world.Timestep;
			GetTooltipIcon = () => TooltipIcon;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			iconSize = new float2(IconWidth, IconHeight);
		}

		protected ObserverProductionIconsWidget(ObserverProductionIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			world = other.world;
			worldRenderer = other.worldRenderer;
			timestep = other.timestep;
			clocks = other.clocks;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;
			iconSize = new float2(IconWidth, IconHeight);

			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;

			TooltipIcon = other.TooltipIcon;
			GetTooltipIcon = () => TooltipIcon;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Draw()
		{
			productionIcons.Clear();
			productionIconsBounds.Clear();

			var player = GetPlayer();
			if (player == null)
				return;

			var queues = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player)
				.Select((a, i) => new { a.Trait, i });

			foreach (var queue in queues)
				if (!clocks.ContainsKey(queue.Trait))
					clocks.Add(queue.Trait, new Animation(world, ClockAnimation));

			var currentItemsByItem = queues
					.Select(a => a.Trait.CurrentItem())
					.Where(pi => pi != null)
					.GroupBy(pr => pr.Item)
					.OrderBy(g => g.First().Queue.Info.DisplayOrder)
					.ThenBy(g => g.First().BuildPaletteOrder)
					.ToList();

			Bounds.Width = currentItemsByItem.Count * (IconWidth + IconSpacing);

			var queueCol = 0;
			foreach (var currentItems in currentItemsByItem)
			{
				var current = currentItems.OrderBy(pi => pi.Done ? 0 : (pi.Paused ? 2 : 1)).ThenBy(q => q.RemainingTimeActual).First();
				var queue = current.Queue;

				var faction = queue.Actor.Owner.Faction.InternalName;
				var actor = queue.AllItems().FirstOrDefault(a => a.Name == current.Item);
				if (actor == null)
					continue;

				var rsi = actor.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(world, rsi.GetImage(actor, world.Map.Rules.Sequences, faction));
				var bi = actor.TraitInfo<BuildableInfo>();

				icon.Play(bi.Icon);
				var topLeftOffset = new float2(queueCol * (IconWidth + IconSpacing), 0);

				var iconTopLeft = RenderOrigin + topLeftOffset;
				var centerPosition = iconTopLeft;

				WidgetUtils.DrawSHPCentered(icon.Image, centerPosition + 0.5f * iconSize, worldRenderer.Palette(bi.IconPalette), 0.5f);

				productionIcons.Add(new ProductionIcon { Actor = actor, ProductionQueue = current.Queue });
				productionIconsBounds.Add(new Rectangle((int)iconTopLeft.X, (int)iconTopLeft.Y, (int)iconSize.X, (int)iconSize.Y));

				var pio = queue.Actor.Owner.PlayerActor.TraitsImplementing<IProductionIconOverlay>()
					.FirstOrDefault(p => p.IsOverlayActive(actor));

				if (pio != null)
					WidgetUtils.DrawSHPCentered(pio.Sprite, centerPosition + 0.5f * iconSize + pio.Offset(iconSize),
						worldRenderer.Palette(pio.Palette), 0.5f);

				var clock = clocks[queue];
				clock.PlayFetchIndex(ClockSequence, () => current.TotalTime == 0 ? 0 :
					(current.TotalTime - current.RemainingTime) * (clock.CurrentSequence.Length - 1) / current.TotalTime);

				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, centerPosition + 0.5f * iconSize, worldRenderer.Palette(ClockPalette), 0.5f);

				var tiny = Game.Renderer.Fonts["Tiny"];
				var text = GetOverlayForItem(current, timestep);
				tiny.DrawTextWithContrast(text,
					centerPosition + new float2(16, 12) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);

				if (currentItems.Count() > 1)
				{
					var bold = Game.Renderer.Fonts["Small"];
					text = currentItems.Count().ToString();
					bold.DrawTextWithContrast(text, centerPosition + new float2(16, 0) - new float2(bold.Measure(text).X / 2, 0),
						Color.White, Color.Black, 1);
				}

				queueCol++;
			}
		}

		static string GetOverlayForItem(ProductionItem item, int timestep)
		{
			if (item.Paused)
				return "ON HOLD";

			if (item.Done)
				return "READY";

			return WidgetUtils.FormatTime(item.Queue.RemainingTimeActual(item), timestep);
		}

		public override Widget Clone()
		{
			return new ObserverProductionIconsWidget(this);
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			for (var i = 0; i < productionIconsBounds.Count; i++)
			{
				if (!productionIconsBounds[i].Contains(Viewport.LastMousePos))
					continue;

				TooltipIcon = productionIcons[i];
				break;
			}

			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "player", GetPlayer() }, { "getTooltipIcon", GetTooltipIcon } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Tick()
		{
			if (lastIconIdx >= productionIconsBounds.Count)
			{
				TooltipIcon = null;
				return;
			}

			if (TooltipIcon != null && productionIconsBounds[lastIconIdx].Contains(Viewport.LastMousePos))
				return;

			for (var i = 0; i < productionIconsBounds.Count; i++)
			{
				if (!productionIconsBounds[i].Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipIcon = productionIcons[i];
				return;
			}

			TooltipIcon = null;
		}
	}
}
