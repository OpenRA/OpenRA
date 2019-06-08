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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class ModContentLoadScreen : ILoadScreen
	{
		Sprite sprite;
		Rectangle bounds;

		public void Init(ModData modData, Dictionary<string, string> info)
		{
			var res = Game.Renderer.Resolution;
			bounds = new Rectangle(0, 0, res.Width, res.Height);

			using (var stream = modData.DefaultFileSystem.Open(info["Image"]))
			{
				var sheet = new Sheet(SheetType.BGRA, stream);
				sprite = new Sprite(sheet, new Rectangle(0, 0, 1024, 480), TextureChannel.RGBA);
			}
		}

		public void Display()
		{
			var r = Game.Renderer;
			if (r == null)
				return;

			r.BeginFrame(int2.Zero, 1f);
			WidgetUtils.FillRectWithSprite(bounds, sprite);
			r.EndFrame(new NullInputHandler());
		}

		public void StartGame(Arguments args)
		{
			var modId = args.GetValue("Content.Mod", null);
			Manifest selectedMod;
			if (modId == null || !Game.Mods.TryGetValue(modId, out selectedMod))
				throw new InvalidOperationException("Invalid or missing Content.Mod argument.");

			var content = selectedMod.Get<ModContent>(Game.ModData.ObjectCreator);

			Ui.LoadWidget("MODCONTENT_BACKGROUND", Ui.Root, new WidgetArgs());

			if (!IsModInstalled(content))
			{
				var widgetArgs = new WidgetArgs
				{
					{ "continueLoading", () => Game.RunAfterTick(() => Game.InitializeMod(modId, new Arguments())) },
					{ "mod", selectedMod },
					{ "content", content },
				};

				Ui.OpenWindow("CONTENT_PROMPT_PANEL", widgetArgs);
			}
			else
			{
				var widgetArgs = new WidgetArgs
				{
					{ "mod", selectedMod },
					{ "content", content },
					{ "onCancel", () => Game.RunAfterTick(() => Game.InitializeMod(modId, new Arguments())) }
				};

				Ui.OpenWindow("CONTENT_PANEL", widgetArgs);
			}
		}

		bool IsModInstalled(ModContent content)
		{
			return content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));
		}

		public void Dispose()
		{
			if (sprite != null)
				sprite.Sheet.Dispose();
		}

		public bool BeforeLoad()
		{
			return true;
		}
	}
}
