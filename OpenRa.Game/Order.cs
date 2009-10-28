using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	sealed class Order
	{
		public readonly Player Player;
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly Actor TargetActor;
		public readonly int2 TargetLocation;
		public readonly string TargetString;

		private Order( Player player, string orderString, Actor subject, Actor targetActor, int2 targetLocation, string targetString )
		{
			this.Player = player;
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
		}

		// TODO: serialize / deserialize

		public static Order Attack( Actor subject, Actor target )
		{
			return new Order( subject.Owner, "Attack", subject, target, int2.Zero, null );
		}

		public static Order Move( Actor subject, int2 target )
		{
			return new Order( subject.Owner, "Move", subject, null, target, null );
		}

		public static Order DeployMcv( Actor subject )
		{
			return new Order( subject.Owner, "DeployMcv", subject, null, int2.Zero, null );
		}

		public static Order PlaceBuilding( Player subject, int2 target, string buildingName )
		{
			return new Order( subject, "PlaceBuilding", null, null, target, buildingName );
		}
	}
}
