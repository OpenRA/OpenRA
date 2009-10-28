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

			if (!hasOverlay) return;

			var bi = (UnitInfo.BuildingInfo)Rules.UnitInfo[name];
			
			var maxDistance = bi.Adjacent + 2;	/* real-ra is weird. this is 1 GAP. */
			var tooFarFromBase = !Footprint.Tiles(bi, position).Any(
				t => Game.GetDistanceToBase(t, Game.LocalPlayer) < maxDistance);

			foreach( var t in Footprint.Tiles( bi, position ) )
				spriteRenderer.DrawSprite( !tooFarFromBase && Game.IsCellBuildable( t, bi.WaterBound 
					? UnitMovementType.Float : UnitMovementType.Wheel )
					? buildOk : buildBlocked, Game.CellSize * t, 0 );

			spriteRenderer.Flush();
		}

		bool hasOverlay;
		int2 position;
		string name;

		public void KillOverlay()
		{
			hasOverlay = false;
		}

		public void SetCurrentOverlay(int2 cell, string name)
		{
			hasOverlay = true;
			position = cell;
			this.name = name;
		}
	}
}
