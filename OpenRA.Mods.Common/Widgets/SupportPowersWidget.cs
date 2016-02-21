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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
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

		public readonly string ClockAnimation = "clock";
		public readonly string ClockSequence = "idle";
		public readonly string ClockPalette = "chrome";

		public int IconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => { };

		readonly WorldRenderer worldRenderer;
		readonly SupportPowerManager spm;

		Animation icon;
		Animation clock;
		Dictionary<Rectangle, SupportPowerIcon> icons = new Dictionary<Rectangle, SupportPowerIcon>();

		public SupportPowerIcon TooltipIcon { get; private set; }
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
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			icon = new Animation(world, "icon");
			clock = new Animation(world, ClockAnimation);
		}

		public class SupportPowerIcon
		{
			public SupportPowerInstance Power;
			public float2 Pos;
			public Sprite Sprite;
			public PaletteReference Palette;
			public PaletteReference IconClockPalette;
			public Hotkey Hotkey;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, SupportPowerIcon>();
			var powers = spm.Powers.Values.Where(p => !p.Disabled);

			var oldIconCount = IconCount;
			IconCount = 0;

			var rb = RenderBounds;
			var ks = Game.Settings.Keys;

			foreach (var p in powers)
			{
				var rect = new Rectangle(rb.X, rb.Y + IconCount * (IconSize.Y + IconMargin), IconSize.X, IconSize.Y);
				icon.Play(p.Info.Icon);

				var power = new SupportPowerIcon()
				{
					Power = p,
					Pos = new float2(rect.Location),
					Sprite = icon.Image,
					Palette = worldRenderer.Palette(p.Info.IconPalette),
					IconClockPalette = worldRenderer.Palette(ClockPalette),
					Hotkey = ks.GetSupportPowerHotkey(IconCount)
				};

				icons.Add(rect, power);
				IconCount++;
			}

			eventBounds = icons.Any() ? icons.Keys.Aggregate(Rectangle.Union) : Rectangle.Empty;

			if (oldIconCount != IconCount)
				OnIconCountChanged(oldIconCount, IconCount);
		}

		protected void ClickIcon(SupportPowerIcon clicked)
		{
			if (!clicked.Power.Active)
				Game.Sound.PlayToPlayer(spm.Self.Owner, clicked.Power.Info.InsufficientPowerSound);
			else
				clicked.Power.Target();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var hotkey = Hotkey.FromKeyInput(e);
				var a = icons.Values.FirstOrDefault(i => i.Hotkey == hotkey);

				if (a != null)
				{
					ClickIcon(a);
					return true;
				}
			}

			return false;
		}

		public override void Draw()
		{
			var iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;
			overlayFont = Game.Renderer.Fonts["TinyBold"];

			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0, worldRenderer.World.Timestep)) / 2;

			// Icons
			foreach (var p in icons.Values)
			{
				WidgetUtils.DrawSHPCentered(p.Sprite, p.Pos + iconOffset, p.Palette);

				// Charge progress
				var sp = p.Power;
				clock.PlayFetchIndex(ClockSequence,
					() => sp.TotalTime == 0 ? clock.CurrentSequence.Length - 1 : (sp.TotalTime - sp.RemainingTime)
					* (clock.CurrentSequence.Length - 1) / sp.TotalTime);

				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, p.Pos + iconOffset, p.IconClockPalette);
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
					overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(p.Power.RemainingTime, worldRenderer.World.Timestep),
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
				new WidgetArgs() { { "world", worldRenderer.World }, { "palette", this } });
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

				TooltipIcon = icon;
				return false;
			}

			if (mi.Event != MouseInputEvent.Down)
				return false;

			var clicked = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (clicked != null)
				ClickIcon(clicked);

			return true;
		}
	}
}
