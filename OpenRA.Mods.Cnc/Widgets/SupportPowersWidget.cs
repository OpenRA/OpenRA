#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class SupportPowersWidget : Widget
	{
		public readonly string ReadyText = "";
		public readonly string HoldText = "";

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SUPPORT_POWER_TOOLTIP";

		public int Spacing = 10;

		readonly WorldRenderer worldRenderer;
		readonly SupportPowerManager spm;

		Animation icon;
		Animation clock;
		Dictionary<Rectangle, SupportPowerIcon> icons = new Dictionary<Rectangle, SupportPowerIcon>();

		public SupportPowerInstance TooltipPower { get; private set; }
		Lazy<TooltipContainerWidget> tooltipContainer;

		Rectangle eventBounds;
		public override Rectangle EventBounds { get { return eventBounds; } }
		SpriteFont overlayFont;
		float2 holdOffset, readyOffset, timeOffset;

		[ObjectCreator.UseCtor]
		public SupportPowersWidget(World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			spm = world.LocalPlayer.PlayerActor.Trait<SupportPowerManager>();
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			icon = new Animation("icon");
			clock = new Animation("clock");
		}

		public class SupportPowerIcon
		{
			public SupportPowerInstance Power;
			public float2 Pos;
			public Sprite Sprite;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, SupportPowerIcon>();
			var powers = spm.Powers.Values.Where(p => !p.Disabled);

			var i = 0;
			var rb = RenderBounds;
			foreach (var p in powers)
			{
				var rect = new Rectangle(rb.X + 1, rb.Y + i * (48 + Spacing) + 1, 64, 48);
				icon.Play(p.Info.Icon);
				var power = new SupportPowerIcon()
				{
					Power = p,
					Pos = new float2(rect.Location),
					Sprite = icon.Image
				};

				icons.Add(rect, power);
				i++;
			}

			eventBounds = (icons.Count == 0) ? Rectangle.Empty : icons.Keys.Aggregate(Rectangle.Union);
		}

		public override void Draw()
		{
			overlayFont = Game.Renderer.Fonts["TinyBold"];
			holdOffset = new float2(32, 24) - overlayFont.Measure(HoldText) / 2;
			readyOffset = new float2(32, 24) - overlayFont.Measure(ReadyText) / 2;
			timeOffset = new float2(32, 24) - overlayFont.Measure(WidgetUtils.FormatTime(0)) / 2;

			// Background
			foreach (var rect in icons.Keys)
				WidgetUtils.DrawPanel("panel-black", rect.InflateBy(1, 1, 1, 1));

			// Icons
			foreach (var p in icons.Values)
			{
				WidgetUtils.DrawSHP(p.Sprite, p.Pos, worldRenderer);

				// Charge progress
				clock.PlayFetchIndex("idle",
					() => (p.Power.TotalTime - p.Power.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / p.Power.TotalTime);
				clock.Tick();
				WidgetUtils.DrawSHP(clock.Image, p.Pos, worldRenderer);
			}

			// Overlay
			foreach (var p in icons.Values)
			{
				if (p.Power.Ready)
					overlayFont.DrawTextWithContrast(ReadyText,
						p.Pos + readyOffset,
						Color.White, Color.Black, 1);
				else if (!p.Power.Active)
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
			if (TooltipContainer == null) return;
			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() { { "palette", this } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
			{
				var icon = icons.Where(i => i.Key.Contains(mi.Location))
					.Select(i => i.Value).FirstOrDefault();
				TooltipPower = (icon != null) ? icon.Power : null;
				return false;
			}

			if (mi.Event != MouseInputEvent.Down)
				return false;

			var clicked = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (clicked != null)
				spm.Target(clicked.Power.Info.OrderName);

			return true;
		}
	}
}