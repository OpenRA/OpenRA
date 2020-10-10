#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class GuardOrderGenerator : GenericSelectTarget
	{
		public GuardOrderGenerator(IEnumerable<Actor> subjects, string order, string cursor, MouseButton button)
			: base(subjects, order, cursor, button) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button != ExpectedButton)
				yield break;

			var target = FriendlyGuardableUnits(world, mi).FirstOrDefault();
			if (target == null)
				yield break;

			world.CancelInputMode();

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			yield return new Order(OrderName, null, Target.FromActor(target), queued, null, subjects.Where(s => s != target).ToArray());
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

			return canGuard ? Cursor : "move-blocked";
		}

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
