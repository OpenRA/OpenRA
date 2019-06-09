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
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class HardwareCursor : ICursor
	{
		readonly Dictionary<string, IHardwareCursor[]> hardwareCursors = new Dictionary<string, IHardwareCursor[]>();
		readonly CursorProvider cursorProvider;
		readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
		readonly SheetBuilder sheetBuilder;
		readonly HardwarePalette hardwarePalette = new HardwarePalette();
		readonly Cache<string, PaletteReference> paletteReferences;

		CursorSequence cursor;
		bool isLocked = false;
		int2 lockedPosition;

		public HardwareCursor(CursorProvider cursorProvider)
		{
			this.cursorProvider = cursorProvider;

			paletteReferences = new Cache<string, PaletteReference>(CreatePaletteReference);
			foreach (var p in cursorProvider.Palettes)
				hardwarePalette.AddPalette(p.Key, p.Value, false);

			hardwarePalette.Initialize();

			sheetBuilder = new SheetBuilder(SheetType.Indexed);
			foreach (var kv in cursorProvider.Cursors)
			{
				var palette = cursorProvider.Palettes[kv.Value.Palette];
				var hc = kv.Value.Frames
					.Select(f => CreateCursor(f, palette, kv.Key, kv.Value))
					.ToArray();

				hardwareCursors.Add(kv.Key, hc);

				var s = kv.Value.Frames.Select(a => sheetBuilder.Add(a)).ToArray();
				sprites.Add(kv.Key, s);
			}

			sheetBuilder.Current.ReleaseBuffer();

			Update();
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = hardwarePalette.GetPalette(name);
			return new PaletteReference(name, hardwarePalette.GetPaletteIndex(name), pal, hardwarePalette);
		}

		IHardwareCursor CreateCursor(ISpriteFrame f, ImmutablePalette palette, string name, CursorSequence sequence)
		{
			var hotspot = sequence.Hotspot - f.Offset.ToInt2() + new int2(f.Size) / 2;

			// Expand the frame if required to include the hotspot
			var frameWidth = f.Size.Width;
			var dataWidth = f.Size.Width;
			var dataX = 0;
			if (hotspot.X < 0)
			{
				dataX = -hotspot.X;
				dataWidth += dataX;
				hotspot = hotspot.WithX(0);
			}
			else if (hotspot.X >= frameWidth)
				dataWidth = hotspot.X + 1;

			var frameHeight = f.Size.Height;
			var dataHeight = f.Size.Height;
			var dataY = 0;
			if (hotspot.Y < 0)
			{
				dataY = -hotspot.Y;
				dataHeight += dataY;
				hotspot = hotspot.WithY(0);
			}
			else if (hotspot.Y >= frameHeight)
				dataHeight = hotspot.Y + 1;

			var data = new byte[4 * dataWidth * dataHeight];
			for (var j = 0; j < frameHeight; j++)
			{
				for (var i = 0; i < frameWidth; i++)
				{
					var bytes = BitConverter.GetBytes(palette[f.Data[j * frameWidth + i]]);
					var start = 4 * ((j + dataY) * dataWidth + dataX + i);
					for (var k = 0; k < 4; k++)
						data[start + k] = bytes[k];
				}
			}

			return Game.Renderer.Window.CreateHardwareCursor(name, new Size(dataWidth, dataHeight), data, hotspot);
		}

		public void SetCursor(string cursorName)
		{
			if ((cursorName == null && cursor == null) || (cursor != null && cursorName == cursor.Name))
				return;

			if (cursorName == null || !cursorProvider.Cursors.TryGetValue(cursorName, out cursor))
				cursor = null;

			Update();
		}

		int frame;
		int ticks;

		public void Tick()
		{
			if (cursor == null || cursor.Length == 1)
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
			if (cursor != null && frame >= cursor.Length)
				frame %= cursor.Length;

			if (cursor == null || isLocked)
				Game.Renderer.Window.SetHardwareCursor(null);
			else
				Game.Renderer.Window.SetHardwareCursor(hardwareCursors[cursor.Name][frame]);
		}

		public void Render(Renderer renderer)
		{
			if (cursor.Name == null || !isLocked)
				return;

			var cursorSequence = cursorProvider.GetCursorSequence(cursor.Name);
			var cursorSprite = sprites[cursor.Name][frame];

			var cursorOffset = cursorSequence.Hotspot + (0.5f * cursorSprite.Size.XY).ToInt2();

			renderer.SetPalette(hardwarePalette);
			renderer.SpriteRenderer.DrawSprite(cursorSprite,
				lockedPosition - cursorOffset,
				paletteReferences[cursorSequence.Palette],
				cursorSprite.Size);
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

		public void Dispose()
		{
			foreach (var cursors in hardwareCursors)
				foreach (var cursor in cursors.Value)
					cursor.Dispose();

			sheetBuilder.Dispose();
			hardwareCursors.Clear();
		}
	}
}
