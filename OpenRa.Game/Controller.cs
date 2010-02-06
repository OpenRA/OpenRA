using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Collections;
using IjwFramework.Types;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Orders;
using OpenRa.Traits;

namespace OpenRa
{
	public class Controller : IHandleInput
	{
		public IOrderGenerator orderGenerator;

		readonly Func<Modifiers> GetModifierKeys;

		public Controller(Func<Modifiers> getModifierKeys)
		{
			GetModifierKeys = getModifierKeys;
			CancelInputMode();
		}

		public void CancelInputMode()
		{
			orderGenerator = new UnitOrderGenerator(new Actor[] { });
		}

		public bool ToggleInputMode<T>() where T : IOrderGenerator, new()
		{
			if (orderGenerator is T)
			{
				CancelInputMode();
				return false;
			}
			else
			{
				orderGenerator = new T();
				return true;
			}
		}

		void ApplyOrders(World world, float2 xy, MouseInput mi)
		{
			if (orderGenerator == null) return;

			var orders = orderGenerator.Order(world, xy.ToInt2(), mi).ToArray();
			Game.orderManager.IssueOrders( orders );

			var voicedActor = orders.Select(o => o.Subject)
				.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.traits.Contains<Unit>());

			var isMove = orders.Any(o => o.OrderString == "Move");
			var isAttack = orders.Any( o => o.OrderString == "Attack" );

			if (voicedActor != null)
			{
				Sound.PlayVoice(isAttack ? "Attack" : "Move", voicedActor);

				if (isMove)
					world.Add(new Effects.MoveFlash(world, Game.CellSize * xy));
			}
		}

		float2 dragStart, dragEnd;
		public bool HandleInput(World world, MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorld(mi);

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!(orderGenerator is PlaceBuildingOrderGenerator))
					dragStart = dragEnd = xy;
				ApplyOrders(world, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (orderGenerator is UnitOrderGenerator)
				{
					var newSelection = world.SelectActorsInBox(Game.CellSize * dragStart, Game.CellSize * xy);
					CombineSelection(world, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
				}

				dragStart = dragEnd = xy;
			}

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
				ApplyOrders(world, xy, mi);

			return true;
		}

		void CombineSelection(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
		{
			var oldSelection = (orderGenerator is UnitOrderGenerator)
							   ? (orderGenerator as UnitOrderGenerator).selection : new Actor[] { }.AsEnumerable();

			if (isClick)
			{
				var adjNewSelection = newSelection.Take(1);	/* todo: select BEST, not FIRST */
				orderGenerator = new UnitOrderGenerator(isCombine
					? oldSelection.SymmetricDifference(adjNewSelection) : adjNewSelection);
			}
			else
				orderGenerator = new UnitOrderGenerator(isCombine
					? oldSelection.Union(newSelection) : newSelection);

			var voicedUnit = ((UnitOrderGenerator)orderGenerator).selection
				.Where(a => a.traits.Contains<Unit>()
					&& a.Owner == world.LocalPlayer)
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

		public string ChooseCursor( World world )
		{
			int sync = world.SyncHash();

			try
			{
				var mi = new MouseInput
				{
					Location = ( Game.CellSize * MousePosition - Game.viewport.Location ).ToInt2(),
					Button = MouseButton.Right,
					Modifiers = GetModifierKeys(),
				};

				return orderGenerator.GetCursor( world, MousePosition.ToInt2(), mi );
			}
			finally
			{
				if( sync != world.SyncHash() )
					throw new InvalidOperationException( "Desync in Controller.ChooseCursor" );
			}
		}

		Cache<int, List<Actor>> controlGroups = new Cache<int, List<Actor>>(_ => new List<Actor>());

		public void DoControlGroup(World world, int group, Modifiers mods)
		{
			var uog = orderGenerator as UnitOrderGenerator;
			if (uog == null) return;

			if (mods.HasModifier(Modifiers.Ctrl))
			{
				if (!uog.selection.Any())
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

			CombineSelection(world, controlGroups[group], mods.HasModifier(Modifiers.Shift), false);
		}

		public int? GetControlGroupForActor(Actor a)
		{
			return controlGroups.Where(g => g.Value.Contains(a))
				.Select(g => (int?)g.Key)
				.FirstOrDefault();
		}
	}
}
