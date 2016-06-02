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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface ICursor : IDisposable
	{
		void Render(Renderer renderer);
		void SetCursor(string cursor);
		void Tick();
	}

	public sealed class SoftwareCursor : ICursor
	{
		readonly HardwarePalette palette = new HardwarePalette();
		readonly Cache<string, PaletteReference> paletteReferences;
		readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
		readonly CursorProvider cursorProvider;
		readonly SheetBuilder sheetBuilder;

		public SoftwareCursor(CursorProvider cursorProvider)
		{
			this.cursorProvider = cursorProvider;

			paletteReferences = new Cache<string, PaletteReference>(CreatePaletteReference);
			foreach (var p in cursorProvider.Palettes)
				palette.AddPalette(p.Key, p.Value, false);

			palette.Initialize();

			sheetBuilder = new SheetBuilder(SheetType.Indexed);
			foreach (var kv in cursorProvider.Cursors)
			{
				var s = kv.Value.Frames.Select(a => sheetBuilder.Add(a)).ToArray();
				sprites.Add(kv.Key, s);
			}

			sheetBuilder.Current.ReleaseBuffer();

			Game.Renderer.Device.SetHardwareCursor(null);
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = palette.GetPalette(name);
			return new PaletteReference(name, palette.GetPaletteIndex(name), pal, palette);
		}

		string cursorName;
		public void SetCursor(string cursor)
		{
			cursorName = cursor;
		}

		float cursorFrame;
		public void Tick()
		{
			cursorFrame += 0.5f;
		}

		public void Render(Renderer renderer)
		{
			if (cursorName == null)
				return;

			var cursorSequence = cursorProvider.GetCursorSequence(cursorName);
			var cursorSprite = sprites[cursorName][((int)cursorFrame % cursorSequence.Length)];
			var cursorSize = CursorProvider.CursorViewportZoomed ? 2.0f * cursorSprite.Size : cursorSprite.Size;

			var cursorOffset = CursorProvider.CursorViewportZoomed ?
				(2 * cursorSequence.Hotspot) + cursorSprite.Size.XY.ToInt2() :
				cursorSequence.Hotspot + (0.5f * cursorSprite.Size.XY).ToInt2();

			renderer.SetPalette(palette);
			renderer.SpriteRenderer.DrawSprite(cursorSprite,
				Viewport.LastMousePos - cursorOffset,
				paletteReferences[cursorSequence.Palette],
				cursorSize);
		}

		public void Dispose()
		{
			palette.Dispose();
			sheetBuilder.Dispose();
		}
	}
}
