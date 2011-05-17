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
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncSettingsLogic : IWidgetDelegate
	{	
		enum PanelType
		{
			General,
			Input
		}
		PanelType Settings = PanelType.General;
		ColorRamp playerColor;
		Modifiers groupAddModifier;
		MouseScrollType mouseScroll;
		WindowMode windowMode;

		CncColorPickerPaletteModifier playerPalettePreview;
		World world;
		
		[ObjectCreator.UseCtor]
		public CncSettingsLogic([ObjectCreator.Param] Widget widget,
		                        [ObjectCreator.Param] World world,
		                        [ObjectCreator.Param] Action onExit)
		{
			this.world = world;
			var panel = widget.GetWidget("SETTINGS_PANEL");
					
			// General pane
			var generalButton = panel.GetWidget<ButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => Settings = PanelType.General;
			generalButton.IsDisabled = () => Settings == PanelType.General;
			
			var generalPane = panel.GetWidget("GENERAL_CONTROLS");
			generalPane.IsVisible = () => Settings == PanelType.General;
			
			// Player profile
			var nameTextfield = generalPane.GetWidget<TextFieldWidget>("NAME_TEXTFIELD");
			nameTextfield.Text = Game.Settings.Player.Name;
			
			playerColor = Game.Settings.Player.ColorRamp;
			playerPalettePreview = world.WorldActor.Trait<CncColorPickerPaletteModifier>();
			playerPalettePreview.Ramp = playerColor;
			
			var colorDropdown = generalPane.GetWidget<CncDropDownButtonWidget>("COLOR_DROPDOWN");
			colorDropdown.OnClick = () => ShowColorPicker(colorDropdown);
			colorDropdown.GetWidget<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerColor.GetColor(0);
			
			// Debug
			var perftext = Game.Settings.Debug.PerfText;
			var perftextCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => perftext;
			perftextCheckbox.OnClick = () => perftext ^= true;
			
			var perfgraph = Game.Settings.Debug.PerfGraph;
			var perfgraphCheckbox = generalPane.GetWidget<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => perfgraph;
			perfgraphCheckbox.OnClick = () => perfgraph ^= true;
			
			var matchtimer = Game.Settings.Game.MatchTimer;
			var matchtimerCheckbox = generalPane.GetWidget<CheckboxWidget>("MATCHTIME_CHECKBOX");
			matchtimerCheckbox.IsChecked = () => matchtimer;
			matchtimerCheckbox.OnClick = () => matchtimer ^= true;
			
			var checkunsynced = Game.Settings.Debug.SanityCheckUnsyncedCode;
			var checkunsyncedCheckbox = generalPane.GetWidget<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => checkunsynced;
			checkunsyncedCheckbox.OnClick = () => checkunsynced ^= true;
			
			// Video
			windowMode = Game.Settings.Graphics.Mode;
			var windowModeDropdown = generalPane.GetWidget<CncDropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseUp = _ => ShowWindowModeDropdown(windowModeDropdown);
			windowModeDropdown.GetText = () => windowMode == WindowMode.Windowed ? "Windowed" : windowMode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";
			
			generalPane.GetWidget("WINDOW_RESOLUTION").IsVisible = () => windowMode == WindowMode.Windowed;
			var windowWidth = generalPane.GetWidget<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = Game.Settings.Graphics.WindowedSize.X.ToString();
			
			var windowHeight = generalPane.GetWidget<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = Game.Settings.Graphics.WindowedSize.Y.ToString();

			// Audio
			var soundVolume = Game.Settings.Sound.SoundVolume;
			var soundSlider = generalPane.GetWidget<SliderWidget>("SOUND_SLIDER");
			soundSlider.OnChange += x => { soundVolume = x; Sound.SoundVolume = x;};
			soundSlider.GetOffset = () => { return soundVolume; };
			soundSlider.SetOffset(soundVolume);
			
			var musicVolume = Game.Settings.Sound.MusicVolume;
			var musicSlider = generalPane.GetWidget<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => { musicVolume = x; Sound.MusicVolume = x; };
			musicSlider.GetOffset = () => { return musicVolume; };
			musicSlider.SetOffset(musicVolume);
			
			var shellmapMusic = Game.Settings.Game.ShellmapMusic;
			var shellmapMusicCheckbox = generalPane.GetWidget<CheckboxWidget>("SHELLMAP_MUSIC");
			shellmapMusicCheckbox.IsChecked = () => shellmapMusic;
			shellmapMusicCheckbox.OnClick = () => shellmapMusic ^= true;
			
			
			// Input pane
			var inputPane = panel.GetWidget("INPUT_CONTROLS");
			inputPane.IsVisible = () => Settings == PanelType.Input;

			var inputButton = panel.GetWidget<ButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => Settings = PanelType.Input;
			inputButton.IsDisabled = () => Settings == PanelType.Input;
				
			inputPane.GetWidget<CheckboxWidget>("CLASSICORDERS_CHECKBOX").IsDisabled = () => true;
			
			var scrollStrength = Game.Settings.Game.ViewportEdgeScrollStep;
			var scrollSlider = inputPane.GetWidget<SliderWidget>("SCROLLSPEED_SLIDER");
			scrollSlider.OnChange += x => scrollStrength = scrollSlider.GetOffset();
			scrollSlider.SetOffset(scrollStrength);
			
			var edgescroll = Game.Settings.Game.ViewportEdgeScroll;
			var edgescrollCheckbox = inputPane.GetWidget<CheckboxWidget>("EDGESCROLL_CHECKBOX");
			edgescrollCheckbox.IsChecked = () => edgescroll;
			edgescrollCheckbox.OnClick = () => edgescroll ^= true;
			
			mouseScroll = Game.Settings.Game.MouseScroll;
			var mouseScrollDropdown = inputPane.GetWidget<CncDropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnClick = () => ShowMouseScrollDropdown(mouseScrollDropdown);
			mouseScrollDropdown.GetText = () => mouseScroll.ToString();
			
			var teamchat = Game.Settings.Game.TeamChatToggle;
			var teamchatCheckbox = inputPane.GetWidget<CheckboxWidget>("TEAMCHAT_CHECKBOX");
			teamchatCheckbox.IsChecked = () => teamchat;
			teamchatCheckbox.OnClick = () => teamchat ^= true;
			
			groupAddModifier = Game.Settings.Keyboard.ControlGroupModifier;
			var groupModifierDropdown = inputPane.GetWidget<CncDropDownButtonWidget>("GROUPADD_MODIFIER");
			groupModifierDropdown.OnClick = () => ShowGroupModifierDropdown(groupModifierDropdown);
			groupModifierDropdown.GetText = () => groupAddModifier.ToString();
			
			
			panel.GetWidget<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Widget.CloseWindow();
				onExit();
			};
			
			panel.GetWidget<ButtonWidget>("SAVE_BUTTON").OnClick = () =>
			{
				var s = Game.Settings;
				s.Player.Name = nameTextfield.Text;
				s.Player.ColorRamp = playerColor;
				
				s.Debug.PerfText = perftext;
				s.Debug.PerfGraph = perfgraph;
				s.Game.MatchTimer = matchtimer;
				s.Debug.SanityCheckUnsyncedCode = checkunsynced;
				
				s.Graphics.Mode = windowMode;
				
				int x = s.Graphics.WindowedSize.X, y = s.Graphics.WindowedSize.Y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				s.Graphics.WindowedSize = new int2(x,y);
				
				s.Sound.SoundVolume = soundVolume;
				Sound.SoundVolume = soundVolume;
				s.Sound.MusicVolume = musicVolume;
				Sound.MusicVolume = musicVolume;
				s.Game.ShellmapMusic = shellmapMusic;
				
				
				s.Game.ViewportEdgeScrollStep = scrollStrength;
				s.Game.ViewportEdgeScroll = edgescroll;
				s.Game.MouseScroll = mouseScroll;
				
				s.Game.TeamChatToggle = teamchat;
				s.Keyboard.ControlGroupModifier = groupAddModifier;
				s.Save();
				Widget.CloseWindow();
				onExit();
			};
		}
		
		void ShowColorPicker(CncDropDownButtonWidget color)
		{
			Action<ColorRamp> onSelect = c =>
			{
				playerColor = c;
				color.RemovePanel();
			};
			
			Action<ColorRamp> onChange = c =>
			{
				playerPalettePreview.Ramp = c;
			};
			
			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", playerColor }
			});
			
			color.AttachPanel(colorChooser);
		}
		
		void ShowGroupModifierDropdown(CncDropDownButtonWidget dropdown)
		{
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", dropdown.Bounds.Width }};
			
			var panel = (ScrollPanelWidget)Game.LoadWidget(world, "LABEL_DROPDOWN_TEMPLATE", null, new WidgetArgs()
			{
				{ "substitutions", substitutions }
			});

			var itemTemplate = panel.GetWidget<ScrollItemWidget>("TEMPLATE");
			var options = new List<Pair<string, Modifiers>>()
			{
				Pair.New("Ctrl", Modifiers.Ctrl),
				Pair.New("Alt", Modifiers.Alt),
				Pair.New("Shift", Modifiers.Shift),
				// TODO: Display this as Cmd on osx once we have platform detection
				Pair.New("Meta", Modifiers.Meta)
			};

			foreach (var o in options)
			{
				var key = o;
				var item = ScrollItemWidget.Setup(itemTemplate, () => groupAddModifier == key.Second, () => { groupAddModifier = key.Second; dropdown.RemovePanel(); });
				item.GetWidget<LabelWidget>("LABEL").GetText = () => key.First;
				panel.AddChild(item);
			}
			panel.Bounds.Height = panel.ContentHeight;
			dropdown.AttachPanel(panel);
		}
		
		bool ShowWindowModeDropdown(Widget dropdown)
		{
			var dropDownOptions = new List<Pair<string, Action>>()
			{
				Pair.New("Pseudo-Fullscreen", new Action(() => windowMode = WindowMode.PseudoFullscreen)),
				Pair.New("Fullscreen", new Action(() => windowMode = WindowMode.Fullscreen)),
				Pair.New("Windowed", new Action(() => windowMode = WindowMode.Windowed)),
			};
			
			CncDropDownButtonWidget.ShowDropDown(dropdown,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = ac.First,
					Align = LabelWidget.TextAlign.Center,
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
		
		
		bool ShowMouseScrollDropdown(Widget dropdown)
		{
			var dropDownOptions = new List<Pair<string, Action>>()
			{
				Pair.New("Disabled", new Action(() => mouseScroll = MouseScrollType.Disabled)),
				Pair.New("Standard", new Action(() => mouseScroll = MouseScrollType.Standard)),
				Pair.New("Inverted", new Action(() => mouseScroll = MouseScrollType.Inverted)),
			};
			
			CncDropDownButtonWidget.ShowDropDown(dropdown,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = ac.First,
					Align = LabelWidget.TextAlign.Center,
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
	}
}
