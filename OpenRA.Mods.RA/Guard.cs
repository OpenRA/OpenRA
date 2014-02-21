#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class GuardInfo : TraitInfo<Guard> { }

	class Guard : IResolveOrder, IOrderVoice
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Guard")
			{
				var target = Target.FromActor(order.TargetActor);

				GuardTarget(self, target);
			}
		}

		public void GuardTarget(Actor self, Target target)
		{
			self.SetTargetLine(target, Color.Yellow);

			var range = WDist.FromCells(target.Actor.Info.Traits.Get<GuardableInfo>().Range);
			self.QueueActivity(false, new AttackMove.AttackMoveActivity(self, self.Trait<IMove>().MoveFollow(self, target, range)));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Guard" ? "Move" : null;
		}
	}

	class GuardOrderGenerator : IOrderGenerator
	{
		readonly IEnumerable<Actor> subjects;

		public GuardOrderGenerator(IEnumerable<Actor> subjects)
		{
			this.subjects = subjects;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == Game.mouseButtonPreference.Cancel)
			{
				world.CancelInputMode();
				yield break;
			}

			var target = FriendlyGuardableUnits(world, mi).FirstOrDefault();

			if (target == null || subjects.All(s => s.IsDead()))
				yield break;

			foreach (var subject in subjects)
				if (subject != target)
					yield return new Order("Guard", subject, false) { TargetActor = target };
		}

		public void Tick(World world)
		{
			if (subjects.All(s => s.IsDead() || !s.HasTrait<Guard>()))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world) { }

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			var multiple = subjects.Count() > 1;
			var canGuard = FriendlyGuardableUnits(world, mi)
				.Any(a => multiple || a != subjects.First());

			return canGuard ? "guard" : "move-blocked";
		}

		static IEnumerable<Actor> FriendlyGuardableUnits(World world, MouseInput mi)
		{
			return world.ScreenMap.ActorsAt(mi)
				.Where(a => !world.FogObscures(a) && !a.IsDead() &&
					a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) &&
					a.HasTrait<Guardable>());
		}
	}

	class GuardableInfo : TraitInfo<Guardable>
	{
		public readonly int Range = 2;
	}

	class Guardable { }
}
