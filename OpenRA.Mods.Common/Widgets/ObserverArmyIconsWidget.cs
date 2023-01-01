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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverArmyIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 1;

		readonly float2 iconSize;
		public int MinWidth = 240;

		public ArmyUnit TooltipUnit { get; private set; }
		public Func<ArmyUnit> GetTooltipUnit;

		public readonly string TooltipTemplate = "ARMY_TOOLTIP";
		public readonly string TooltipContainer;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly List<ArmyIcon> armyIcons = new List<ArmyIcon>();

		readonly CachedTransform<Player, PlayerStatistics> stats = new CachedTransform<Player, PlayerStatistics>(player => player.PlayerActor.TraitOrDefault<PlayerStatistics>());

		int lastIconIdx;
		int currentTooltipToken;

		[ObjectCreator.UseCtor]
		public ObserverArmyIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			GetTooltipUnit = () => TooltipUnit;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ObserverArmyIconsWidget(ObserverArmyIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			world = other.world;
			worldRenderer = other.worldRenderer;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;
			iconSize = new float2(IconWidth, IconHeight);

			MinWidth = other.MinWidth;

			TooltipUnit = other.TooltipUnit;
			GetTooltipUnit = () => TooltipUnit;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Draw()
		{
			armyIcons.Clear();

			var player = GetPlayer();
			if (player == null)
				return;

			var playerStatistics = stats.Update(player);

			var items = playerStatistics.Units.Values
				.Where(u => u.Count > 0 && u.Icon != null)
				.OrderBy(u => u.ProductionQueueOrder)
				.ThenBy(u => u.BuildPaletteOrder);

			Game.Renderer.EnableAntialiasingFilter();

			var queueCol = 0;
			foreach (var unit in items)
			{
				var icon = unit.Icon;
				var topLeftOffset = new int2(queueCol * (IconWidth + IconSpacing), 0);

				var iconTopLeft = RenderOrigin + topLeftOffset;
				var centerPosition = iconTopLeft;

				var palette = unit.IconPaletteIsPlayerPalette ? unit.IconPalette + player.InternalName : unit.IconPalette;
				WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(palette), centerPosition + 0.5f * iconSize, 0.5f);

				armyIcons.Add(new ArmyIcon
				{
					Bounds = new Rectangle(iconTopLeft.X, iconTopLeft.Y, (int)iconSize.X, (int)iconSize.Y),
					Unit = unit
				});

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

			var bold = Game.Renderer.Fonts["TinyBold"];
			foreach (var armyIcon in armyIcons)
			{
				var text = armyIcon.Unit.Count.ToString();
				bold.DrawTextWithContrast(text, armyIcon.Bounds.Location + new float2(iconSize.X, 0) - new float2(bold.Measure(text).X, bold.TopOffset),
					Color.White, Color.Black, 1);
			}

			var parentWidth = Bounds.X + Bounds.Width;
			Parent.Bounds.Width = parentWidth;

			var gradient = Parent.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			var offset = gradient.Bounds.X - Bounds.X;
			var gradientWidth = Math.Max(MinWidth - offset, queueCol * (IconWidth + IconSpacing));

			gradient.Bounds.Width = gradientWidth;
			var widestChildWidth = Parent.Parent.Children.Max(x => x.Bounds.Width);

			Parent.Parent.Bounds.Width = Math.Max(25 + widestChildWidth, Bounds.Left + MinWidth);
		}

		public override Widget Clone()
		{
			return new ObserverArmyIconsWidget(this);
		}

		public override void Tick()
		{
			if (TooltipContainer == null)
				return;

			if (Ui.MouseOverWidget != this)
			{
				if (TooltipUnit != null)
				{
					tooltipContainer.Value.RemoveTooltip(currentTooltipToken);
					lastIconIdx = 0;
					TooltipUnit = null;
				}

				return;
			}

			if (TooltipUnit != null && lastIconIdx < armyIcons.Count)
			{
				var armyIcon = armyIcons[lastIconIdx];
				if (armyIcon.Unit.ActorInfo == TooltipUnit.ActorInfo && armyIcon.Bounds.Contains(Viewport.LastMousePos))
					return;
			}

			for (var i = 0; i < armyIcons.Count; i++)
			{
				var armyIcon = armyIcons[i];
				if (!armyIcon.Bounds.Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipUnit = armyIcon.Unit;
				currentTooltipToken = tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs { { "getTooltipUnit", GetTooltipUnit } });

				return;
			}

			TooltipUnit = null;
		}

		class ArmyIcon
		{
			public Rectangle Bounds { get; set; }
			public ArmyUnit Unit { get; set; }
		}
	}
}
