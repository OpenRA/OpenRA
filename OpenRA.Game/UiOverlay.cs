#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public class UiOverlay
	{
		Sprite buildOk, buildBlocked, unitDebug;

		public UiOverlay()
		{
			buildOk = SynthesizeTile(0x0f);
			buildBlocked = SynthesizeTile(0x08);
			unitDebug = SynthesizeTile(0x04);
		}

		public static Sprite SynthesizeTile(byte paletteIndex)
		{
			byte[] data = new byte[Game.CellSize * Game.CellSize];

			for (int i = 0; i < Game.CellSize; i++)
				for (int j = 0; j < Game.CellSize; j++)
					data[i * Game.CellSize + j] = ((i + j) % 4 < 2) ? (byte)0 : paletteIndex;

			return Game.modData.SheetBuilder.Add(data, new Size(Game.CellSize, Game.CellSize));
		}

		public void Draw( WorldRenderer wr, World world )
		{
			if( world.LocalPlayer == null ) return;
			if (world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().UnitInfluenceDebug)
			{
				var uim = world.WorldActor.Trait<UnitInfluence>();
				
				for (var i = world.Map.Bounds.Left; i < world.Map.Bounds.Right; i++)
					for (var j = world.Map.Bounds.Top; j < world.Map.Bounds.Bottom; j++)	
						if (uim.GetUnitsAt(new int2(i, j)).Any())
							unitDebug.DrawAt(wr, Game.CellSize * new float2(i, j), "terrain");
			}
		}

		public void DrawGrid( WorldRenderer wr, Dictionary<int2, bool> cells )
		{
			foreach( var c in cells )
				( c.Value ? buildOk : buildBlocked ).DrawAt( wr, Game.CellSize * c.Key, "terrain" );
		}
	}
}
