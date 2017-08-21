#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Sets the mimimum speed of a group after the command is given to stick together. Attach this to the player actor.")]
	public class GroupSpeedInfo : TraitInfo<GroupSpeed> { }

	public class GroupSpeed : IResolveOrder, IOrderVoice
	{
		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GroupSpeed")
				return;

			var actors = order.TargetString.Split(',')
				.Select(id => uint.Parse(id, NumberStyles.Any, NumberFormatInfo.InvariantInfo))
				.Select(id => self.World.GetActorById(id))
				.Where(a => a != null);

			var speedLimit = int.MaxValue;
			foreach (var actor in actors)
			{
				var move = actor.TraitsImplementing<IMove>().FirstOrDefault();
				if (move.SpeedLimit > 0) // toggle off if one already has a speed limit
					speedLimit = 0;

				speedLimit = Math.Min(move.Speed, speedLimit);
			}

			foreach (var actor in actors)
			{
				var move = actor.TraitsImplementing<IMove>().FirstOrDefault();
				move.SpeedLimit = speedLimit;
			}

			var actorList = actors.Select(a => "#{0} {1}".F(a.ActorID, a.Info.Name)).JoinWith(", ");
			Log.Write("debug", "Grouped actors {0} with new speed limit {1}".F(actorList, speedLimit));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return null;
		}
	}
}
