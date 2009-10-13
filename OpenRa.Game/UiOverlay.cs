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
		Game game;

		public UiOverlay(SpriteRenderer spriteRenderer, Game game)
		{
			this.spriteRenderer = spriteRenderer;
			this.game = game;

			buildOk = SynthesizeTile(0x80);
			buildBlocked = SynthesizeTile(0xe6);
		}

		static Sprite SynthesizeTile(byte paletteIndex)
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

			foreach (var t in Footprint.Tiles(name,position))
				spriteRenderer.DrawSprite(game.IsCellBuildable(t) ? buildOk : buildBlocked, 24 * t, 0);

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
