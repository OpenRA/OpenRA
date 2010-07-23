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

namespace OpenRA.Mods.RA
{
	class TransformsOnDeployInfo : TraitInfo<TransformsOnDeploy>
	{
		[ActorReference]
		public readonly string TransformsInto = null;
		public readonly int[] Offset = null;
		public readonly int[] DeployDirections = new int[] {96};
		public readonly bool TransferHealthPercentage = true; // Set to false to transfer the absolute health
		public readonly string[] TransformSounds = null;
		public readonly string[] NoTransformSounds = null;
	}

	class TransformsOnDeploy : IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
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
		
		public void ResolveOrder( Actor self, Order order )
		{
			if (order.OrderString == "DeployTransform")
			{
				var info = self.Info.Traits.Get<TransformsOnDeployInfo>();
				
				var transInfo = Rules.Info[info.TransformsInto];
				
				if (transInfo.Traits.Contains<BuildingInfo>())
				{
					var bi = transInfo.Traits.Get<BuildingInfo>();
					if (!self.World.CanPlaceBuilding(info.TransformsInto, bi, self.Location + new int2(info.Offset[0], info.Offset[1]), self))
					{
						foreach (var s in info.NoTransformSounds)
							Sound.PlayToPlayer(self.Owner, s);
							
						return;
					}
					
				}
				self.CancelActivity();


				if (self.traits.Contains<Unit>())	// Pick the closest deploy direction to turn to
				{
					// TODO: Pick the closest deploy direction
					var bestDir = info.DeployDirections[0];
										
					self.QueueActivity(new Turn(bestDir));
				}
				
				self.QueueActivity(new TransformIntoActor(info.TransformsInto, new int2(info.Offset[0], info.Offset[1]), info.TransferHealthPercentage, info.TransformSounds));
			}
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeployTransform")
				return null;

			var depInfo = self.Info.Traits.Get<TransformsOnDeployInfo>();
			var transInfo = Rules.Info[depInfo.TransformsInto];
			if (transInfo.Traits.Contains<BuildingInfo>())
			{
				var bi = transInfo.Traits.Get<BuildingInfo>();
				if (!self.World.CanPlaceBuilding(depInfo.TransformsInto, bi, self.Location + new int2(depInfo.Offset[0], depInfo.Offset[1]), self))
					return "deploy-blocked";
			}
			return "deploy";
		}
	}
}
