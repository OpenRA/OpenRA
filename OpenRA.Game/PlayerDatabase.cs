#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using System.Net;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public class PlayerDatabase : IGlobalModData
	{
		public readonly string Profile = "https://forum.openra.net/openra/info/";
		public readonly int IconSize = 24;

		// 512x512 is large enough for 49 unique 72x72 badges
		// or 100 unique 42x42 badges
		// or 441 unique 24x24 badges
		// or some combination of the above if the DPI changes ingame
		[FieldLoader.Ignore]
		SheetBuilder sheetBuilder;

		[FieldLoader.Ignore]
		Cache<Pair<PlayerBadge, int>, Sprite> iconCache;

		Sprite LoadSprite(string url, int density)
		{
			var spriteSize = IconSize * density;
			var sprite = sheetBuilder.Allocate(new Size(spriteSize, spriteSize), 1f / density);

			Action<DownloadDataCompletedEventArgs> onComplete = i =>
			{
				if (i.Error != null)
					return;

				try
				{
					var icon = new Png(new MemoryStream(i.Result));
					if (icon.Width == spriteSize && icon.Height == spriteSize)
					{
						Game.RunAfterTick(() =>
						{
							Util.FastCopyIntoSprite(sprite, icon);
							sprite.Sheet.CommitBufferedData();
						});
					}
				}
				catch { }
			};

			new Download(url, _ => { }, onComplete);

			return sprite;
		}

		Sheet CreateSheet()
		{
			var sheet = new Sheet(SheetType.BGRA, new Size(512, 512));

			// We must manually force the buffer creation to avoid a crash
			// that is indirectly triggered by rendering from a Sheet that
			// has not yet been written to.
			sheet.CreateBuffer();
			sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;

			return sheet;
		}

		public PlayerBadge LoadBadge(MiniYaml yaml)
		{
			if (sheetBuilder == null)
			{
				sheetBuilder = new SheetBuilder(SheetType.BGRA, CreateSheet);

				iconCache = new Cache<Pair<PlayerBadge, int>, Sprite>(p =>
				{
					if (p.Second > 2 && !string.IsNullOrEmpty(p.First.Icon3x))
						return LoadSprite(p.First.Icon3x, 3);

					if (p.Second > 1 && !string.IsNullOrEmpty(p.First.Icon2x))
						return LoadSprite(p.First.Icon2x, 2);

					return LoadSprite(p.First.Icon, 1);
				});
			}

			var labelNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Label");
			var icon24Node = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon24");
			var icon48Node = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon48");
			var icon72Node = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon72");
			if (labelNode == null)
				return null;

			return new PlayerBadge(
				labelNode.Value.Value,
				icon24Node != null ? icon24Node.Value.Value : null,
				icon48Node != null ? icon48Node.Value.Value : null,
				icon72Node != null ? icon72Node.Value.Value : null);
		}

		public Sprite GetIcon(PlayerBadge badge)
		{
			var ws = Game.Renderer.WindowScale;
			var density = ws > 2 ? 3 : ws > 1 ? 2 : 1;
			return iconCache[Pair.New(badge, density)];
		}
	}
}
