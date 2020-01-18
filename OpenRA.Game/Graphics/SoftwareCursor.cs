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
using System.Linq;

namespace OpenRA.Graphics
{
	public interface ICursor : IDisposable
	{
		void Render(Renderer renderer);
		void SetCursor(string cursor);
		void Tick();
		void Lock();
		void Unlock();
	}

	public sealed class SoftwareCursor : ICursor
	{
		readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
		readonly CursorProvider cursorProvider;
		SheetBuilder sheetBuilder;

		bool isLocked = false;
		int2 lockedPosition;

		public SoftwareCursor(CursorProvider cursorProvider)
		{
			this.cursorProvider = cursorProvider;

			sheetBuilder = new SheetBuilder(SheetType.BGRA, 1024);

			foreach (var kv in cursorProvider.Cursors)
			{
				var palette = !string.IsNullOrEmpty(kv.Value.Palette) ? cursorProvider.Palettes[kv.Value.Palette] : null;
				var s = kv.Value.Frames.Select(f => sheetBuilder.Add(FrameToBGRA(kv.Key, f, palette), f.Size, 0, f.Offset)).ToArray();

				sprites.Add(kv.Key, s);
			}

			sheetBuilder.Current.ReleaseBuffer();

			Game.Renderer.Window.SetHardwareCursor(null);
		}

		public static byte[] FrameToBGRA(string name, ISpriteFrame frame, ImmutablePalette palette)
		{
			// Data is already in BGRA format
			if (frame.Type == SpriteFrameType.BGRA)
				return frame.Data;

			// Cursors may be either native BGRA or Indexed.
			// Indexed sprites are converted to BGRA using the referenced palette.
			// All palettes must be explicitly referenced, even if they are embedded in the sprite.
			if (frame.Type == SpriteFrameType.Indexed && palette == null)
				throw new InvalidOperationException("Cursor sequence `{0}` attempted to load an indexed sprite but does not define Palette".F(name));

			var width = frame.Size.Width;
			var height = frame.Size.Height;
			var data = new byte[4 * width * height];
			for (var j = 0; j < height; j++)
			{
				for (var i = 0; i < width; i++)
				{
					var bytes = BitConverter.GetBytes(palette[frame.Data[j * width + i]]);
					var c = palette[frame.Data[j * width + i]];
					var k = 4 * (j * width + i);

					// Convert RGBA to BGRA
					data[k] = bytes[2];
					data[k + 1] = bytes[1];
					data[k + 2] = bytes[0];
					data[k + 3] = bytes[3];
				}
			}

			return data;
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

			var doubleCursor = cursorProvider.DoubleCursorSize && cursorName != "default";
			var cursorSequence = cursorProvider.GetCursorSequence(cursorName);
			var cursorSprite = sprites[cursorName][(int)cursorFrame % cursorSequence.Length];
			var cursorSize = doubleCursor ? 2.0f * cursorSprite.Size : cursorSprite.Size;

			var cursorOffset = doubleCursor ?
				(2 * cursorSequence.Hotspot) + cursorSprite.Size.XY.ToInt2() :
				cursorSequence.Hotspot + (0.5f * cursorSprite.Size.XY).ToInt2();

			var mousePos = isLocked ? lockedPosition : Viewport.LastMousePos;

			renderer.RgbaSpriteRenderer.DrawSprite(cursorSprite,
				mousePos - cursorOffset,
				cursorSize);
		}

		public void Lock()
		{
			Game.Renderer.Window.SetRelativeMouseMode(true);
			lockedPosition = Viewport.LastMousePos;
			isLocked = true;
		}

		public void Unlock()
		{
			Game.Renderer.Window.SetRelativeMouseMode(false);
			isLocked = false;
		}

		public void Dispose()
		{
			sheetBuilder.Dispose();
		}
	}
}
