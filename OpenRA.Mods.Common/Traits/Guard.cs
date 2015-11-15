#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The player can give this unit the order to follow and protect friendly units with the Guardable trait.")]
	public class GuardInfo : ITraitInfo
	{
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Guard(this); }
	}

	public class Guard : IResolveOrder, IOrderVoice
	{
		readonly GuardInfo info;

		public Guard(GuardInfo info)
		{
			this.info = info;
		}

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

			var range = target.Actor.Info.TraitInfo<GuardableInfo>().Range;
			self.QueueActivity(false, new AttackMoveActivity(self, self.Trait<IMove>().MoveFollow(self, target, WDist.Zero, range)));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Guard" ? info.Voice : null;
		}
	}

	public class GuardOrderGenerator : GenericSelectTarget
	{
		public GuardOrderGenerator(IEnumerable<Actor> subjects, string order, string cursor, MouseButton button)
			: base(subjects, order, cursor, button) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos xy, MouseInput mi)
		{
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
