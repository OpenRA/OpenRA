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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class FontManager
	{
		readonly IPlatformWindow window;
		IReadOnlyDictionary<string, SpriteFont> fonts;

		SheetBuilder fontSheetBuilder;

		public FontManager(IPlatformWindow window)
		{
			this.window = window;
		}

		public void InitializeFonts(ModData modData)
		{
			if (fonts != null)
				foreach (var font in fonts.Values)
					font.Dispose();
			using (new PerfTimer("SpriteFonts"))
			{
				fontSheetBuilder?.Dispose();
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA, 512);
				fonts = modData.Manifest.Get<Fonts>().FontList.ToDictionary(x => x.Key,
					x => new SpriteFont(x.Value.Font, modData.DefaultFileSystem.Open(x.Value.Font).ReadAllBytes(),
						x.Value.Size, x.Value.Ascender, window.EffectiveWindowScale, fontSheetBuilder));
			}

			window.OnWindowScaleChanged += (oldNative, oldEffective, newNative, newEffective) =>
			{
				Game.RunAfterTick(() =>
				{
					ChromeProvider.SetDPIScale(newEffective);

					foreach (var f in fonts)
						f.Value.SetScale(newEffective);
				});
			};
		}

		public SpriteFont this[string key] => fonts[key];

		public bool TryGetValue(string font, out SpriteFont symbolFont)
		{
			return fonts.TryGetValue(font, out symbolFont);
		}

		public void Dispose()
		{
			fontSheetBuilder?.Dispose();
			if (fonts != null)
				foreach (var font in fonts.Values)
					font.Dispose();
		}
	}
}
