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

		public void WorldClicked(object sender, MouseEventArgs e)
		{
			var xy = (1 / 24.0f) * (new float2(e.Location) + game.viewport.Location);
			if (orderGenerator != null)
				orderGenerator.Order(game, new int2((int)xy.X, (int)xy.Y)).Apply(game);
			// todo: route all orders through netcode
		}
	}
}
