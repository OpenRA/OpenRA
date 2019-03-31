#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class PortableChronoInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[Desc("Cooldown in ticks until the unit can teleport.")]
		public readonly int ChargeDelay = 500;

		[Desc("Can the unit teleport only a certain distance?")]
		public readonly bool HasDistanceLimit = true;

		[Desc("The maximum distance in cells this unit can teleport (only used if HasDistanceLimit = true).")]
		public readonly int MaxDistance = 12;

		[Desc("Sound to play when teleporting.")]
		public readonly string ChronoshiftSound = "chrotnk1.aud";

		[Desc("Cursor to display when able to deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[Desc("Cursor to display when targeting a teleport location.")]
		public readonly string TargetCursor = "chrono-target";

		[Desc("Cursor to display when the targeted location is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		[Desc("Kill cargo on teleporting.")]
		public readonly bool KillCargo = true;

		[Desc("Flash the screen on teleporting.")]
		public readonly bool FlashScreen = false;

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new PortableChrono(init.Self, this); }
	}

	class PortableChrono : IIssueOrder, IResolveOrder, ITick, ISelectionBar, IOrderVoice, ISync
	{
		public readonly PortableChronoInfo Info;
		readonly IMove move;
		[Sync] int chargeTick = 0;

		public PortableChrono(Actor self, PortableChronoInfo info)
		{
			Info = info;
			move = self.Trait<IMove>();
		}

		void ITick.Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new PortableChronoOrderTargeter(Info.TargetCursor);
				yield return new DeployOrderTargeter("PortableChronoDeploy", 5,
					() => CanTeleport ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PortableChronoDeploy")
			{
				// HACK: Switch the global order generator instead of actually issuing an order
				if (CanTeleport)
					self.World.OrderGenerator = new PortableChronoOrderGenerator(self, Info);

				// HACK: We need to issue a fake order to stop the game complaining about the bodge above
				return new Order(order.OrderID, self, Target.Invalid, queued);
			}

			if (order.OrderID == "PortableChronoTeleport")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PortableChronoTeleport" && order.Target.Type != TargetType.Invalid)
			{
				var maxDistance = Info.HasDistanceLimit ? Info.MaxDistance : (int?)null;
				if (!order.Queued)
					self.CancelActivity();

				var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
				self.SetTargetLine(order.Target, Color.LawnGreen);
				if (maxDistance != null)
					self.QueueActivity(move.MoveWithinRange(order.Target, WDist.FromCells(maxDistance.Value)));

				self.QueueActivity(new Teleport(self, cell, maxDistance, Info.KillCargo, Info.FlashScreen, Info.ChronoshiftSound));
				self.QueueActivity(move.MoveTo(cell, 5));
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "PortableChronoTeleport" ? Info.Voice : null;
		}

		public void ResetChargeTime()
		{
			chargeTick = Info.ChargeDelay;
		}

		public bool CanTeleport
		{
			get { return chargeTick <= 0; }
		}

		float ISelectionBar.GetValue()
		{
			return (float)(Info.ChargeDelay - chargeTick) / Info.ChargeDelay;
		}

		Color ISelectionBar.GetColor() { return Color.Magenta; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}

	class PortableChronoOrderTargeter : IOrderTargeter
	{
		readonly string targetCursor;

		public PortableChronoOrderTargeter(string targetCursor)
		{
			this.targetCursor = targetCursor;
		}

		public string OrderID { get { return "PortableChronoTeleport"; } }
		public int OrderPriority { get { return 5; } }
		public bool IsQueued { get; protected set; }
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			if (modifiers.HasModifier(TargetModifiers.ForceMove))
			{
				var xy = self.World.Map.CellContaining(target.CenterPosition);

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (self.IsInWorld && self.Owner.Shroud.IsExplored(xy))
				{
					cursor = targetCursor;
					return true;
				}

				return false;
			}

			return false;
		}
	}

	class PortableChronoOrderGenerator : OrderGenerator
	{
		readonly Actor self;
		readonly PortableChronoInfo info;

		public PortableChronoOrderGenerator(Actor self, PortableChronoInfo info)
		{
			this.self = self;
			this.info = info;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == Game.Settings.Game.MouseButtonPreference.Cancel)
			{
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld && self.Location != cell
				&& self.Trait<PortableChrono>().CanTeleport && self.Owner.Shroud.IsExplored(cell))
			{
				world.CancelInputMode();
				yield return new Order("PortableChronoTeleport", self, Target.FromCell(world, cell), mi.Modifiers.HasModifier(Modifiers.Shift));
			}
		}

		protected override void Tick(World world)
		{
			if (!self.IsInWorld || self.IsDead)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
		{
			yield break;
		}

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			if (!self.IsInWorld || self.Owner != self.World.LocalPlayer)
				yield break;

			if (!self.Trait<PortableChrono>().Info.HasDistanceLimit)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				WDist.FromCells(self.Trait<PortableChrono>().Info.MaxDistance),
				0,
				Color.FromArgb(128, Color.LawnGreen),
				Color.FromArgb(96, Color.Black));
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (self.IsInWorld && self.Location != cell
				&& self.Trait<PortableChrono>().CanTeleport && self.Owner.Shroud.IsExplored(cell))
				return info.TargetCursor;
			else
				return info.TargetBlockedCursor;
		}
	}
}
