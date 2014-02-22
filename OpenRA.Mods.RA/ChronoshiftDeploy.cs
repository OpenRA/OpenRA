#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class ChronoshiftDeployInfo : ITraitInfo
	{
		public readonly int ChargeTime = 30; // seconds
		public readonly int JumpDistance = 10;
		public readonly string ChronoshiftSound = "chrotnk1.aud";

		public object Create(ActorInitializer init) { return new ChronoshiftDeploy(init.self, this); }
	}

	class ChronoshiftDeploy : IIssueOrder, IResolveOrder, ITick, IPips, IOrderVoice, ISync
	{
		[Sync] int chargeTick = 0;
		public readonly ChronoshiftDeployInfo Info;
		readonly Actor self;

		public ChronoshiftDeploy(Actor self, ChronoshiftDeployInfo info)
		{
			this.self = self;
			this.Info = info;
		}

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("ChronoshiftJump", 5, () => chargeTick <= 0); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "ChronoshiftJump" && chargeTick <= 0)
				self.World.OrderGenerator = new ChronoTankOrderGenerator(self);

			return new Order("ChronoshiftJump", self, false); // Hack until we can return null
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "ChronoshiftJump")
			{
				if (CanJumpTo(order.TargetLocation, true))
				{
					self.CancelActivity();
					self.QueueActivity(new Teleport(null, order.TargetLocation, true, Info.ChronoshiftSound));
					chargeTick = 25 * Info.ChargeTime;
				}
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "ChronoshiftDeploy" && chargeTick <= 0) ? "Move" : null;
		}

		// Display 2 pips indicating the current charge status
		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 2;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / (25 * Info.ChargeTime)) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}

				yield return PipType.Blue;
			}
		}

		public bool CanJumpTo(CPos xy, bool ignoreVis)
		{
			var movement = self.TraitOrDefault<IPositionable>();

			if (chargeTick <= 0 // Can jump
				&& (self.Location - xy).Length <= Info.JumpDistance // Within jump range
				&& movement.CanEnterCell(xy) // Can enter cell
				&& (ignoreVis || self.Owner.Shroud.IsExplored(xy))) // Not in shroud						
				return true;
			else
				return false;
		}
	}

	class ChronoTankOrderGenerator : IOrderGenerator
	{
		readonly Actor self;

		public ChronoTankOrderGenerator(Actor self) { this.self = self; }

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == Game.mouseButtonPreference.Cancel)
			{
				world.CancelInputMode();
				yield break;
			}

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);

			var cinfo = self.Trait<ChronoshiftDeploy>();
			if (cinfo.CanJumpTo(xy, false))
			{
				self.World.CancelInputMode();
				yield return new Order("ChronoshiftJump", self, queued) { TargetLocation = xy };
			}
		}

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			if (self.IsInWorld && self.Trait<ChronoshiftDeploy>().CanJumpTo(xy,false))
				return "chrono-target";
			else
				return "move-blocked";
		}

		public void Tick(World world)
		{
			if (!self.IsInWorld || self.IsDead())
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world)
		{
			if (!self.IsInWorld)
				return;

			if (self.Owner != self.World.LocalPlayer)
				return;

			wr.DrawRangeCircleWithContrast(
				self.CenterPosition,
				WDist.FromCells(self.Trait<ChronoshiftDeploy>().Info.JumpDistance),
				Color.FromArgb(128, Color.DeepSkyBlue),
				Color.FromArgb(96, Color.Black)
			);
		}
	}
}
