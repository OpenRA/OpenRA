#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.RA
{
	class PortableChronoInfo : ITraitInfo
	{
		[Desc("Cooldown in seconds until the unit can teleport.")]
		public readonly int ChargeTime = 20;
		[Desc("Can the unit teleport only a certain distance?")]
		public readonly bool HasDistanceLimit = true;
		[Desc("The maximum distance in cells this unit can teleport (only used if HasDistanceLimit = true).")]
		public readonly int MaxDistance = 12;
		[Desc("Sound to play when teleporting.")]
		public readonly string ChronoshiftSound = "chrotnk1.aud";

		public object Create(ActorInitializer init) { return new PortableChrono(this); }
	}

	class PortableChrono : IIssueOrder, IResolveOrder, ITick, IPips, IOrderVoice, ISync
	{
		[Sync] int chargeTick = 0;
		public readonly PortableChronoInfo Info;

		public PortableChrono(PortableChronoInfo info)
		{
			this.Info = info;
		}

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new PortableChronoOrderTargeter();
				yield return new DeployOrderTargeter("PortableChronoDeploy", 5, () => CanTeleport);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PortableChronoDeploy" && CanTeleport)
				self.World.OrderGenerator = new PortableChronoOrderGenerator(self);

			if (order.OrderID == "PortableChronoTeleport")
				return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PortableChronoTeleport" && CanTeleport)
			{
				var maxDistance = Info.HasDistanceLimit ? Info.MaxDistance : (int?)null;
				self.CancelActivity();
				self.QueueActivity(new Teleport(null, order.TargetLocation, maxDistance, true, false, Info.ChronoshiftSound));
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "PortableChronoTeleport" && CanTeleport ? "Move" : null;
		}

		public void ResetChargeTime()
		{
			chargeTick = 25 * Info.ChargeTime;
		}

		public bool CanTeleport
		{
			get { return chargeTick <= 0; }
		}

		// Display 2 pips indicating the current charge status
		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 2;
			for (var i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / (25 * Info.ChargeTime)) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}

				yield return PipType.Blue;
			}
		}
	}

	class PortableChronoOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "PortableChronoTeleport"; } }
		public int OrderPriority { get { return 5; } }
		public bool IsQueued { get; protected set; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
		{
			// TODO: When target modifiers are configurable this needs to be revisited
			if (modifiers.HasModifier(TargetModifiers.ForceMove) || modifiers.HasModifier(TargetModifiers.ForceQueue))
			{
				var xy = self.World.Map.CellContaining(target.CenterPosition);

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (self.IsInWorld && self.Owner.Shroud.IsExplored(xy))
				{
					cursor = "chrono-target";
					return true;
				}
				return false;
			}

			return false;
		}
	}

	class PortableChronoOrderGenerator : IOrderGenerator
	{
		readonly Actor self;

		public PortableChronoOrderGenerator(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld && self.Location != xy
				&& self.Trait<PortableChrono>().CanTeleport && self.Owner.Shroud.IsExplored(xy))
			{
				world.CancelInputMode();
				yield return new Order("PortableChronoTeleport", self, mi.Modifiers.HasModifier(Modifiers.Shift)) { TargetLocation = xy };
			}
		}

		public void Tick(World world)
		{
			if (!self.IsInWorld || self.IsDead)
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
		{
			yield break;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
		{
			if (!self.IsInWorld || self.Owner != self.World.LocalPlayer)
				yield break;

			if (!self.Trait<PortableChrono>().Info.HasDistanceLimit)
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				WRange.FromCells(self.Trait<PortableChrono>().Info.MaxDistance),
				0,
				Color.FromArgb(128, Color.LawnGreen),
				Color.FromArgb(96, Color.Black)
			);
		}

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			if (self.IsInWorld && self.Location != xy
				&& self.Trait<PortableChrono>().CanTeleport && self.Owner.Shroud.IsExplored(xy))
				return "chrono-target";
			else
				return "move-blocked";
		}
	}
}
