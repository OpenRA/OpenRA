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
		
		public virtual object Create(ActorInitializer init) { return new Transforms(this); }
	}

	class Transforms : IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		TransformsInfo Info;
		BuildingInfo bi;
		
		public Transforms(TransformsInfo info)
		{
			Info = info;
			bi = Rules.Info[info.IntoActor].Traits.GetOrDefault<BuildingInfo>();
		}
		
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && self == underCursor)
				return new Order("DeployTransform", self);

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? "Move" : null;
		}
		
		bool CanDeploy(Actor self)
		{
			return (bi == null || self.World.CanPlaceBuilding(Info.IntoActor, bi, self.Location + Info.Offset, self));
		}
		
		public void ResolveOrder( Actor self, Order order )
		{
			if (order.OrderString == "DeployTransform")
			{
				if (!CanDeploy(self))
				{
					foreach (var s in Info.NoTransformSounds)
						Sound.PlayToPlayer(self.Owner, s);
					return;
				}
				self.CancelActivity();
				
				if (self.traits.Contains<IFacing>())
					self.QueueActivity(new Turn(Info.Facing));
				
				self.QueueActivity(new Transform(self, Info.IntoActor, Info.Offset, Info.Facing, Info.TransformSounds));
			}
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeployTransform")
				return null;
			
			return CanDeploy(self) ? "deploy" : "deploy-blocked";
		}
	}
}
