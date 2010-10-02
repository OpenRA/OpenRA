#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.GameRules;
using System.Collections.Generic;
using OpenRA.Mods.RA.Orders;

namespace OpenRA.Mods.RA
{
	class TransformsInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string IntoActor = null;
		public readonly int2 Offset = int2.Zero;
		public readonly int Facing = 96;
		public readonly string[] TransformSounds = {};
		public readonly string[] NoTransformSounds = {};
		
		public virtual object Create(ActorInitializer init) { return new Transforms(init.self, this); }
	}

	class Transforms : IIssueOrder, IResolveOrder, IOrderVoice
	{
		Actor self;
		TransformsInfo Info;
		BuildingInfo bi;
		
		public Transforms(Actor self, TransformsInfo info)
		{
			this.self = self;
			Info = info;
			bi = Rules.Info[info.IntoActor].Traits.GetOrDefault<BuildingInfo>();
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? "Move" : null;
		}
		
		bool CanDeploy()
		{
			return (bi == null || self.World.CanPlaceBuilding(Info.IntoActor, bi, self.Location + Info.Offset, self));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter( "DeployTransform", 5, () => CanDeploy() ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "DeployTransform" )
				return new Order( order.OrderID, self );

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if (order.OrderString == "DeployTransform")
			{
				if (!CanDeploy())
				{
					foreach (var s in Info.NoTransformSounds)
						Sound.PlayToPlayer(self.Owner, s);
					return;
				}
				self.CancelActivity();
				
				if (self.HasTrait<IFacing>())
					self.QueueActivity(new Turn(Info.Facing));
				
				self.QueueActivity(new Transform(self, Info.IntoActor, Info.Offset, Info.Facing, Info.TransformSounds));
			}
		}
	}
}
