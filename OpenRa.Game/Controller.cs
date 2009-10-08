using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IjwFramework.Types;
using System.Drawing;

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
            var xy = GetWorldPos(mi);
            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
            {
                dragStart = dragEnd = xy;
            }

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Move)
                if (dragEnd != null)
                    dragEnd = GetWorldPos(mi);

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Up)
            {
                if (dragStart.HasValue && !(dragStart.Value == GetWorldPos(mi)))
                    orderGenerator = FindUnit(dragStart.Value, xy); /* band-box select */
                else
                    orderGenerator = FindUnit(xy, xy);  /* click select */

                dragStart = dragEnd = null;
            }

            if (mi.Button == MouseButtons.None && mi.Event == MouseInputEvent.Move)
            {
                /* update the cursor to reflect the thing under us - note this 
                 * needs to also happen when the *thing* changes, so per-frame hook */
            }

            if (mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down)
                if (orderGenerator != null)
                    orderGenerator.Order(game, new int2((int)xy.X, (int)xy.Y)).Apply(game);
		}

        public Unit FindUnit(float2 a, float2 b)
        {
            return FindUnits(game, 24 * a, 24 * b).FirstOrDefault();
        }

        public static IEnumerable<Unit> FindUnits(Game game, float2 a, float2 b)
        {
            var min = new float2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            var max = new float2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

            var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

            return game.world.Actors.OfType<Unit>()
                .Where(x => (x.owner == game.LocalPlayer) && (x.Bounds.IntersectsWith(rect)));
        }

        public Pair<float2, float2>? SelectionBox()
        {
            if (dragStart == null || dragEnd == null)
                return null;

            return Pair.New(24 * dragStart.Value, 24 * dragEnd.Value);
        }
	}
}
