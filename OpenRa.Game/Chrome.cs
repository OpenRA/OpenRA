using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;
using System.Drawing;

namespace OpenRa.Game
{
	class Chrome
	{
		readonly Renderer renderer;
		readonly Sheet specialBin;
		readonly SpriteRenderer spriteRenderer;
		readonly Sprite specialBinSprite;
		readonly Sprite moneyBinSprite;

		public Chrome(Renderer r)
		{
			this.renderer = r;
			specialBin = new Sheet(renderer, "specialbin.png");
			spriteRenderer = new SpriteRenderer(renderer, true, renderer.RgbaSpriteShader);
			specialBinSprite = new Sprite(specialBin, new Rectangle(0, 0, 64, 256), TextureChannel.Alpha);
			moneyBinSprite = new Sprite(specialBin, new Rectangle(128, 0, 384, 64), TextureChannel.Alpha);
		}

		public void Draw()
		{
			renderer.Device.DisableScissor();
			renderer.DrawText(string.Format("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\n$ {4}\nPower {5}",
				Game.RenderFrame,
				Game.orderManager.FrameNumber,
				PerfHistory.items["render"].LastValue,
				PerfHistory.items["tick_time"].LastValue,
				Game.LocalPlayer.Cash,
				Game.LocalPlayer.GetTotalPower()
				), new int2(140, 5), Color.White);

			PerfHistory.Render(renderer, Game.worldRenderer.lineRenderer);

			spriteRenderer.DrawSprite(specialBinSprite, float2.Zero, 0);
			spriteRenderer.DrawSprite(moneyBinSprite, new float2( Game.viewport.Width - 384, 0 ), 0);
			spriteRenderer.Flush();
		}
	}
}
