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
				var frames = kv.Value.Frames;
				var palette = cursorProvider.Palettes[kv.Value.Palette];

				// Hardware cursors have a number of odd platform-specific bugs/limitations.
				// Reduce the number of edge cases by padding the individual frames such that:
				// - the hotspot is inside the frame bounds (enforced by SDL)
				// - all frames within a sequence have the same size (needed for macOS 10.15)
				// - the frame size is a multiple of 8 (needed for Windows)
				var sequenceBounds = Rectangle.FromLTRB(0, 0, 1, 1);
				var frameHotspots = new int2[frames.Length];
				for (var i = 0; i < frames.Length; i++)
				{
					// Hotspot relative to the center of the frame
					frameHotspots[i] = kv.Value.Hotspot - frames[i].Offset.ToInt2() + new int2(frames[i].Size) / 2;

					// Bounds relative to the hotspot
					sequenceBounds = Rectangle.Union(sequenceBounds, new Rectangle(-frameHotspots[i], frames[i].Size));
				}

				// Pad bottom-right edge to make the frame size a multiple of 8
				var paddedSize = 8 * new int2((sequenceBounds.Width + 7) / 8, (sequenceBounds.Height + 7) / 8);

				var cursors = new IHardwareCursor[frames.Length];
				var frameSprites = new Sprite[frames.Length];
				for (var i = 0; i < frames.Length; i++)
				{
					// Software rendering is used when the cursor is locked
					frameSprites[i] = sheetBuilder.Add(frames[i].Data, frames[i].Size, 0, frames[i].Offset);

					// Calculate the padding to position the frame within sequenceBounds
					var paddingTL = -(sequenceBounds.Location + frameHotspots[i]);
					var paddingBR = paddedSize - new int2(frames[i].Size) - paddingTL;
					cursors[i] = CreateCursor(kv.Key, frames[i], palette, paddingTL, paddingBR, -sequenceBounds.Location);
				}

				hardwareCursors.Add(kv.Key, cursors);
				sprites.Add(kv.Key, frameSprites);
			}

			sheetBuilder.Current.ReleaseBuffer();

			Update();
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = hardwarePalette.GetPalette(name);
			return new PaletteReference(name, hardwarePalette.GetPaletteIndex(name), pal, hardwarePalette);
		}

		IHardwareCursor CreateCursor(string name, ISpriteFrame frame, ImmutablePalette palette, int2 paddingTL, int2 paddingBR, int2 hotspot)
		{
			// Pad the cursor and convert to RBGA
			var newWidth = paddingTL.X + frame.Size.Width + paddingBR.X;
			var newHeight = paddingTL.Y + frame.Size.Height + paddingBR.Y;
			var rgbaData = new byte[4 * newWidth * newHeight];
			for (var j = 0; j < frame.Size.Height; j++)
			{
				for (var i = 0; i < frame.Size.Width; i++)
				{
					var bytes = BitConverter.GetBytes(palette[frame.Data[j * frame.Size.Width + i]]);
					var o = 4 * ((j + paddingTL.Y) * newWidth + i + paddingTL.X);
					for (var k = 0; k < 4; k++)
						rgbaData[o + k] = bytes[k];
				}
			}

			return Game.Renderer.Window.CreateHardwareCursor(name, new Size(newWidth, newHeight), rgbaData, hotspot);
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
