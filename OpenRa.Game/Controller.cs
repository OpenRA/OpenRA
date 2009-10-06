using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Controller
	{
		Game game;

		public IOrderGenerator orderGenerator;

		public Controller(Game game)
		{
			this.game = game;
		}

		public void HandleMouseInput(MouseInput mi)
		{
            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
            {
                var xy = (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + game.viewport.Location);

                if (orderGenerator != null)
                    orderGenerator.Order(game, new int2((int)xy.X, (int)xy.Y)).Apply(game);
                // todo: route all orders through netcode
            }

            if (mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down)
                orderGenerator = null;
		}
	}
}
