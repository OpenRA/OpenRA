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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class Theater
	{
		SheetBuilder sheetBuilder;
		Dictionary<ushort, Sprite[]> templates;
		Sprite missingTile;

		Sprite[] LoadTemplate(string filename, string[] exts, Cache<string, ISpriteFrame[]> r8Cache, int[] frames)
		{
			if (exts.Contains(".R8") && FileSystem.Exists(filename+".R8"))
			{
				return frames.Select(f =>
				{
					if (f < 0)
						return null;

					var image = r8Cache[filename][f];
					return sheetBuilder.Add(image.Data, new Size(image.Size.Width, image.Size.Height));
				}).ToArray();
			}

			using (var s = FileSystem.OpenWithExts(filename, exts))
			{
				var type = SpriteSource.DetectSpriteType(s);
				ISpriteSource source;
				switch (type)
				{
					case SpriteType.TmpTD:
						source = new TmpTDReader(s);
						break;
					case SpriteType.TmpRA:
						source = new TmpRAReader(s);
						break;
					default:
						throw new InvalidDataException(filename + " is not a valid terrain tile");
				}

				return source.Frames.Select(f => sheetBuilder.Add(f)).ToArray();
			}
		}

		public Theater(TileSet tileset)
		{
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
				allocated = true;

				return new Sheet(new Size(tileset.SheetSize, tileset.SheetSize));
			};

			var r8Cache = new Cache<string, ISpriteFrame[]>(s => new R8Reader(FileSystem.OpenWithExts(s, ".R8")).Frames.ToArray());
			templates = new Dictionary<ushort, Sprite[]>();
			sheetBuilder = new SheetBuilder(SheetType.Indexed, allocate);
			foreach (var t in tileset.Templates)
				templates.Add(t.Value.Id, LoadTemplate(t.Value.Image, tileset.Extensions, r8Cache, t.Value.Frames));

			// 1x1px transparent tile
			missingTile = sheetBuilder.Add(new byte[1], new Size(1, 1));
		}

		public Sprite TileSprite(TileReference<ushort, byte> r)
		{
			Sprite[] template;
			if (!templates.TryGetValue(r.Type, out template))
				return missingTile;

			if (r.Index >= template.Length)
				return missingTile;

			return template[r.Index];
		}

		public Sheet Sheet { get { return sheetBuilder.Current; } }
	}
}
