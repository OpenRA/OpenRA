using System.Drawing;
using OpenRa.Game.Graphics;
using System;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class UiOverlay
	{
		SpriteRenderer spriteRenderer;
		Sprite buildOk;
		Sprite buildBlocked;

		public UiOverlay(SpriteRenderer spriteRenderer)
		{
			this.spriteRenderer = spriteRenderer;

			buildOk = SynthesizeTile(0x80);
			buildBlocked = SynthesizeTile(0xe6);
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
			if (!hasOverlay)
				return;

			foreach (var t in Footprint.Tiles(name,position))
				spriteRenderer.DrawSprite(Game.IsCellBuildable(t) ? buildOk : buildBlocked, Game.CellSize * t, 0);

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
