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

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 1;

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;

		readonly Dictionary<ProductionQueue, Animation> clocks;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly List<ProductionIcon> productionIcons = new List<ProductionIcon>();
		readonly List<Rectangle> productionIconsBounds = new List<Rectangle>();

		readonly float2 iconSize;
		int lastIconIdx;
		public int MinWidth = 240;
		int currentTooltipToken;

		[ObjectCreator.UseCtor]
		public ObserverProductionIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<ProductionQueue, Animation>();
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

			MinWidth = other.MinWidth;

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

			Game.Renderer.EnableAntialiasingFilter();

			var queueCol = 0;
			foreach (var currentItems in currentItemsByItem)
			{
				var queued = currentItems
					.OrderBy(pi => pi.Done ? 0 : (pi.Paused ? 2 : 1))
					.ThenBy(q => q.RemainingTimeActual)
					.ToList();

				var current = queued.First();
				var queue = current.Queue;

				var faction = queue.Actor.Owner.Faction.InternalName;
				var actor = queue.AllItems().FirstOrDefault(a => a.Name == current.Item);
				if (actor == null)
					continue;

				var rsi = actor.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(world, rsi.GetImage(actor, faction));
				var bi = actor.TraitInfo<BuildableInfo>();

				icon.Play(bi.Icon);
				var topLeftOffset = new float2(queueCol * (IconWidth + IconSpacing), 0);

				var iconTopLeft = RenderOrigin + topLeftOffset;
				var centerPosition = iconTopLeft + 0.5f * iconSize;

				var palette = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + player.InternalName : bi.IconPalette;
				WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(palette), centerPosition, 0.5f);

				var rect = new Rectangle((int)iconTopLeft.X, (int)iconTopLeft.Y, (int)iconSize.X, (int)iconSize.Y);
				productionIcons.Add(new ProductionIcon
				{
					Actor = actor,
					Pos = new float2(rect.Location),
					Queued = queued,
					ProductionQueue = current.Queue
				});

				productionIconsBounds.Add(rect);

				var pios = queue.Actor.Owner.PlayerActor.TraitsImplementing<IProductionIconOverlay>();

				foreach (var pio in pios.Where(p => p.IsOverlayActive(actor)))
					WidgetUtils.DrawSpriteCentered(pio.Sprite, worldRenderer.Palette(pio.Palette),
						centerPosition + pio.Offset(iconSize), 0.5f);

				var clock = clocks[queue];
				clock.PlayFetchIndex(ClockSequence, () => current.TotalTime == 0 ? 0 :
					(current.TotalTime - current.RemainingTime) * (clock.CurrentSequence.Length - 1) / current.TotalTime);

				clock.Tick();
				WidgetUtils.DrawSpriteCentered(clock.Image, worldRenderer.Palette(ClockPalette), centerPosition, 0.5f);

				queueCol++;
			}

			var newWidth = Math.Max(queueCol * (IconWidth + IconSpacing), MinWidth);

			if (newWidth != Bounds.Width)
			{
				var wasInBounds = EventBounds.Contains(Viewport.LastMousePos);
				Bounds.Width = newWidth;
				var isInBounds = EventBounds.Contains(Viewport.LastMousePos);

				// HACK: Ui.MouseOverWidget is normally only updated when the mouse moves
				// Call ResetTooltips to force a fake mouse movement so the checks in Tick will work properly
				if (wasInBounds != isInBounds)
					Game.RunAfterTick(Ui.ResetTooltips);
			}

			Game.Renderer.DisableAntialiasingFilter();

			var tiny = Game.Renderer.Fonts["Tiny"];
			var bold = Game.Renderer.Fonts["Small"];
			foreach (var icon in productionIcons)
			{
				var current = icon.Queued.First();
				var text = GetOverlayForItem(current, world.Timestep);
				tiny.DrawTextWithContrast(text,
					icon.Pos + new float2(16, 12) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);

				if (icon.Queued.Count > 1)
				{
					text = icon.Queued.Count.ToString();
					bold.DrawTextWithContrast(text, icon.Pos + new float2(16, 0) - new float2(bold.Measure(text).X / 2, 0),
						Color.White, Color.Black, 1);
				}
			}

			var parentWidth = Bounds.X + Bounds.Width;
			Parent.Bounds.Width = parentWidth;

			var gradient = Parent.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			var offset = gradient.Bounds.X - Bounds.X;
			var gradientWidth = Math.Max(MinWidth - offset, currentItemsByItem.Count * (IconWidth + IconSpacing));

			gradient.Bounds.Width = gradientWidth;
			var widestChildWidth = Parent.Parent.Children.Max(x => x.Bounds.Width);

			Parent.Parent.Bounds.Width = Math.Max(25 + widestChildWidth, Bounds.Left + MinWidth);
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

		public override void Tick()
		{
			if (TooltipContainer == null)
				return;

			if (Ui.MouseOverWidget != this)
			{
				if (TooltipIcon != null)
				{
					tooltipContainer.Value.RemoveTooltip(currentTooltipToken);
					lastIconIdx = 0;
					TooltipIcon = null;
				}

				return;
			}

			if (TooltipIcon != null && productionIconsBounds.Count > lastIconIdx && productionIcons[lastIconIdx].Actor == TooltipIcon.Actor && productionIconsBounds[lastIconIdx].Contains(Viewport.LastMousePos))
				return;

			for (var i = 0; i < productionIconsBounds.Count; i++)
			{
				if (!productionIconsBounds[i].Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipIcon = productionIcons[i];
				currentTooltipToken = tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "player", GetPlayer() }, { "getTooltipIcon", GetTooltipIcon } });
				return;
			}

			TooltipIcon = null;
		}
	}
}
