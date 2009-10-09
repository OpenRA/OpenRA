using System.Drawing;
using OpenRa.Game.Graphics;
using System;

namespace OpenRa.Game
{
	class UiOverlay
	{
		SpriteRenderer spriteRenderer;
		Sprite buildOk;
		Sprite buildBlocked;
		Game game;

		public UiOverlay(SpriteRenderer spriteRenderer, Game game)
		{
			this.spriteRenderer = spriteRenderer;
			this.game = game;

			buildOk = SynthesizeTile(0x80);
			buildBlocked = SynthesizeTile(0xe6);
		}

		Sprite SynthesizeTile(byte paletteIndex)
		{
			byte[] data = new byte[24 * 24];

			for (int i = 0; i < 24; i++)
				for (int j = 0; j < 24; j++)
					data[i * 24 + j] = ((i + j) % 4 < 2) ? (byte)0 : paletteIndex;

			return SheetBuilder.Add( data, new Size(24,24) );
		}

		public void Draw()
		{
			if (!hasOverlay)
				return;

			Func<int, int, bool> passableAt = (x, y) =>
				{
					x += game.map.Offset.X;
					y += game.map.Offset.Y;

					return game.map.IsInMap(x, y) &&
					TerrainCosts.Cost(UnitMovementType.Wheel,
						game.terrain.tileSet.GetWalkability(game.map.MapTiles[x, y])) < double.PositiveInfinity;
				};

			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					spriteRenderer.DrawSprite(passableAt(position.X + i, position.Y + j) ? buildOk : buildBlocked,
						24 * (position + new int2(i, j)) + game.viewport.Location, 0);

			spriteRenderer.Flush();
		}

		bool hasOverlay;
		int2 position;
		int width, height;

		public void KillOverlay()
		{
			hasOverlay = false;
		}

		public void SetCurrentOverlay(int2 cell, int width, int height)
		{
			hasOverlay = true;
			position = cell;
			this.width = width;
			this.height = height;
		}
	}
}
