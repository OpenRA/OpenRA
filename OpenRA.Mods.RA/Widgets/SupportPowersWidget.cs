#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class SupportPowersWidget : Widget
	{
		[Translate] public readonly string ReadyText = "";
		[Translate] public readonly string HoldText = "";

		public readonly int2 IconSize = new int2(64, 48);
		public readonly int IconMargin = 10;
		public readonly int2 IconSpriteOffset = int2.Zero;

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SUPPORT_POWER_TOOLTIP";

		public int IconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => {};

		readonly World world;
		readonly WorldRenderer worldRenderer;

		Animation icon;
		Animation clock;
		Dictionary<Rectangle, SupportPowerIcon> icons = new Dictionary<Rectangle, SupportPowerIcon>();

		public SupportPower TooltipPower { get; private set; }
		Lazy<TooltipContainerWidget> tooltipContainer;

		Rectangle eventBounds;
		public override Rectangle EventBounds { get { return eventBounds; } }
		SpriteFont overlayFont;
		float2 holdOffset, readyOffset, timeOffset;

		[ObjectCreator.UseCtor]
		public SupportPowersWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			icon = new Animation(world, "icon");
			clock = new Animation(world, "clock");
		}

		public class SupportPowerIcon
		{
			public SupportPower Power;
			public float2 Pos;
			public Sprite Sprite;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, SupportPowerIcon>();
			var oldIconCount = IconCount;
			IconCount = 0;

	
			var rb = RenderBounds;

			var powers = world.ActorsWithTrait<SupportPower>().Where(s => s.Actor.Owner == world.LocalPlayer && s.Trait.HasPrerequisites);
			foreach (var sp in powers)
			{
				var rect = new Rectangle(rb.X, rb.Y + IconCount * (IconSize.Y + IconMargin), IconSize.X, IconSize.Y);
				icon.Play(sp.Trait.Info.Icon);

				var power = new SupportPowerIcon()
				{
					Power = sp.Trait,
					Pos = new float2(rect.Location),
					Sprite = icon.Image
				};

				icons.Add(rect, power);
				IconCount++;
			}

			eventBounds = icons.Any() ? icons.Keys.Aggregate(Rectangle.Union) : Rectangle.Empty;

			if (oldIconCount != IconCount)
				OnIconCountChanged(oldIconCount, IconCount);
		}

		public override void Draw()
		{
			var iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;
			overlayFont = Game.Renderer.Fonts["TinyBold"];

			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0)) / 2;

			// Icons
			foreach (var p in icons.Values)
			{
				WidgetUtils.DrawSHPCentered(p.Sprite, p.Pos + iconOffset, worldRenderer);

				// Charge progress
				var sp = p.Power;
				clock.PlayFetchIndex("idle",
					() => sp.TotalTime == 0 ? clock.CurrentSequence.Length - 1 : (sp.TotalTime - sp.RemainingTime)
					* (clock.CurrentSequence.Length - 1) / sp.TotalTime);

				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, p.Pos + iconOffset, worldRenderer);
			}

			// Overlay
			foreach (var p in icons.Values)
			{
				if (p.Power.Ready)
					overlayFont.DrawTextWithContrast(ReadyText,
						p.Pos + readyOffset,
						Color.White, Color.Black, 1);
				else if (p.Power.Disabled)
					overlayFont.DrawTextWithContrast(HoldText,
						p.Pos + holdOffset,
						Color.White, Color.Black, 1);
				else
					overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(p.Power.RemainingTime),
						p.Pos + timeOffset,
						Color.White, Color.Black, 1);
			}
		}

		public override void Tick()
		{
			// TODO: Only do this when the powers have changed
			RefreshIcons();
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() { { "palette", this } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
			{
				var icon = icons.Where(i => i.Key.Contains(mi.Location))
					.Select(i => i.Value).FirstOrDefault();

				TooltipPower = icon != null ? icon.Power : null;
				return false;
			}

			if (mi.Event != MouseInputEvent.Down)
				return false;

			var clicked = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (clicked != null)
			{
				if (clicked.Power.Disabled)
					Sound.PlayToPlayer(world.LocalPlayer, clicked.Power.Info.InsufficientPowerSound);

				clicked.Power.TargetLocation();
			}

			return true;
		}
	}
}