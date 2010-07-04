#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Traits.Activities;

namespace OpenRA.Traits
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

	class TransformsOnDeploy : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && self == underCursor)
				return new Order("DeployTransform", self);

			return null;
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
	}
}
