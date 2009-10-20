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
		public IOrderGenerator orderGenerator;

        float2 dragStart, dragEnd;
		public void HandleMouseInput(MouseInput mi)
		{
            var xy = Game.viewport.ViewToWorld(mi);

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
            {
				if (!(orderGenerator is PlaceBuilding))
					dragStart = dragEnd = xy;

				if (orderGenerator != null)
					foreach (var order in orderGenerator.Order(xy.ToInt2()))
						order.Apply(true);
            }

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Up)
            {
				if (!(orderGenerator is PlaceBuilding))
				{
					if (dragStart != xy)
						orderGenerator = new UnitOrderGenerator( 
							Game.SelectUnitsInBox( Game.CellSize * dragStart, Game.CellSize * xy ) );
					else
						orderGenerator = new UnitOrderGenerator( 
							Game.SelectUnitOrBuilding( Game.CellSize * xy ) );
				}

				dragStart = dragEnd = xy;
            }

            if (mi.Button == MouseButtons.None && mi.Event == MouseInputEvent.Move)
            {
                /* update the cursor to reflect the thing under us - note this 
                 * needs to also happen when the *thing* changes, so per-frame hook */
				dragStart = dragEnd = xy;
            }

			if( mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down )
				if( orderGenerator != null )
					foreach( var order in orderGenerator.Order( xy.ToInt2() ) )
						order.Apply( false );
		}

        public Pair<float2, float2>? SelectionBox
        {
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(Game.CellSize * dragStart, Game.CellSize * dragEnd);
			}
        }

		public Cursor ChooseCursor()
		{
			var uog = orderGenerator as UnitOrderGenerator;

			if (uog != null && uog.selection.Count > 0 && uog.selection.Any(a => a.traits.Contains<Traits.Mobile>()))
				return Cursor.Move;

			if (Game.SelectUnitOrBuilding(Game.CellSize * dragEnd).Any())
				return Cursor.Select;
			
			return Cursor.Default;
		}
	}
}
