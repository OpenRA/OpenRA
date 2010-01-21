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

		public void Draw( World world )
		{
			if (ShowUnitDebug)
				for (var j = 0; j < 128; j++)
					for (var i = 0; i < 128; i++)
						if (world.UnitInfluence.GetUnitsAt(new int2(i, j)).Any())
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), 0);
		}

		public void DrawBuildingGrid( World world, string name, BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var topLeft = position - Footprint.AdjustForBuildingSize( bi );
			var isCloseEnough = world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, topLeft);

			foreach( var t in Footprint.Tiles( name, bi, topLeft ) )
				spriteRenderer.DrawSprite( ( isCloseEnough && world.IsCellBuildable( t, bi.WaterBound
					? UnitMovementType.Float : UnitMovementType.Wheel ) && !world.Map.ContainsResource( t ) )
					? buildOk : buildBlocked, Game.CellSize * t, 0 );

			spriteRenderer.Flush();
		}
	}
}
