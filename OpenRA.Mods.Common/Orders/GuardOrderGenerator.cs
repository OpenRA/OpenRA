#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class GuardOrderGenerator : UnitOrderGenerator
	{
		readonly string orderName;
		readonly string cursor;
		readonly MouseButton expectedButton;
		IEnumerable<Actor> subjects;

		public GuardOrderGenerator(IEnumerable<Actor> subjects, string order, string cursor, MouseButton button)
		{
			orderName = order;
			this.cursor = cursor;
			expectedButton = button;
			this.subjects = subjects;
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != expectedButton)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			var target = FriendlyGuardableUnits(world, mi).FirstOrDefault();
			if (target == null)
				yield break;

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			if (!queued)
				world.CancelInputMode();

			yield return new Order(orderName, null, Target.FromActor(target), queued, null, subjects.Where(s => s != target).ToArray());
		}

		public override void SelectionChanged(World world, IEnumerable<Actor> selected)
		{
			// Guarding doesn't work without AutoTarget, so require at least one unit in the selection to have it
			subjects = selected.Where(s => !s.IsDead && s.Info.HasTraitInfo<GuardInfo>());
			if (!subjects.Any(s => s.Info.HasTraitInfo<AutoTargetInfo>()))
				world.CancelInputMode();
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!subjects.Any())
				return null;

			var multiple = subjects.Count() > 1;
			var canGuard = FriendlyGuardableUnits(world, mi)
				.Any(a => multiple || a != subjects.First());

			return canGuard ? cursor : "move-blocked";
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}

		public override bool ClearSelectionOnLeftClick => false;

		static IEnumerable<Actor> FriendlyGuardableUnits(World world, MouseInput mi)
		{
			return world.ScreenMap.ActorsAtMouse(mi)
				.Select(a => a.Actor)
				.Where(a => !a.IsDead &&
					a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) &&
					a.Info.HasTraitInfo<GuardableInfo>() &&
					!world.FogObscures(a));
		}
	}
}
