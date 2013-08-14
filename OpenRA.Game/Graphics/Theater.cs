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
using OpenRA.FileFormats.Graphics;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class Theater
	{
		SheetBuilder sheetBuilder;
		Dictionary<ushort, Sprite[]> templates;
		Sprite missingTile;

		Sprite[] LoadTemplate(string filename, string[] exts)
		{
			using (var s = FileSystem.OpenWithExts(filename, exts))
			{
				var t = new Terrain(s);
				return t.TileBitmapBytes
					.Select(b => b != null ? sheetBuilder.Add(b, new Size(t.Width, t.Height)) : null)
					.ToArray();
			}
		}

		public Theater(TileSet tileset)
		{
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow");
				allocated = true;

				// TODO: Use a fixed sheet size specified in the tileset yaml
				return SheetBuilder.AllocateSheet();
			};

			templates = new Dictionary<ushort, Sprite[]>();
			sheetBuilder = new SheetBuilder(SheetType.Indexed, allocate);
			foreach (var t in tileset.Templates)
				templates.Add(t.Value.Id, LoadTemplate(t.Value.Image, tileset.Extensions));

			// 1x1px transparent tile
			missingTile = sheetBuilder.Add(new byte[1], new Size(1, 1));
		}

		public Sprite TileSprite(TileReference<ushort, byte> r)
		{
			Sprite[] template;
			if (templates.TryGetValue(r.type, out template))
				if (template.Length > r.index && template[r.index] != null)
					return template[r.index];

			return missingTile;
		}

		public Sheet Sheet { get { return sheetBuilder.Current; } }
	}
}
