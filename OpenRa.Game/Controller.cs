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

        float2 GetWorldPos(MouseInput mi)
        {
            return (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + game.viewport.Location);
        }

        float2? dragStart, dragEnd;
		public void HandleMouseInput(MouseInput mi)
		{
            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
            {
                var xy = GetWorldPos(mi);

                if (orderGenerator != null)
                    orderGenerator.Order(game, new int2((int)xy.X, (int)xy.Y)).Apply(game);

                else { dragStart = dragEnd = xy; }
                // todo: route all orders through netcode
            }

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Move)
                if (dragEnd != null)
                    dragEnd = GetWorldPos(mi);

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Up)
                if (dragStart.HasValue && !(dragStart.Value == GetWorldPos(mi)))
                {
                    /* finalize drag selection */
                }
                else
                {
                    /* finalize click selection */
                }

            if (mi.Button == MouseButtons.None && mi.Event == MouseInputEvent.Move)
            {
                /* update the cursor to reflect the thing under us - note this 
                 * needs to also happen when the *thing* changes, so per-frame hook */
            }

            if (mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down)
                orderGenerator = null;
		}
	}
}
