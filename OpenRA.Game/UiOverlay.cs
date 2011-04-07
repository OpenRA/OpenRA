#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA
{
	public class UiOverlay
	{
		Sprite buildOk, buildBlocked;

		public UiOverlay()
		{
			buildOk = SynthesizeTile(0x0f);
			buildBlocked = SynthesizeTile(0x08);
		}

		public static Sprite SynthesizeTile(byte paletteIndex)
		{
			byte[] data = new byte[Game.CellSize * Game.CellSize];

			for (int i = 0; i < Game.CellSize; i++)
				for (int j = 0; j < Game.CellSize; j++)
					data[i * Game.CellSize + j] = ((i+j) % 4 != 0) ? (byte)0 : paletteIndex;

			return Game.modData.SheetBuilder.Add(data, new Size(Game.CellSize, Game.CellSize));
		}

		public void DrawGrid( WorldRenderer wr, Dictionary<int2, bool> cells )
		{
			foreach( var c in cells )
				( c.Value ? buildOk : buildBlocked ).DrawAt( wr, Game.CellSize * c.Key, "terrain" );
		}
	}
}
