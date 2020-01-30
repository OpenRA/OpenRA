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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class CursorManager
	{
		class Cursor
		{
			public string Name;
			public int2 PaddedSize;
			public Rectangle Bounds;

			public int Length;
			public Sprite[] Sprites;
			public IHardwareCursor[] Cursors;
		}

		readonly Dictionary<string, Cursor> cursors = new Dictionary<string, Cursor>();
		readonly SheetBuilder sheetBuilder;
		readonly GraphicSettings graphicSettings;

		Cursor cursor;
		bool isLocked = false;
		int2 lockedPosition;
		bool hardwareCursorsDisabled = false;
		bool hardwareCursorsDoubled = false;

		public CursorManager(CursorProvider cursorProvider)
		{
			hardwareCursorsDisabled = Game.Settings.Graphics.DisableHardwareCursors;

			graphicSettings = Game.Settings.Graphics;
			sheetBuilder = new SheetBuilder(SheetType.BGRA);
			foreach (var kv in cursorProvider.Cursors)
			{
				var frames = kv.Value.Frames;
				var palette = !string.IsNullOrEmpty(kv.Value.Palette) ? cursorProvider.Palettes[kv.Value.Palette] : null;

				var c = new Cursor
				{
					Name = kv.Key,
					Bounds = Rectangle.FromLTRB(0, 0, 1, 1),

					Length = 0,
					Sprites = new Sprite[frames.Length],
					Cursors = new IHardwareCursor[frames.Length]
				};

				// Hardware cursors have a number of odd platform-specific bugs/limitations.
				// Reduce the number of edge cases by padding the individual frames such that:
				// - the hotspot is inside the frame bounds (enforced by SDL)
				// - all frames within a sequence have the same size (needed for macOS 10.15)
				// - the frame size is a multiple of 8 (needed for Windows)
				foreach (var f in frames)
				{
					// Hotspot is specified relative to the center of the frame
					var hotspot = f.Offset.ToInt2() - kv.Value.Hotspot - new int2(f.Size) / 2;

					// SheetBuilder expects data in BGRA
					var data = FrameToBGRA(kv.Key, f, palette);
					c.Sprites[c.Length++] = sheetBuilder.Add(data, f.Size, 0, hotspot);

					// Bounds relative to the hotspot
					c.Bounds = Rectangle.Union(c.Bounds, new Rectangle(hotspot, f.Size));
				}

				// Pad bottom-right edge to make the frame size a multiple of 8
				c.PaddedSize = 8 * new int2((c.Bounds.Width + 7) / 8, (c.Bounds.Height + 7) / 8);

				cursors.Add(kv.Key, c);
			}

			CreateOrUpdateHardwareCursors();

			foreach (var s in sheetBuilder.AllSheets)
				s.ReleaseBuffer();

			Update();
		}

		void CreateOrUpdateHardwareCursors()
		{
			if (hardwareCursorsDisabled)
				return;

			// Dispose any existing cursors to avoid leaking native resources
			ClearHardwareCursors();

			try
			{
				foreach (var kv in cursors)
				{
					var template = kv.Value;
					for (var i = 0; i < template.Sprites.Length; i++)
					{
						if (template.Cursors[i] != null)
							template.Cursors[i].Dispose();

						// Calculate the padding to position the frame within sequenceBounds
						var paddingTL = -(template.Bounds.Location - template.Sprites[i].Offset.XY.ToInt2());
						var paddingBR = template.PaddedSize - new int2(template.Sprites[i].Bounds.Size) - paddingTL;

						template.Cursors[i] = CreateHardwareCursor(kv.Key, template.Sprites[i], paddingTL, paddingBR, -template.Bounds.Location);
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to initialize hardware cursors. Falling back to software cursors.");
				Log.Write("debug", "Error was: " + e.Message);

				Console.WriteLine("Failed to initialize hardware cursors. Falling back to software cursors.");
				Console.WriteLine("Error was: " + e.Message);

				ClearHardwareCursors();
			}

			hardwareCursorsDoubled = graphicSettings.CursorDouble;
		}

		public void SetCursor(string cursorName)
		{
			if ((cursorName == null && cursor == null) || (cursor != null && cursorName == cursor.Name))
				return;

			if (cursorName == null || !cursors.TryGetValue(cursorName, out cursor))
				cursor = null;

			Update();
		}

		int frame;
		int ticks;

		public void Tick()
		{
			if (hardwareCursorsDoubled != graphicSettings.CursorDouble)
			{
				CreateOrUpdateHardwareCursors();
				Update();
			}

			if (cursor == null || cursor.Cursors.Length == 1)
				return;

			if (++ticks > 2)
			{
				ticks -= 2;
				frame++;

				Update();
			}
		}

		void Update()
		{
			if (cursor != null && frame >= cursor.Cursors.Length)
				frame %= cursor.Cursors.Length;

			if (cursor == null || isLocked)
				Game.Renderer.Window.SetHardwareCursor(null);
			else
				Game.Renderer.Window.SetHardwareCursor(cursor.Cursors[frame]);
		}

		public void Render(Renderer renderer)
		{
			// Cursor is hidden
			if (cursor == null)
				return;

			// Hardware cursor is enabled
			if (!isLocked && cursor.Cursors[frame % cursor.Length] != null)
				return;

			// Render cursor in software
			var doubleCursor = graphicSettings.CursorDouble;
			var cursorSprite = cursor.Sprites[frame % cursor.Length];
			var cursorSize = doubleCursor ? 2.0f * cursorSprite.Size : cursorSprite.Size;

			// Cursor is rendered in native window coordinates
			// Apply same scaling rules as hardware cursors
			if (Game.Renderer.NativeWindowScale > 1.5f)
				cursorSize = 2 * cursorSize;

			var mousePos = isLocked ? lockedPosition : Viewport.LastMousePos;
			renderer.RgbaSpriteRenderer.DrawSprite(cursorSprite,
				mousePos,
				cursorSize / Game.Renderer.WindowScale);
		}

		public void Lock()
		{
			lockedPosition = Viewport.LastMousePos;
			Game.Renderer.Window.SetRelativeMouseMode(true);
			isLocked = true;
			Update();
		}

		public void Unlock()
		{
			Game.Renderer.Window.SetRelativeMouseMode(false);
			isLocked = false;
			Update();
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

		IHardwareCursor CreateHardwareCursor(string name, Sprite data, int2 paddingTL, int2 paddingBR, int2 hotspot)
		{
			var size = data.Bounds.Size;
			var srcStride = data.Sheet.Size.Width;
			var srcData = data.Sheet.GetData();
			var newWidth = paddingTL.X + size.Width + paddingBR.X;
			var newHeight = paddingTL.Y + size.Height + paddingBR.Y;
			var rgbaData = new byte[4 * newWidth * newHeight];

			for (var j = 0; j < size.Height; j++)
			{
				for (var i = 0; i < size.Width; i++)
				{
					var src = 4 * ((j + data.Bounds.Top) * srcStride + data.Bounds.Left + i);
					var dest = 4 * ((j + paddingTL.Y) * newWidth + i + paddingTL.X);
					Array.Copy(srcData, src, rgbaData, dest, 4);
				}
			}

			return Game.Renderer.Window.CreateHardwareCursor(name, new Size(newWidth, newHeight), rgbaData, hotspot, graphicSettings.CursorDouble);
		}

		void ClearHardwareCursors()
		{
			foreach (var c in cursors.Values)
			{
				for (var i = 0; i < c.Cursors.Length; i++)
				{
					if (c.Cursors[i] != null)
					{
						c.Cursors[i].Dispose();
						c.Cursors[i] = null;
					}
				}
			}
		}

		public void Dispose()
		{
			ClearHardwareCursors();

			cursors.Clear();
			sheetBuilder.Dispose();
		}
	}
}
