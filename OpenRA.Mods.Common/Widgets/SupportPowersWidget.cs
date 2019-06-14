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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SupportPowersWidget : Widget
	{
		[Translate]
		public readonly string ReadyText = "";

		[Translate]
		public readonly string HoldText = "";

		public readonly int2 IconSize = new int2(64, 48);
		public readonly int IconMargin = 10;
		public readonly int2 IconSpriteOffset = int2.Zero;

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SUPPORT_POWER_TOOLTIP";

		// Note: LinterHotkeyNames assumes that these are disabled by default
		public readonly string HotkeyPrefix = null;
		public readonly int HotkeyCount = 0;

		public readonly string ClockAnimation = "clock";
		public readonly string ClockSequence = "idle";
		public readonly string ClockPalette = "chrome";

		public readonly bool Horizontal = false;

		public int IconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => { };

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		readonly SupportPowerManager spm;

		Animation icon;
		Animation clock;
		Dictionary<Rectangle, SupportPowerIcon> icons = new Dictionary<Rectangle, SupportPowerIcon>();

		public SupportPowerIcon TooltipIcon { get; private set; }
		public Func<SupportPowerIcon> GetTooltipIcon;
		Lazy<TooltipContainerWidget> tooltipContainer;
		HotkeyReference[] hotkeys;

		Rectangle eventBounds;
		public override Rectangle EventBounds { get { return eventBounds; } }
		SpriteFont overlayFont;
		float2 holdOffset, readyOffset, timeOffset;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError, Action<string> emitWarning)
		{
			var prefix = "";
			var prefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyPrefix");
			if (prefixNode != null)
				prefix = prefixNode.Value.Value;

			var count = 0;
			var countNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyCount");
			if (countNode != null)
				count = FieldLoader.GetValue<int>("HotkeyCount", countNode.Value.Value);

			if (count == 0)
				return new string[0];

			if (string.IsNullOrEmpty(prefix))
				emitError("{0} must define HotkeyPrefix if HotkeyCount > 0.".F(widgetNode.Location));

			return Exts.MakeArray(count, i => prefix + (i + 1).ToString("D2"));
		}

		[ObjectCreator.UseCtor]
		public SupportPowersWidget(ModData modData, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.worldRenderer = worldRenderer;
			GetTooltipIcon = () => TooltipIcon;
			spm = world.LocalPlayer.PlayerActor.Trait<SupportPowerManager>();
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			icon = new Animation(world, "icon");
			clock = new Animation(world, ClockAnimation);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			hotkeys = Exts.MakeArray(HotkeyCount,
				i => modData.Hotkeys[HotkeyPrefix + (i + 1).ToString("D2")]);
		}

		public class SupportPowerIcon
		{
			public SupportPowerInstance Power;
			public float2 Pos;
			public Sprite Sprite;
			public PaletteReference Palette;
			public PaletteReference IconClockPalette;
			public HotkeyReference Hotkey;
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, SupportPowerIcon>();
			var powers = spm.Powers.Values.Where(p => !p.Disabled)
				.OrderBy(p => p.Info.SupportPowerPaletteOrder);

			var oldIconCount = IconCount;
			IconCount = 0;

			var rb = RenderBounds;
			foreach (var p in powers)
			{
				Rectangle rect;
				if (Horizontal)
					rect = new Rectangle(rb.X + IconCount * (IconSize.X + IconMargin), rb.Y, IconSize.X, IconSize.Y);
				else
					rect = new Rectangle(rb.X, rb.Y + IconCount * (IconSize.Y + IconMargin), IconSize.X, IconSize.Y);

				icon.Play(p.Info.Icon);

				var power = new SupportPowerIcon()
				{
					Power = p,
					Pos = new float2(rect.Location),
					Sprite = icon.Image,
					Palette = worldRenderer.Palette(p.Info.IconPalette),
					IconClockPalette = worldRenderer.Palette(ClockPalette),
					Hotkey = IconCount < HotkeyCount ? hotkeys[IconCount] : null,
				};

				icons.Add(rect, power);
				IconCount++;
			}

			eventBounds = icons.Keys.Union();

			if (oldIconCount != IconCount)
				OnIconCountChanged(oldIconCount, IconCount);
		}

		protected void ClickIcon(SupportPowerIcon clicked)
		{
			if (!clicked.Power.Active)
			{
				Game.Sound.PlayToPlayer(SoundType.UI, spm.Self.Owner, clicked.Power.Info.InsufficientPowerSound);
				Game.Sound.PlayNotification(spm.Self.World.Map.Rules, spm.Self.Owner, "Speech",
					clicked.Power.Info.InsufficientPowerSpeechNotification, spm.Self.Owner.Faction.InternalName);
			}
			else
				clicked.Power.Target();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var a = icons.Values.FirstOrDefault(i => i.Hotkey != null && i.Hotkey.IsActivatedBy(e));

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
				new WidgetArgs() { { "world", worldRenderer.World }, { "player", spm.Self.Owner }, { "getTooltipIcon", GetTooltipIcon } });
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
