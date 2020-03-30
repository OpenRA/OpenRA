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
using System.Net;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA
{
	public class PlayerDatabase : IGlobalModData
	{
		public readonly string Profile = "https://forum.openra.net/openra/info/";

		[FieldLoader.Ignore]
		readonly object syncObject = new object();

		[FieldLoader.Ignore]
		readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

		// 128x128 is large enough for 25 unique 24x24 sprites
		[FieldLoader.Ignore]
		SheetBuilder sheetBuilder;

		public PlayerBadge LoadBadge(MiniYaml yaml)
		{
			if (sheetBuilder == null)
			{
				sheetBuilder = new SheetBuilder(SheetType.BGRA, 128);

				// We must manually force the buffer creation to avoid a crash
				// that is indirectly triggered by rendering from a Sheet that
				// has not yet been written to.
				sheetBuilder.Current.CreateBuffer();
			}

			var labelNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Label");
			var icon24Node = yaml.Nodes.FirstOrDefault(n => n.Key == "Icon24");
			if (labelNode == null || icon24Node == null)
				return null;

			Sprite sprite;
			lock (syncObject)
			{
				if (!spriteCache.TryGetValue(icon24Node.Value.Value, out sprite))
				{
					sprite = spriteCache[icon24Node.Value.Value] = sheetBuilder.Allocate(new Size(24, 24));

					Action<DownloadDataCompletedEventArgs> onComplete = i =>
					{
						if (i.Error != null)
							return;

						try
						{
							var icon = new Png(new MemoryStream(i.Result));
							if (icon.Width == 24 && icon.Height == 24)
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

					new Download(icon24Node.Value.Value, _ => { }, onComplete);
				}
			}

			return new PlayerBadge(labelNode.Value.Value, sprite);
		}
	}
}
