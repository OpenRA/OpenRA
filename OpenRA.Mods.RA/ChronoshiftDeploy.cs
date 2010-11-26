#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ChronoshiftDeployInfo : TraitInfo<ChronoshiftDeploy>
	{
		public readonly int ChargeTime = 120; // Seconds
	}

	class ChronoshiftDeploy : IIssueOrder, IResolveOrder, ITick, IPips, IOrderVoice
	{
		// Recharge logic
		[Sync]
		int chargeTick = 0; // How long until we can chronoshift again?

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter( "ChronoshiftDeploy", 5, () => chargeTick <= 0 ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "ChronoshiftDeploy" )
				if (chargeTick <= 0)
					self.World.OrderGenerator = new SetChronoTankDestination( self );

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			var movement = self.TraitOrDefault<IMove>();
			if (order.OrderString == "ChronoshiftSelf" && movement.CanEnterCell(order.TargetLocation))
			{
				if (self.Owner == self.World.LocalPlayer)
				{
					self.World.CancelInputMode();
				}
				
				self.CancelActivity();
				self.QueueActivity(new Teleport(order.TargetLocation));
				Sound.Play("chrotnk1.aud", self.CenterLocation);
				Sound.Play("chrotnk1.aud", Game.CellSize * order.TargetLocation.ToFloat2());
				chargeTick = 25 * self.Info.Traits.Get<ChronoshiftDeployInfo>().ChargeTime;

				foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "ChronoshiftDeploy" && chargeTick <= 0) ? "Move" : null;
		}
		
		// Display 5 pips indicating the current charge status
		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 5;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / (25 * self.Info.Traits.Get<ChronoshiftDeployInfo>().ChargeTime)) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}

				switch (i)
				{
					case 0:
					case 1:
						yield return PipType.Red;
						break;
					case 2:
					case 3:
						yield return PipType.Yellow;
						break;
					case 4:
						yield return PipType.Green;
						break;
				}
			}
		}
	}
}
