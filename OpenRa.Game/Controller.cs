using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Collections;
using IjwFramework.Types;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class Controller : IHandleInput
	{
		public IOrderGenerator orderGenerator;

		readonly Func<Modifiers> GetModifierKeys;

		public Controller(Func<Modifiers> getModifierKeys)
		{
			GetModifierKeys = getModifierKeys;
		}

		List<Order> recentOrders = new List<Order>();

		void ApplyOrders(float2 xy, MouseInput mi)
		{
			if (orderGenerator == null) return;

			var orders = orderGenerator.Order(xy.ToInt2(), mi).ToArray();
			recentOrders.AddRange( orders );

			var voicedActor = orders.Select(o => o.Subject)
				.FirstOrDefault(a => a.Owner == Game.LocalPlayer && a.traits.Contains<Unit>());

			var isMove = orders.Any(o => o.OrderString == "Move");
			var isAttack = orders.Any( o => o.OrderString == "Attack" );

			if (voicedActor != null)
			{
				Sound.PlayVoice(isAttack ? "Attack" : "Move", voicedActor);

				if (isMove)
					Game.world.Add(new Effects.MoveFlash(Game.CellSize * xy));
			}
		}

		public void AddOrder(Order o) { recentOrders.Add(o); }

		public List<Order> GetRecentOrders( bool imm )
		{
			Func<Order, bool> p = o => o.IsImmediate ^ !imm;
			var result = recentOrders.Where(p).ToList();
			recentOrders.RemoveAll(o => p(o));		// ffs.
			return result;
		}

		float2 dragStart, dragEnd;
		public bool HandleInput(MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorld(mi);

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!(orderGenerator is PlaceBuilding))
					dragStart = dragEnd = xy;
				ApplyOrders(xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (!(orderGenerator is PlaceBuilding))
				{
					var newSelection = Game.SelectActorsInBox(Game.CellSize * dragStart, Game.CellSize * xy);
					CombineSelection(newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
				}

				dragStart = dragEnd = xy;
			}

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
				ApplyOrders(xy, mi);

			return true;
		}

		void CombineSelection(IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			var oldSelection = (orderGenerator is UnitOrderGenerator)
							   ? (orderGenerator as UnitOrderGenerator).selection : new Actor[] { }.AsEnumerable();

			if (isClick)
				orderGenerator = new UnitOrderGenerator(isCombine 
					? oldSelection.SymmetricDifference(newSelection) : newSelection);
			else
				orderGenerator = new UnitOrderGenerator(isCombine 
					? oldSelection.Union(newSelection) : newSelection);

			var voicedUnit = ((UnitOrderGenerator)orderGenerator).selection
				.Where(a => a.traits.Contains<Unit>()
					&& a.Owner == Game.LocalPlayer)
				.FirstOrDefault();

			Sound.PlayVoice("Select", voicedUnit);
		}

		public Pair<float2, float2>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(Game.CellSize * dragStart, Game.CellSize * dragEnd);
			}
		}

		public float2 MousePosition { get { return dragEnd; } }

		public Cursor ChooseCursor()
		{
			var mods = GetModifierKeys();
			var c = (orderGenerator is UnitOrderGenerator) ? orderGenerator.Order(dragEnd.ToInt2(),
				new MouseInput { Button = MouseButton.Right, Modifiers = mods })
				.Where(o => o.Validate())
				.Select(o => CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation))
				.FirstOrDefault(a => a != null) : null;

			return c ?? (Game.SelectActorsInBox(Game.CellSize * dragEnd, Game.CellSize * dragEnd).Any() ? Cursor.Select : Cursor.Default);
		}

		Cursor CursorForOrderString( string s, Actor a, int2 location )
		{
			var movement = a.traits.WithInterface<IMovement>().FirstOrDefault();
			switch( s )
			{
			case "Attack": return Cursor.Attack;
			case "Move":
				if (movement.CanEnterCell(location))
					return Cursor.Move;
				else
					return Cursor.MoveBlocked;
			case "DeployMcv":
				var factBuildingInfo = (BuildingInfo)Rules.UnitInfo[ "fact" ];
				if( Game.CanPlaceBuilding( factBuildingInfo, a.Location - new int2( 1, 1 ), a, false ) )
					return Cursor.Deploy;
				else
					return Cursor.DeployBlocked;
            case "ActivatePortableChronoshift": return Cursor.Deploy;
            case "UsePortableChronoshift":
				if (movement.CanEnterCell(location))
                    return Cursor.Chronoshift;
                else
                    return Cursor.MoveBlocked;
			case "Enter": return Cursor.Enter;
			case "Harvest": return Cursor.Attack; // TODO: special harvest cursor?
			default:
				return null;
			}
		}

		Cache<int, List<Actor>> controlGroups = new Cache<int, List<Actor>>(_ => new List<Actor>());

		public void DoControlGroup(int group, Modifiers mods)
		{
			var uog = orderGenerator as UnitOrderGenerator;
			if (mods.HasModifier(Modifiers.Ctrl))
			{
				if (uog == null || !uog.selection.Any())
					return;

				controlGroups[group].Clear();

				for (var i = 0; i < 10; i++)	/* all control groups */
					controlGroups[i].RemoveAll(a => uog.selection.Contains(a));

				controlGroups[group].AddRange(uog.selection);
				return;
			}

			if (mods.HasModifier(Modifiers.Alt))
			{
				Game.viewport.Center(controlGroups[group]);
				return;
			}

			if (uog == null) return;
			CombineSelection(controlGroups[group], mods.HasModifier(Modifiers.Shift), false);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}
	}
}
