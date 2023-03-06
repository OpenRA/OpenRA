#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class EmbeddedSpritePalette
	{
		readonly uint[] filePalette;
		readonly Dictionary<int, uint[]> framePalettes;

		public EmbeddedSpritePalette(uint[] filePalette = null, Dictionary<int, uint[]> framePalettes = null)
		{
			this.filePalette = filePalette;
			this.framePalettes = framePalettes;
		}

		public bool TryGetPaletteForFrame(int frame, out uint[] palette)
		{
			if (framePalettes == null || !framePalettes.TryGetValue(frame, out palette))
				palette = filePalette;

			return palette != null;
		}
	}
}
