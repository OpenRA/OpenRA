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

			Func<int2, bool> passableAt = a =>
				{
					a += game.map.Offset;

					return game.map.IsInMap(a.X, a.Y) &&
					TerrainCosts.Cost(UnitMovementType.Wheel,
						game.terrain.tileSet.GetWalkability(game.map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
				};

			var footprint = Rules.Footprint.GetFootprint(name);
			var j = 0;
			foreach (var row in footprint)
			{
				var i = 0;
				foreach (var c in row)
				{
					if (c != '_')
						spriteRenderer.DrawSprite(passableAt(position + new int2(i, j)) ? buildOk : buildBlocked,
							24 * (position + new int2(i, j)), 0);
					++i;
				}
				++j;
			}

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
