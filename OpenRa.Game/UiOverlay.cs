using System.Drawing;
using System.Linq;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa
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
						if (Game.UnitInfluence.GetUnitsAt(new int2(i, j)).Any())
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), 0);
		}

		public void DrawBuildingGrid( string name, BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var isCloseEnough = Game.IsCloseEnoughToBase(Game.LocalPlayer, name, bi, position);

			foreach( var t in Footprint.Tiles( name, bi, position ) )
				spriteRenderer.DrawSprite( ( isCloseEnough && Game.IsCellBuildable( t, bi.WaterBound
					? UnitMovementType.Float : UnitMovementType.Wheel ) && !Rules.Map.ContainsResource( t ) )
					? buildOk : buildBlocked, Game.CellSize * t, 0 );

			spriteRenderer.Flush();
		}
	}
}
