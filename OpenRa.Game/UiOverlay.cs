using System.Drawing;
using OpenRa.Game.Graphics;
using System;
using OpenRa.Game.GameRules;
using System.Linq;

namespace OpenRa.Game
{
	class UiOverlay
	{
		SpriteRenderer spriteRenderer;
		Sprite buildOk, buildBlocked, unitDebug;

		public static bool ShowUnitDebug = false;
		public static bool ShowBuildDebug = false;

		public UiOverlay(SpriteRenderer spriteRenderer)
		{
			this.spriteRenderer = spriteRenderer;

			buildOk = SynthesizeTile(0x80);
			buildBlocked = SynthesizeTile(0xe6);
			unitDebug = SynthesizeTile(0x7c);
		}

		static Sprite SynthesizeTile(byte paletteIndex)
		{
			byte[] data = new byte[Game.CellSize * Game.CellSize];

			for (int i = 0; i < Game.CellSize; i++)
				for (int j = 0; j < Game.CellSize; j++)
					data[i * Game.CellSize + j] = ((i + j) % 4 < 2) ? (byte)0 : paletteIndex;

			return SheetBuilder.Add( data, new Size(Game.CellSize,Game.CellSize) );
		}

		public void Draw()
		{
			if (ShowUnitDebug)
				for (var j = 0; j < 128; j++)
					for (var i = 0; i < 128; i++)
						if (Game.UnitInfluence.GetUnitAt(new int2(i, j)) != null)
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), 0);
		}

		public void DrawBuildingGrid( BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var isCloseEnough = Game.IsCloseEnoughToBase(Game.LocalPlayer, bi, position);

			foreach( var t in Footprint.Tiles( bi, position ) )
				spriteRenderer.DrawSprite( ( isCloseEnough && Game.IsCellBuildable( t, bi.WaterBound
					? UnitMovementType.Float : UnitMovementType.Wheel ) && !Rules.Map.ContainsResource( t ) )
					? buildOk : buildBlocked, Game.CellSize * t, 0 );

			spriteRenderer.Flush();
		}
	}
}
