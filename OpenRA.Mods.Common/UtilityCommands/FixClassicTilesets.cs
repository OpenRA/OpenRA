#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class FixClassicTilesets : IUtilityCommand
	{
		public string Name { get { return "--fix-classic-tilesets"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("EXTENSIONS", "Fixes missing template tile definitions and adds filename extensions.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var imageField = typeof(TerrainTemplateInfo).GetField("Image");
			var pickAnyField = typeof(TerrainTemplateInfo).GetField("PickAny");
			var tileInfoField = typeof(TerrainTemplateInfo).GetField("tileInfo", BindingFlags.NonPublic | BindingFlags.Instance);
			var terrainTypeField = typeof(TerrainTileInfo).GetField("TerrainType");
			var terrainLeftColorField = typeof(TerrainTileInfo).GetField("LeftColor");
			var terrainRightColorField = typeof(TerrainTileInfo).GetField("RightColor");
			var empty = new Size(0, 0);
			var single = new int2(1, 1);
			var exts = new[] { "" }.Concat(args[1].Split(','));

			foreach (var t in modData.Manifest.TileSets)
			{
				var ts = new TileSet(modData.DefaultFileSystem, t);
				var frameCache = new FrameCache(modData.DefaultFileSystem, modData.SpriteLoaders);

				Console.WriteLine("Tileset: " + ts.Name);
				foreach (var template in ts.Templates.Values)
				{
					// Find the sprite associated with this template
					foreach (var ext in exts)
					{
						Stream s;
						if (modData.DefaultFileSystem.TryOpen(template.Images[0] + ext, out s))
							s.Dispose();
						else
							continue;

						// Rewrite the template image (normally readonly) using reflection
						imageField.SetValue(template, template.Images[0] + ext);

						// Fetch the private tileInfo array so that we can write new entries
						var tileInfo = (TerrainTileInfo[])tileInfoField.GetValue(template);

						// Open the file and search for any implicit frames
						var allFrames = frameCache[template.Images[0]];
						var frames = template.Frames != null ? template.Frames.Select(f => allFrames[f]).ToArray() : allFrames;

						// Resize array for new entries
						if (frames.Length > template.TilesCount)
						{
							var ti = new TerrainTileInfo[frames.Length];
							Array.Copy(tileInfo, ti, template.TilesCount);
							tileInfoField.SetValue(template, ti);
							tileInfo = ti;
						}

						for (var i = 0; i < template.TilesCount; i++)
						{
							if (template[i] == null && frames[i] != null && frames[i].Size != empty)
							{
								tileInfo[i] = new TerrainTileInfo();
								var ti = ts.GetTerrainIndex("Clear");
								terrainTypeField.SetValue(tileInfo[i], ti);
								terrainLeftColorField.SetValue(tileInfo[i], ts[ti].Color);
								terrainRightColorField.SetValue(tileInfo[i], ts[ti].Color);
								Console.WriteLine("Fixing entry for {0}:{1}", template.Images[0], i);
							}
						}

						if (template.TilesCount > 1 && template.Size == single)
							pickAnyField.SetValue(template, true);
					}
				}

				ts.Save(t);
			}
		}
	}
}
