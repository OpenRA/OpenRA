#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
			if (target == null || Subjects.All(s => s.IsDead))
				yield break;

			world.CancelInputMode();
			foreach (var subject in Subjects)
				if (subject != target)
					yield return new Order(OrderName, subject, false) { TargetActor = target };
		}

		public override void Tick(World world)
		{
			if (Subjects.All(s => s.IsDead || !s.Info.HasTraitInfo<GuardInfo>()))
				world.CancelInputMode();
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!Subjects.Any())
				return null;

			var multiple = Subjects.Count() > 1;
			var canGuard = FriendlyGuardableUnits(world, mi)
				.Any(a => multiple || a != Subjects.First());

			return canGuard ? Cursor : "move-blocked";
		}

		static IEnumerable<Actor> FriendlyGuardableUnits(World world, MouseInput mi)
		{
			return world.ScreenMap.ActorsAt(mi)
				.Where(a => !world.FogObscures(a) && !a.IsDead &&
					a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) &&
					a.Info.HasTraitInfo<GuardableInfo>());
		}
	}
}
