#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public sealed class IconManager
	{
		readonly Sprite icon;
		readonly SheetBuilder sheetBuilder;

		public IconManager(ModData modData)
		{
			if (Platform.CurrentPlatform != PlatformType.Linux)
				return;

			var metadata = modData.Manifest.Metadata;
			if (string.IsNullOrEmpty(metadata.WindowIcon))
				return;

			sheetBuilder = new SheetBuilder(SheetType.BGRA);
			var fileSystem = modData.DefaultFileSystem;
			using (var stream = fileSystem.Open(metadata.WindowIcon))
				icon = sheetBuilder.Add(new Png(stream));

			var size = icon.Bounds.Size;
			var srcStride = icon.Sheet.Size.Width;
			var srcData = icon.Sheet.GetData();
			var rgbaData = new byte[4 * size.Width * size.Height];

			for (var j = 0; j < size.Height; j++)
			{
				for (var i = 0; i < size.Width; i++)
				{
					var src = 4 * (j * srcStride + i);
					var dest = 4 * (j * size.Width + i);
					Array.Copy(srcData, src, rgbaData, dest, 4);
				}
			}

			Game.Renderer.Window.SetWindowIcon(size, rgbaData);

			foreach (var s in sheetBuilder.AllSheets)
				s.ReleaseBuffer();
		}

		public void Dispose()
		{
			sheetBuilder.Dispose();
		}
	}
}
