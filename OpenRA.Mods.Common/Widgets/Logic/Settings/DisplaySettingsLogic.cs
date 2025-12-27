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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DisplaySettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string Close = "options-camera.close";

		[FluentReference]
		const string Medium = "options-camera.medium";

		[FluentReference]
		const string Far = "options-camera.far";

		[FluentReference]
		const string Furthest = "options-camera.furthest";

		[FluentReference]
		const string Windowed = "options-display-mode.windowed";

		[FluentReference]
		const string LegacyFullscreen = "options-display-mode.legacy-fullscreen";

		[FluentReference]
		const string Fullscreen = "options-display-mode.fullscreen";

		[FluentReference("number")]
		const string Display = "label-video-display-index";

		[FluentReference]
		const string Standard = "options-status-bars.standard";

		[FluentReference]
		const string ShowOnDamage = "options-status-bars.show-on-damage";

		[FluentReference]
		const string AlwaysShow = "options-status-bars.always-show";

		[FluentReference]
		const string Automatic = "options-target-lines.automatic";

		[FluentReference]
		const string Manual = "options-target-lines.manual";

		[FluentReference]
		const string Disabled = "options-target-lines.disabled";

		[FluentReference("fps")]
		const string FrameLimiter = "checkbox-frame-limiter";

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;
		readonly GameSettings gameSettings;
		readonly GraphicSettings graphicSettings;
		static GraphicSettings originalGraphicSettings;

		readonly string showOnDamage;
		readonly string alwaysShow;

		readonly string automatic;
		readonly string manual;
		readonly string disabled;

		readonly string legacyFullscreen;
		readonly string fullscreen;

		[ObjectCreator.UseCtor]
		public DisplaySettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			this.modData = modData;
			viewportSizes = modData.GetOrCreate<WorldViewportSizes>();
			gameSettings = modData.GetSettings<GameSettings>();
			graphicSettings = modData.GetSettings<GraphicSettings>();
			originalGraphicSettings ??= graphicSettings.Clone();

			legacyFullscreen = FluentProvider.GetMessage(LegacyFullscreen);
			fullscreen = FluentProvider.GetMessage(Fullscreen);

			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);

			showOnDamage = FluentProvider.GetMessage(ShowOnDamage);
			alwaysShow = FluentProvider.GetMessage(AlwaysShow);

			automatic = FluentProvider.GetMessage(Automatic);
			manual = FluentProvider.GetMessage(Manual);
			disabled = FluentProvider.GetMessage(Disabled);
		}

		public static string GetViewportSizeName(ModData modData, WorldViewport worldViewport)
		{
			switch (worldViewport)
			{
				case WorldViewport.Close:
					return FluentProvider.GetMessage(Close);
				case WorldViewport.Medium:
					return FluentProvider.GetMessage(Medium);
				case WorldViewport.Far:
					return FluentProvider.GetMessage(Far);
				case WorldViewport.Native:
					return FluentProvider.GetMessage(Furthest);
				default:
					return "";
			}
		}

		Func<bool> InitPanel(Widget panel)
		{
			var world = worldRenderer.World;
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			SettingsUtils.BindCheckboxPref(panel, "CURSORDOUBLE_CHECKBOX", graphicSettings, "CursorDouble");
			SettingsUtils.BindCheckboxPref(panel, "VSYNC_CHECKBOX", graphicSettings, "VSync");
			SettingsUtils.BindCheckboxPref(panel, "FRAME_LIMIT_CHECKBOX", graphicSettings, "CapFramerate");
			SettingsUtils.BindCheckboxPref(panel, "FRAME_LIMIT_GAMESPEED_CHECKBOX", graphicSettings, "CapFramerateToGameFps");
			SettingsUtils.BindIntSliderPref(panel, "FRAME_LIMIT_SLIDER", graphicSettings, "MaxFramerate");
			SettingsUtils.BindCheckboxPref(panel, "PLAYER_STANCE_COLORS_CHECKBOX", gameSettings, "UsePlayerStanceColors");

			var cb = panel.Get<CheckboxWidget>("PLAYER_STANCE_COLORS_CHECKBOX");
			cb.IsChecked = () => gameSettings.UsePlayerStanceColors;
			cb.OnClick = () =>
			{
				gameSettings.UsePlayerStanceColors = cb.IsChecked() ^ true;
				Player.SetupRelationshipColors(world.Players, world.LocalPlayer, worldRenderer, false);
			};

			if (panel.GetOrNull<CheckboxWidget>("PAUSE_SHELLMAP_CHECKBOX") != null)
				SettingsUtils.BindCheckboxPref(panel, "PAUSE_SHELLMAP_CHECKBOX", gameSettings, "PauseShellmap");

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, graphicSettings, scrollPanel);
			windowModeDropdown.GetText = () => graphicSettings.Mode == WindowMode.Windowed
				? FluentProvider.GetMessage(Windowed)
				: graphicSettings.Mode == WindowMode.Fullscreen ? legacyFullscreen : fullscreen;

			var displaySelectionDropDown = panel.Get<DropDownButtonWidget>("DISPLAY_SELECTION_DROPDOWN");
			displaySelectionDropDown.OnMouseDown = _ => ShowDisplaySelectionDropdown(displaySelectionDropDown, graphicSettings);
			var displaySelectionLabel = new CachedTransform<int, string>(i => FluentProvider.GetMessage(Display, "number", i + 1));
			displaySelectionDropDown.GetText = () => displaySelectionLabel.Update(graphicSettings.VideoDisplay);
			displaySelectionDropDown.IsDisabled = () => Game.Renderer.DisplayCount < 2;

			var glProfileLabel = new CachedTransform<GLProfile, string>(p => p.ToString());
			var glProfileDropdown = panel.Get<DropDownButtonWidget>("GL_PROFILE_DROPDOWN");
			var disableProfile = Game.Renderer.SupportedGLProfiles.Length < 2 && graphicSettings.GLProfile == GLProfile.Automatic;
			glProfileDropdown.OnMouseDown = _ => ShowGLProfileDropdown(glProfileDropdown, graphicSettings);
			glProfileDropdown.GetText = () => glProfileLabel.Update(graphicSettings.GLProfile);
			glProfileDropdown.IsDisabled = () => disableProfile;

			var statusBarsDropDown = panel.Get<DropDownButtonWidget>("STATUS_BAR_DROPDOWN");
			statusBarsDropDown.OnMouseDown = _ => ShowStatusBarsDropdown(statusBarsDropDown, gameSettings);
			statusBarsDropDown.GetText = () => gameSettings.StatusBars == StatusBarsType.Standard
				? FluentProvider.GetMessage(Standard)
				: gameSettings.StatusBars == StatusBarsType.DamageShow
					? showOnDamage
					: alwaysShow;

			var targetLinesDropDown = panel.Get<DropDownButtonWidget>("TARGET_LINES_DROPDOWN");
			targetLinesDropDown.OnMouseDown = _ => ShowTargetLinesDropdown(targetLinesDropDown, gameSettings);
			targetLinesDropDown.GetText = () => gameSettings.TargetLines == TargetLinesType.Automatic
				? automatic
				: gameSettings.TargetLines == TargetLinesType.Manual
					? manual
					: disabled;

			var battlefieldCameraDropDown = panel.Get<DropDownButtonWidget>("BATTLEFIELD_CAMERA_DROPDOWN");
			var battlefieldCameraLabel = new CachedTransform<WorldViewport, string>(vs => GetViewportSizeName(modData, vs));
			battlefieldCameraDropDown.OnMouseDown = _ => ShowBattlefieldCameraDropdown(modData, battlefieldCameraDropDown, viewportSizes, graphicSettings);
			battlefieldCameraDropDown.GetText = () => battlefieldCameraLabel.Update(graphicSettings.ViewportDistance);

			BindTextNotificationPoolFilterSettings(panel, gameSettings);

			// Update vsync immediately
			var vsyncCheckbox = panel.Get<CheckboxWidget>("VSYNC_CHECKBOX");
			var vsyncOnClick = vsyncCheckbox.OnClick;
			vsyncCheckbox.OnClick = () =>
			{
				vsyncOnClick();
				Game.Renderer.SetVSyncEnabled(graphicSettings.VSync);
			};

			var uiScaleDropdown = panel.Get<DropDownButtonWidget>("UI_SCALE_DROPDOWN");
			var uiScaleLabel = new CachedTransform<float, string>(s => $"{(int)(100 * s)}%");
			uiScaleDropdown.OnMouseDown = _ => ShowUIScaleDropdown(uiScaleDropdown, graphicSettings);
			uiScaleDropdown.GetText = () => uiScaleLabel.Update(graphicSettings.UIScale);

			var minResolution = viewportSizes.MinEffectiveResolution;
			var resolution = Game.Renderer.Resolution;
			var disableUIScale = world.Type != WorldType.Shellmap ||
				resolution.Width * graphicSettings.UIScale < 1.25f * minResolution.Width ||
				resolution.Height * graphicSettings.UIScale < 1.25f * minResolution.Height;

			uiScaleDropdown.IsDisabled = () => disableUIScale;

			panel.Get("DISPLAY_SELECTION_CONTAINER").IsVisible = () => graphicSettings.Mode != WindowMode.Windowed;
			panel.Get("WINDOW_RESOLUTION_CONTAINER").IsVisible = () => graphicSettings.Mode == WindowMode.Windowed;
			var windowWidth = panel.Get<TextFieldWidget>("WINDOW_WIDTH");
			var origWidthText = windowWidth.Text = graphicSettings.WindowedSize.X.ToString(NumberFormatInfo.CurrentInfo);

			var windowHeight = panel.Get<TextFieldWidget>("WINDOW_HEIGHT");
			var origHeightText = windowHeight.Text = graphicSettings.WindowedSize.Y.ToString(NumberFormatInfo.CurrentInfo);
			windowHeight.Text = graphicSettings.WindowedSize.Y.ToString(NumberFormatInfo.CurrentInfo);

			var restartDesc = panel.Get("VIDEO_RESTART_REQUIRED_DESC");
			restartDesc.IsVisible = () => graphicSettings.Mode != originalGraphicSettings.Mode ||
				graphicSettings.VideoDisplay != originalGraphicSettings.VideoDisplay ||
				graphicSettings.GLProfile != originalGraphicSettings.GLProfile ||
				(graphicSettings.Mode == WindowMode.Windowed && (origWidthText != windowWidth.Text || origHeightText != windowHeight.Text));

			var frameLimitGamespeedCheckbox = panel.Get<CheckboxWidget>("FRAME_LIMIT_GAMESPEED_CHECKBOX");
			var frameLimitCheckbox = panel.Get<CheckboxWidget>("FRAME_LIMIT_CHECKBOX");
			var frameLimitLabel = new CachedTransform<int, string>(fps => FluentProvider.GetMessage(FrameLimiter, "fps", fps));
			frameLimitCheckbox.GetText = () => frameLimitLabel.Update(graphicSettings.MaxFramerate);
			frameLimitCheckbox.IsDisabled = () => graphicSettings.CapFramerateToGameFps;

			panel.Get<SliderWidget>("FRAME_LIMIT_SLIDER").IsDisabled = () => !frameLimitCheckbox.IsChecked() || frameLimitGamespeedCheckbox.IsChecked();

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () =>
			{
				int.TryParse(windowWidth.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var x);
				int.TryParse(windowHeight.Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var y);
				graphicSettings.WindowedSize = new int2(x, y);

				return graphicSettings.Mode != originalGraphicSettings.Mode ||
					graphicSettings.VideoDisplay != originalGraphicSettings.VideoDisplay ||
					graphicSettings.WindowedSize != originalGraphicSettings.WindowedSize ||
					graphicSettings.FullscreenSize != originalGraphicSettings.FullscreenSize ||
					graphicSettings.GLProfile != originalGraphicSettings.GLProfile;
			};
		}

		Action ResetPanel(Widget panel)
		{
			var defaultGameSettings = new GameSettings();
			var defaultGraphicSettings = new GraphicSettings();
			return () =>
			{
				graphicSettings.CapFramerate = defaultGraphicSettings.CapFramerate;
				graphicSettings.MaxFramerate = defaultGraphicSettings.MaxFramerate;
				graphicSettings.CapFramerateToGameFps = defaultGraphicSettings.CapFramerateToGameFps;
				graphicSettings.GLProfile = defaultGraphicSettings.GLProfile;
				graphicSettings.Mode = defaultGraphicSettings.Mode;
				graphicSettings.VideoDisplay = defaultGraphicSettings.VideoDisplay;
				graphicSettings.WindowedSize = defaultGraphicSettings.WindowedSize;
				graphicSettings.CursorDouble = defaultGraphicSettings.CursorDouble;
				graphicSettings.ViewportDistance = defaultGraphicSettings.ViewportDistance;

				if (graphicSettings.UIScale != defaultGraphicSettings.UIScale)
				{
					var oldScale = graphicSettings.UIScale;
					graphicSettings.UIScale = defaultGraphicSettings.UIScale;
					Game.Renderer.SetUIScale(defaultGraphicSettings.UIScale);
					RecalculateWidgetLayout(Ui.Root);
					Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / graphicSettings.UIScale).ToInt2();
				}

				gameSettings.TextNotificationPoolFilters = defaultGameSettings.TextNotificationPoolFilters;
			};
		}

		static void ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings graphicSettings, ScrollPanelWidget scrollPanel)
		{
			var options = new Dictionary<string, WindowMode>()
			{
				{ FluentProvider.GetMessage(Fullscreen), WindowMode.PseudoFullscreen },
				{ FluentProvider.GetMessage(LegacyFullscreen), WindowMode.Fullscreen },
				{ FluentProvider.GetMessage(Windowed), WindowMode.Windowed },
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => graphicSettings.Mode == options[o],
					() =>
					{
						graphicSettings.Mode = options[o];
						SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);
					});

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		public static void BindTextNotificationPoolFilterSettings(Widget panel, GameSettings gameSettings)
		{
			void ToggleFilterFlag(TextNotificationPoolFilters f)
			{
				gameSettings.TextNotificationPoolFilters ^= f;
				gameSettings.Save();
			}

			var feedbackCheckbox = panel.GetOrNull<CheckboxWidget>("UI_FEEDBACK_CHECKBOX");
			if (feedbackCheckbox != null)
			{
				feedbackCheckbox.IsChecked = () => gameSettings.TextNotificationPoolFilters.HasFlag(TextNotificationPoolFilters.Feedback);
				feedbackCheckbox.OnClick = () => ToggleFilterFlag(TextNotificationPoolFilters.Feedback);
			}

			var transientsCheckbox = panel.GetOrNull<CheckboxWidget>("TRANSIENTS_CHECKBOX");
			if (transientsCheckbox != null)
			{
				transientsCheckbox.IsChecked = () => gameSettings.TextNotificationPoolFilters.HasFlag(TextNotificationPoolFilters.Transients);
				transientsCheckbox.OnClick = () => ToggleFilterFlag(TextNotificationPoolFilters.Transients);
			}
		}

		static void ShowStatusBarsDropdown(DropDownButtonWidget dropdown, GameSettings gameSettings)
		{
			var options = new Dictionary<string, StatusBarsType>()
			{
				{ FluentProvider.GetMessage(Standard), StatusBarsType.Standard },
				{ FluentProvider.GetMessage(ShowOnDamage), StatusBarsType.DamageShow },
				{ FluentProvider.GetMessage(AlwaysShow), StatusBarsType.AlwaysShow },
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.StatusBars == options[o],
					() => gameSettings.StatusBars = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		static void ShowDisplaySelectionDropdown(DropDownButtonWidget dropdown, GraphicSettings graphicSettings)
		{
			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => graphicSettings.VideoDisplay == o,
					() => graphicSettings.VideoDisplay = o);

				var label = $"Display {o + 1}";
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Enumerable.Range(0, Game.Renderer.DisplayCount), SetupItem);
		}

		static void ShowGLProfileDropdown(DropDownButtonWidget dropdown, GraphicSettings graphicSettings)
		{
			ScrollItemWidget SetupItem(GLProfile o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => graphicSettings.GLProfile == o,
					() => graphicSettings.GLProfile = o);

				var label = o.ToString();
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			var profiles = new[] { GLProfile.Automatic }.Concat(Game.Renderer.SupportedGLProfiles);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, profiles, SetupItem);
		}

		static void ShowTargetLinesDropdown(DropDownButtonWidget dropdown, GameSettings gameSettings)
		{
			var options = new Dictionary<string, TargetLinesType>()
			{
				{ FluentProvider.GetMessage(Automatic), TargetLinesType.Automatic },
				{ FluentProvider.GetMessage(Manual), TargetLinesType.Manual },
				{ FluentProvider.GetMessage(Disabled), TargetLinesType.Disabled },
			};

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gameSettings.TargetLines == options[o],
					() => gameSettings.TargetLines = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}

		public static void ShowBattlefieldCameraDropdown(ModData modData, DropDownButtonWidget dropdown,
			WorldViewportSizes viewportSizes, GraphicSettings graphicSettings)
		{
			ScrollItemWidget SetupItem(WorldViewport o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => graphicSettings.ViewportDistance == o,
					() => graphicSettings.ViewportDistance = o);

				var label = GetViewportSizeName(modData, o);
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			var windowHeight = Game.Renderer.NativeResolution.Height;

			var validSizes = new List<WorldViewport>() { WorldViewport.Close };
			if (viewportSizes.GetSizeRange(WorldViewport.Medium).X < windowHeight)
				validSizes.Add(WorldViewport.Medium);

			var farRange = viewportSizes.GetSizeRange(WorldViewport.Far);
			if (farRange.X < windowHeight)
				validSizes.Add(WorldViewport.Far);

			if (viewportSizes.AllowNativeZoom && farRange.Y < windowHeight)
				validSizes.Add(WorldViewport.Native);

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validSizes, SetupItem);
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
				? new WidgetBounds(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
				: w.Parent.Bounds;

			var substitutions = new Dictionary<string, int>
			{
				{ "WINDOW_WIDTH", Game.Renderer.Resolution.Width },
				{ "WINDOW_HEIGHT", Game.Renderer.Resolution.Height },
				{ "PARENT_WIDTH", parentBounds.Width },
				{ "PARENT_HEIGHT", parentBounds.Height }
			};

			var readOnlySubstitutions = new ReadOnlyDictionary<string, int>(substitutions);
			var width = w.Width?.Evaluate(readOnlySubstitutions) ?? 0;
			var height = w.Height?.Evaluate(readOnlySubstitutions) ?? 0;

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			if (insideScrollPanel)
				w.Bounds = new WidgetBounds(w.Bounds.X, w.Bounds.Y, width, w.Bounds.Height);
			else
				w.Bounds = new WidgetBounds(
					w.X?.Evaluate(readOnlySubstitutions) ?? 0,
					w.Y?.Evaluate(readOnlySubstitutions) ?? 0,
					width,
					height);

			foreach (var c in w.Children)
				RecalculateWidgetLayout(c, insideScrollPanel || w is ScrollPanelWidget);
		}

		public static void ShowUIScaleDropdown(DropDownButtonWidget dropdown, GraphicSettings graphicSettings)
		{
			ScrollItemWidget SetupItem(float o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => graphicSettings.UIScale == o,
					() =>
					{
						Game.RunAfterTick(() =>
						{
							var oldScale = graphicSettings.UIScale;
							graphicSettings.UIScale = o;

							Game.Renderer.SetUIScale(o);
							RecalculateWidgetLayout(Ui.Root);
							Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / graphicSettings.UIScale).ToInt2();
						});
					});

				var label = $"{(int)(100 * o)}%";
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			}

			var viewportSizes = Game.ModData.GetOrCreate<WorldViewportSizes>();
			var maxScales = new float2(Game.Renderer.NativeResolution) / new float2(viewportSizes.MinEffectiveResolution);
			var maxScale = Math.Min(maxScales.X, maxScales.Y);

			var validScales = new[] { 1f, 1.25f, 1.5f, 1.75f, 2f }.Where(x => x <= maxScale);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validScales, SetupItem);
		}
	}
}
