#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DisplaySettingsLogic : ChromeLogic
	{
		static readonly int OriginalVideoDisplay;
		static readonly WindowMode OriginalGraphicsMode;
		static readonly int2 OriginalGraphicsWindowedSize;
		static readonly int2 OriginalGraphicsFullscreenSize;
		static readonly GLProfile OriginalGLProfile;

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;

		static DisplaySettingsLogic()
		{
			var original = Game.Settings;
			OriginalGraphicsMode = original.Graphics.Mode;
			OriginalVideoDisplay = original.Graphics.VideoDisplay;
			OriginalGraphicsWindowedSize = original.Graphics.WindowedSize;
			OriginalGraphicsFullscreenSize = original.Graphics.FullscreenSize;
			OriginalGLProfile = original.Graphics.GLProfile;
		}

		[ObjectCreator.UseCtor]
		public DisplaySettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label, ModData modData, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			this.modData = modData;
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();

			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		public static readonly Dictionary<WorldViewport, string> ViewportSizeNames = new Dictionary<WorldViewport, string>()
		{
			{ WorldViewport.Close, "Close" },
			{ WorldViewport.Medium, "Medium" },
			{ WorldViewport.Far, "Far" },
			{ WorldViewport.Native, "Furthest" }
		};

		Func<bool> InitPanel(Widget panel)
		{
			var ds = Game.Settings.Graphics;
			var gs = Game.Settings.Game;
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			SettingsUtils.BindCheckboxPref(panel, "CURSORDOUBLE_CHECKBOX", ds, "CursorDouble");
			SettingsUtils.BindCheckboxPref(panel, "VSYNC_CHECKBOX", ds, "VSync");
			SettingsUtils.BindCheckboxPref(panel, "FRAME_LIMIT_CHECKBOX", ds, "CapFramerate");
			SettingsUtils.BindIntSliderPref(panel, "FRAME_LIMIT_SLIDER", ds, "MaxFramerate");
			SettingsUtils.BindCheckboxPref(panel, "PLAYER_STANCE_COLORS_CHECKBOX", gs, "UsePlayerStanceColors");
			if (panel.GetOrNull<CheckboxWidget>("PAUSE_SHELLMAP_CHECKBOX") != null)
				SettingsUtils.BindCheckboxPref(panel, "PAUSE_SHELLMAP_CHECKBOX", gs, "PauseShellmap");

			SettingsUtils.BindCheckboxPref(panel, "HIDE_REPLAY_CHAT_CHECKBOX", gs, "HideReplayChat");

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, ds, scrollPanel);
			windowModeDropdown.GetText = () => ds.Mode == WindowMode.Windowed ?
				"Windowed" : ds.Mode == WindowMode.Fullscreen ? "Fullscreen (Legacy)" : "Fullscreen";

			var displaySelectionDropDown = panel.Get<DropDownButtonWidget>("DISPLAY_SELECTION_DROPDOWN");
			displaySelectionDropDown.OnMouseDown = _ => ShowDisplaySelectionDropdown(displaySelectionDropDown, ds);
			var displaySelectionLabel = new CachedTransform<int, string>(i => $"Display {i + 1}");
			displaySelectionDropDown.GetText = () => displaySelectionLabel.Update(ds.VideoDisplay);
			displaySelectionDropDown.IsDisabled = () => Game.Renderer.DisplayCount < 2;

			var glProfileLabel = new CachedTransform<GLProfile, string>(p => p.ToString());
			var glProfileDropdown = panel.Get<DropDownButtonWidget>("GL_PROFILE_DROPDOWN");
			var disableProfile = Game.Renderer.SupportedGLProfiles.Length < 2 && ds.GLProfile == GLProfile.Automatic;
			glProfileDropdown.OnMouseDown = _ => ShowGLProfileDropdown(glProfileDropdown, ds);
			glProfileDropdown.GetText = () => glProfileLabel.Update(ds.GLProfile);
			glProfileDropdown.IsDisabled = () => disableProfile;

			var statusBarsDropDown = panel.Get<DropDownButtonWidget>("STATUS_BAR_DROPDOWN");
			statusBarsDropDown.OnMouseDown = _ => ShowStatusBarsDropdown(statusBarsDropDown, gs);
			statusBarsDropDown.GetText = () => gs.StatusBars == StatusBarsType.Standard ?
				"Standard" : gs.StatusBars == StatusBarsType.DamageShow ? "Show On Damage" : "Always Show";

			var targetLinesDropDown = panel.Get<DropDownButtonWidget>("TARGET_LINES_DROPDOWN");
			targetLinesDropDown.OnMouseDown = _ => ShowTargetLinesDropdown(targetLinesDropDown, gs);
			targetLinesDropDown.GetText = () => gs.TargetLines == TargetLinesType.Automatic ?
				"Automatic" : gs.TargetLines == TargetLinesType.Manual ? "Manual" : "Disabled";

			var battlefieldCameraDropDown = panel.Get<DropDownButtonWidget>("BATTLEFIELD_CAMERA_DROPDOWN");
			var battlefieldCameraLabel = new CachedTransform<WorldViewport, string>(vs => ViewportSizeNames[vs]);
			battlefieldCameraDropDown.OnMouseDown = _ => ShowBattlefieldCameraDropdown(battlefieldCameraDropDown, viewportSizes, ds);
			battlefieldCameraDropDown.GetText = () => battlefieldCameraLabel.Update(ds.ViewportDistance);

			BindTextNotificationPoolFilterSettings(panel, gs);

			// Update vsync immediately
			var vsyncCheckbox = panel.Get<CheckboxWidget>("VSYNC_CHECKBOX");
			var vsyncOnClick = vsyncCheckbox.OnClick;
			vsyncCheckbox.OnClick = () =>
			{
				vsyncOnClick();
				Game.Renderer.SetVSyncEnabled(ds.VSync);
			};

			var uiScaleDropdown = panel.Get<DropDownButtonWidget>("UI_SCALE_DROPDOWN");
			var uiScaleLabel = new CachedTransform<float, string>(s => $"{(int)(100 * s)}%");
			uiScaleDropdown.OnMouseDown = _ => ShowUIScaleDropdown(uiScaleDropdown, ds);
			uiScaleDropdown.GetText = () => uiScaleLabel.Update(ds.UIScale);

			var minResolution = viewportSizes.MinEffectiveResolution;
			var resolution = Game.Renderer.Resolution;
			var disableUIScale = worldRenderer.World.Type != WorldType.Shellmap ||
				resolution.Width * ds.UIScale < 1.25f * minResolution.Width ||
				resolution.Height * ds.UIScale < 1.25f * minResolution.Height;

			uiScaleDropdown.IsDisabled = () => disableUIScale;

			panel.Get("DISPLAY_SELECTION_CONTAINER").IsVisible = () => ds.Mode != WindowMode.Windowed;
			panel.Get("WINDOW_RESOLUTION_CONTAINER").IsVisible = () => ds.Mode == WindowMode.Windowed;
			var windowWidth = panel.Get<TextFieldWidget>("WINDOW_WIDTH");
			var origWidthText = windowWidth.Text = ds.WindowedSize.X.ToString();

			var windowHeight = panel.Get<TextFieldWidget>("WINDOW_HEIGHT");
			var origHeightText = windowHeight.Text = ds.WindowedSize.Y.ToString();
			windowHeight.Text = ds.WindowedSize.Y.ToString();

			var restartDesc = panel.Get("RESTART_REQUIRED_DESC");
			restartDesc.IsVisible = () => ds.Mode != OriginalGraphicsMode || ds.VideoDisplay != OriginalVideoDisplay || ds.GLProfile != OriginalGLProfile ||
				(ds.Mode == WindowMode.Windowed && (origWidthText != windowWidth.Text || origHeightText != windowHeight.Text));

			var frameLimitCheckbox = panel.Get<CheckboxWidget>("FRAME_LIMIT_CHECKBOX");
			var frameLimitOrigLabel = frameLimitCheckbox.Text;
			var frameLimitLabel = new CachedTransform<int, string>(fps => frameLimitOrigLabel + $" ({fps} FPS)");
			frameLimitCheckbox.GetText = () => frameLimitLabel.Update(ds.MaxFramerate);

			panel.Get<SliderWidget>("FRAME_LIMIT_SLIDER").IsDisabled = () => !frameLimitCheckbox.IsChecked();

			// Player profile
			var ps = Game.Settings.Player;

			var escPressed = false;
			var nameTextfield = panel.Get<TextFieldWidget>("PLAYERNAME");
			nameTextfield.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
			nameTextfield.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				nameTextfield.Text = nameTextfield.Text.Trim();
				if (nameTextfield.Text.Length == 0)
					nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
				else
				{
					nameTextfield.Text = Settings.SanitizedPlayerName(nameTextfield.Text);
					ps.Name = nameTextfield.Text;
				}
			};

			nameTextfield.OnEnterKey = _ => { nameTextfield.YieldKeyboardFocus(); return true; };
			nameTextfield.OnEscKey = _ =>
			{
				nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
				escPressed = true;
				nameTextfield.YieldKeyboardFocus();
				return true;
			};

			var colorManager = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<ColorPickerManagerInfo>();
			colorManager.Color = ps.Color;

			var colorDropdown = panel.Get<DropDownButtonWidget>("PLAYERCOLOR");
			colorDropdown.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			colorDropdown.OnMouseDown = _ => ColorPickerLogic.ShowColorDropDown(colorDropdown, colorManager, worldRenderer, () =>
			{
				Game.Settings.Player.Color = colorManager.Color;
				Game.Settings.Save();
			});
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => ps.Color;

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () =>
			{
				Exts.TryParseIntegerInvariant(windowWidth.Text, out var x);
				Exts.TryParseIntegerInvariant(windowHeight.Text, out var y);
				ds.WindowedSize = new int2(x, y);
				nameTextfield.YieldKeyboardFocus();

				return ds.Mode != OriginalGraphicsMode ||
					ds.VideoDisplay != OriginalVideoDisplay ||
					ds.WindowedSize != OriginalGraphicsWindowedSize ||
					ds.FullscreenSize != OriginalGraphicsFullscreenSize ||
					ds.GLProfile != OriginalGLProfile;
			};
		}

		Action ResetPanel(Widget panel)
		{
			var ds = Game.Settings.Graphics;
			var ps = Game.Settings.Player;
			var gs = Game.Settings.Game;
			var dds = new GraphicSettings();
			var dps = new PlayerSettings();
			var dgs = new GameSettings();
			return () =>
			{
				ds.CapFramerate = dds.CapFramerate;
				ds.MaxFramerate = dds.MaxFramerate;
				ds.GLProfile = dds.GLProfile;
				ds.Mode = dds.Mode;
				ds.VideoDisplay = dds.VideoDisplay;
				ds.WindowedSize = dds.WindowedSize;
				ds.CursorDouble = dds.CursorDouble;
				ds.ViewportDistance = dds.ViewportDistance;

				if (ds.UIScale != dds.UIScale)
				{
					var oldScale = ds.UIScale;
					ds.UIScale = dds.UIScale;
					Game.Renderer.SetUIScale(dds.UIScale);
					RecalculateWidgetLayout(Ui.Root);
					Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / ds.UIScale).ToInt2();
				}

				ps.Color = dps.Color;
				ps.Name = dps.Name;

				gs.TextNotificationPoolFilters = dgs.TextNotificationPoolFilters;
			};
		}

		static void ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings s, ScrollPanelWidget scrollPanel)
		{
			var options = new Dictionary<string, WindowMode>()
			{
				{ "Fullscreen", WindowMode.PseudoFullscreen },
				{ "Fullscreen (Legacy)", WindowMode.Fullscreen },
				{ "Windowed", WindowMode.Windowed },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.Mode == options[o],
					() =>
					{
						s.Mode = options[o];
						SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);
					});

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		public static void BindTextNotificationPoolFilterSettings(Widget panel, GameSettings gs)
		{
			Action<TextNotificationPoolFilters> toggleFilterFlag = f =>
			{
				gs.TextNotificationPoolFilters ^= f;
				Game.Settings.Save();
			};

			var feedbackCheckbox = panel.GetOrNull<CheckboxWidget>("UI_FEEDBACK_CHECKBOX");
			if (feedbackCheckbox != null)
			{
				feedbackCheckbox.IsChecked = () => gs.TextNotificationPoolFilters.HasFlag(TextNotificationPoolFilters.Feedback);
				feedbackCheckbox.OnClick = () => toggleFilterFlag(TextNotificationPoolFilters.Feedback);
			}

			var transientsCheckbox = panel.GetOrNull<CheckboxWidget>("TRANSIENTS_CHECKBOX");
			if (transientsCheckbox != null)
			{
				transientsCheckbox.IsChecked = () => gs.TextNotificationPoolFilters.HasFlag(TextNotificationPoolFilters.Transients);
				transientsCheckbox.OnClick = () => toggleFilterFlag(TextNotificationPoolFilters.Transients);
			}
		}

		static void ShowStatusBarsDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, StatusBarsType>()
			{
				{ "Standard", StatusBarsType.Standard },
				{ "Show On Damage", StatusBarsType.DamageShow },
				{ "Always Show", StatusBarsType.AlwaysShow },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.StatusBars == options[o],
					() => s.StatusBars = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		static void ShowDisplaySelectionDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.VideoDisplay == o,
					() => s.VideoDisplay = o);

				var label = $"Display {o + 1}";
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Enumerable.Range(0, Game.Renderer.DisplayCount), setupItem);
		}

		static void ShowGLProfileDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			Func<GLProfile, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.GLProfile == o,
					() => s.GLProfile = o);

				var label = o.ToString();
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			var profiles = new[] { GLProfile.Automatic }.Concat(Game.Renderer.SupportedGLProfiles);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, profiles, setupItem);
		}

		static void ShowTargetLinesDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, TargetLinesType>()
			{
				{ "Automatic", TargetLinesType.Automatic },
				{ "Manual", TargetLinesType.Manual },
				{ "Disabled", TargetLinesType.Disabled },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.TargetLines == options[o],
					() => s.TargetLines = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		public static void ShowBattlefieldCameraDropdown(DropDownButtonWidget dropdown, WorldViewportSizes viewportSizes, GraphicSettings gs)
		{
			Func<WorldViewport, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gs.ViewportDistance == o,
					() => gs.ViewportDistance = o);

				var label = ViewportSizeNames[o];
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			var windowHeight = Game.Renderer.NativeResolution.Height;

			var validSizes = new List<WorldViewport>() { WorldViewport.Close };
			if (viewportSizes.GetSizeRange(WorldViewport.Medium).X < windowHeight)
				validSizes.Add(WorldViewport.Medium);

			var farRange = viewportSizes.GetSizeRange(WorldViewport.Far);
			if (farRange.X < windowHeight)
				validSizes.Add(WorldViewport.Far);

			if (viewportSizes.AllowNativeZoom && farRange.Y < windowHeight)
				validSizes.Add(WorldViewport.Native);

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validSizes, setupItem);
		}

		static void RecalculateWidgetLayout(Widget w, bool insideScrollPanel = false)
		{
			// HACK: Recalculate the widget bounds to fit within the new effective window bounds
			// This is fragile, and only works when called when Settings is opened via the main menu.

			// HACK: Skip children badges container on the main menu and settings tab container
			// These have a fixed size, with calculated size and children positions that break if we adjust them here
			if (w.Id == "BADGES_CONTAINER" || w.Id == "SETTINGS_TAB_CONTAINER")
				return;

			var parentBounds = w.Parent == null
				? new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
				: w.Parent.Bounds;

			var substitutions = new Dictionary<string, int>
			{
				{ "WINDOW_RIGHT", Game.Renderer.Resolution.Width },
				{ "WINDOW_BOTTOM", Game.Renderer.Resolution.Height },
				{ "PARENT_RIGHT", parentBounds.Width },
				{ "PARENT_LEFT", parentBounds.Left },
				{ "PARENT_TOP", parentBounds.Top },
				{ "PARENT_BOTTOM", parentBounds.Height }
			};

			var width = Evaluator.Evaluate(w.Width, substitutions);
			var height = Evaluator.Evaluate(w.Height, substitutions);

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			if (insideScrollPanel)
				w.Bounds = new Rectangle(w.Bounds.X, w.Bounds.Y, width, w.Bounds.Height);
			else
				w.Bounds = new Rectangle(
					Evaluator.Evaluate(w.X, substitutions),
					Evaluator.Evaluate(w.Y, substitutions),
					width,
					height);

			foreach (var c in w.Children)
				RecalculateWidgetLayout(c, insideScrollPanel || w is ScrollPanelWidget);
		}

		public static void ShowUIScaleDropdown(DropDownButtonWidget dropdown, GraphicSettings gs)
		{
			Func<float, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gs.UIScale == o,
					() =>
					{
						Game.RunAfterTick(() =>
						{
							var oldScale = gs.UIScale;
							gs.UIScale = o;

							Game.Renderer.SetUIScale(o);
							RecalculateWidgetLayout(Ui.Root);
							Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / gs.UIScale).ToInt2();
						});
					});

				var label = $"{(int)(100 * o)}%";
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			var viewportSizes = Game.ModData.Manifest.Get<WorldViewportSizes>();
			var maxScales = new float2(Game.Renderer.NativeResolution) / new float2(viewportSizes.MinEffectiveResolution);
			var maxScale = Math.Min(maxScales.X, maxScales.Y);

			var validScales = new[] { 1f, 1.25f, 1.5f, 1.75f, 2f }.Where(x => x <= maxScale);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validScales, setupItem);
		}
	}
}
