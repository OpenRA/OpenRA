#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class AssetBrowserLogic
	{
		Widget panel;

		ShpImageWidget spriteImage;
		TextFieldWidget filenameInput;
		SliderWidget frameSlider;
		ButtonWidget playButton;
		ButtonWidget pauseButton;

		[ObjectCreator.UseCtor]
		public AssetBrowserLogic(Widget widget, Action onExit, World world)
		{
			panel = widget;

			spriteImage = panel.Get<ShpImageWidget>("SPRITE");

			filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
			filenameInput.Text = spriteImage.Image;
			filenameInput.OnEnterKey = () => LoadAsset(filenameInput.Text);

			var assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			var template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");

			assetList.RemoveChildren();
			foreach (var folder in FileSystem.FolderPaths)
			{
				if (Directory.Exists(folder))
				{
					var shps = Directory.GetFiles(folder, "*.shp");
					foreach (var shp in shps)
						AddAsset(assetList, shp, template);
				}
			}

			frameSlider = panel.Get<SliderWidget>("FRAME_SLIDER");
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			frameSlider.OnChange += x => { spriteImage.Frame = (int)Math.Round(x); };
			frameSlider.GetValue = () => spriteImage.Frame;

			panel.Get<LabelWidget>("FRAME_COUNT").GetText = () => spriteImage.Frame.ToString();

			playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = () =>
			{
				spriteImage.LoopAnimation = true;
				playButton.Visible = false;
				pauseButton.Visible = true;
			};
			pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = () =>
			{
				spriteImage.LoopAnimation = false;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_STOP").OnClick = () =>
			{
				spriteImage.LoopAnimation = false;
				frameSlider.Value = 0;
				spriteImage.Frame = 0;
				playButton.Visible = true;
				pauseButton.Visible = false;
			};

			panel.Get<ButtonWidget>("BUTTON_NEXT").OnClick = () => { spriteImage.RenderNextFrame(); };
			panel.Get<ButtonWidget>("BUTTON_PREV").OnClick = () => { spriteImage.RenderPreviousFrame(); };

			panel.Get<ButtonWidget>("LOAD_BUTTON").OnClick = () =>
			{
				LoadAsset(filenameInput.Text);
			};

			panel.Get<ButtonWidget>("CLOSE_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		void AddAsset(ScrollPanelWidget list, string filepath, ScrollItemWidget template)
		{
			var sprite = Path.GetFileNameWithoutExtension(filepath);

			var item = ScrollItemWidget.Setup(template,
			                                  () => spriteImage != null && spriteImage.Image == sprite,
			                                  () => LoadAsset(sprite));
			item.Get<LabelWidget>("TITLE").GetText = () => sprite;

			list.AddChild(item);
		}

		bool LoadAsset(string filename)
		{
			if (filename == null)
				return false;

			filenameInput.Text = filename;
			spriteImage.Frame = 0;
			spriteImage.Image = filename;
			frameSlider.MaximumValue = (float)spriteImage.FrameCount;
			frameSlider.Ticks = spriteImage.FrameCount+1;
			return true;
		}
	}
}
