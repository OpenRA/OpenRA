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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IntroductionPromptLogic : ChromeLogic
	{
		// Increment the version number when adding new stats
		const int IntroductionVersion = 2;

		[FluentReference]
		const string Classic = "options-control-scheme.classic";

		[FluentReference]
		const string Modern = "options-control-scheme.modern";

		[FluentReference]
		const string OtherRTS = "options-control-scheme.otherrts";

		public static bool ShouldShowPrompt()
		{
			return Game.Settings.Game.IntroductionPromptVersion < IntroductionVersion;
		}

		[ObjectCreator.UseCtor]
		public IntroductionPromptLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, Action onComplete)
		{
			var playerSettings = modData.GetSettings<PlayerSettings>();
			var graphicSettings = modData.GetSettings<GraphicSettings>();
			var gameSettings = modData.GetSettings<GameSettings>();

			var controlTypes = new Dictionary<MouseControlStyle, string>
			{
				{ MouseControlStyle.Classic, FluentProvider.GetMessage(Classic) },
				{ MouseControlStyle.Modern, FluentProvider.GetMessage(Modern) },
				{ MouseControlStyle.OtherRTS, FluentProvider.GetMessage(OtherRTS) },
			};

			if (gameSettings.IntroductionPromptVersion < 2)
			{
				// UseClassicMouseStyle boolean was replaced by MouseControlStyle enum to support additional control styles
				var classicMouseStyleNode = gameSettings.Yaml.NodeWithKeyOrDefault("UseClassicMouseStyle");
				if (classicMouseStyleNode != null)
				{
					var useClassicMouseStyle = FieldLoader.GetValue<bool>("UseClassicMouseStyle", classicMouseStyleNode.Value.Value);
					gameSettings.MouseControlStyle = useClassicMouseStyle ? MouseControlStyle.Classic : MouseControlStyle.Modern;
				}
			}

			var escPressed = false;
			var nameTextfield = widget.Get<TextFieldWidget>("PLAYERNAME");
			nameTextfield.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);

			var itchIntegration = modData.GetOrCreate<ItchIntegration>();
			itchIntegration.GetPlayerName(name => nameTextfield.Text = Settings.SanitizedPlayerName(name));

			nameTextfield.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				nameTextfield.Text = nameTextfield.Text.Trim();
				if (nameTextfield.Text.Length == 0)
					nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);
				else
				{
					nameTextfield.Text = Settings.SanitizedPlayerName(nameTextfield.Text);
					playerSettings.Name = nameTextfield.Text;
				}
			};

			nameTextfield.OnEnterKey = _ => { nameTextfield.YieldKeyboardFocus(); return true; };
			nameTextfield.OnEscKey = _ =>
			{
				nameTextfield.Text = Settings.SanitizedPlayerName(playerSettings.Name);
				escPressed = true;
				nameTextfield.YieldKeyboardFocus();
				return true;
			};

			var mouseControlDescClassic = widget.Get("MOUSE_CONTROL_DESC_CLASSIC");
			mouseControlDescClassic.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.Classic;

			var mouseControlDescModern = widget.Get("MOUSE_CONTROL_DESC_MODERN");
			mouseControlDescModern.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.Modern;

			var mouseControlDescOtherRTS = widget.Get("MOUSE_CONTROL_DESC_OTHERRTS");
			mouseControlDescOtherRTS.IsVisible = () => gameSettings.MouseControlStyle == MouseControlStyle.OtherRTS;

			var mouseControlDropdown = widget.Get<DropDownButtonWidget>("MOUSE_CONTROL_DROPDOWN");
			mouseControlDropdown.OnMouseDown = _ => InputSettingsLogic.ShowMouseControlDropdown(mouseControlDropdown, controlTypes, gameSettings);
			mouseControlDropdown.GetText = () => controlTypes[gameSettings.MouseControlStyle];

			foreach (var container in new[] { mouseControlDescClassic, mouseControlDescModern, mouseControlDescOtherRTS })
			{
				var classicScrollRight = container.Get("DESC_SCROLL_RIGHT");
				classicScrollRight.IsVisible = () => (gameSettings.MouseControlStyle == MouseControlStyle.Classic) ^ gameSettings.UseAlternateScrollButton;

				var classicScrollMiddle = container.Get("DESC_SCROLL_MIDDLE");
				classicScrollMiddle.IsVisible = () => (gameSettings.MouseControlStyle != MouseControlStyle.Classic) ^ gameSettings.UseAlternateScrollButton;

				var zoomDesc = container.Get("DESC_ZOOM");
				zoomDesc.IsVisible = () => gameSettings.ZoomModifier == Modifiers.None;

				var zoomDescModifier = container.Get<LabelWidget>("DESC_ZOOM_MODIFIER");
				zoomDescModifier.IsVisible = () => gameSettings.ZoomModifier != Modifiers.None;

				var zoomDescModifierTemplate = zoomDescModifier.GetText();
				var zoomDescModifierLabel = new CachedTransform<Modifiers, string>(
					mod => zoomDescModifierTemplate.Replace("MODIFIER", mod.ToString()));
				zoomDescModifier.GetText = () => zoomDescModifierLabel.Update(gameSettings.ZoomModifier);

				var edgescrollDesc = container.Get<LabelWidget>("DESC_EDGESCROLL");
				edgescrollDesc.IsVisible = () => gameSettings.ViewportEdgeScroll;
			}

			SettingsUtils.BindCheckboxPref(widget, "EDGESCROLL_CHECKBOX", gameSettings, "ViewportEdgeScroll");

			var colorManager = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();

			var colorDropdown = widget.Get<DropDownButtonWidget>("PLAYERCOLOR");
			colorDropdown.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			colorDropdown.OnMouseDown = _ => colorManager.ShowColorDropDown(colorDropdown, playerSettings.Color, null, worldRenderer, color =>
			{
				playerSettings.Color = color;
				Game.Settings.Save();
			});
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.Color;

			var viewportSizes = modData.GetOrCreate<WorldViewportSizes>();
			var battlefieldCameraDropDown = widget.Get<DropDownButtonWidget>("BATTLEFIELD_CAMERA_DROPDOWN");
			var battlefieldCameraLabel = new CachedTransform<WorldViewport, string>(vs => DisplaySettingsLogic.GetViewportSizeName(modData, vs));
			battlefieldCameraDropDown.OnMouseDown = _ => DisplaySettingsLogic.ShowBattlefieldCameraDropdown(
				modData, battlefieldCameraDropDown, viewportSizes, graphicSettings);
			battlefieldCameraDropDown.GetText = () => battlefieldCameraLabel.Update(graphicSettings.ViewportDistance);

			var uiScaleDropdown = widget.Get<DropDownButtonWidget>("UI_SCALE_DROPDOWN");
			var uiScaleLabel = new CachedTransform<float, string>(s => $"{(int)(100 * s)}%");
			uiScaleDropdown.OnMouseDown = _ => DisplaySettingsLogic.ShowUIScaleDropdown(uiScaleDropdown, graphicSettings);
			uiScaleDropdown.GetText = () => uiScaleLabel.Update(graphicSettings.UIScale);

			var minResolution = viewportSizes.MinEffectiveResolution;
			var resolution = Game.Renderer.Resolution;
			var disableUIScale = worldRenderer.World.Type != WorldType.Shellmap ||
				resolution.Width * graphicSettings.UIScale < 1.25f * minResolution.Width ||
				resolution.Height * graphicSettings.UIScale < 1.25f * minResolution.Height;

			uiScaleDropdown.IsDisabled = () => disableUIScale;

			SettingsUtils.BindCheckboxPref(widget, "CURSORDOUBLE_CHECKBOX", graphicSettings, "CursorDouble");

			widget.Get<ButtonWidget>("CONTINUE_BUTTON").OnClick = () =>
			{
				gameSettings.IntroductionPromptVersion = IntroductionVersion;
				Game.Settings.Save();
				Ui.CloseWindow();
				onComplete();
			};

			SettingsUtils.AdjustSettingsScrollPanelLayout(widget.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL"));
		}
	}
}
