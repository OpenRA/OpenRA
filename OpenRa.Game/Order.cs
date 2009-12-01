using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenRa.Game.Traits;
using System.IO;

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
		public bool IsImmediate;

		Order(Player player, string orderString, Actor subject, 
			Actor targetActor, int2 targetLocation, string targetString)
		{
			this.Player = player;
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
		}

		public byte[] Serialize()
		{
			switch (OrderString)
			{
				// Format:
				//		u8    : orderID.
				//		            0xFF: Full serialized order.
				//		varies: rest of order.
				default:
					// TODO: specific serializers for specific orders.
					{
						var ret = new MemoryStream();
						var w = new BinaryWriter(ret);
						w.Write( (byte)0xFF );
						w.Write( (uint)Player.Index );
						w.Write(OrderString);
						w.Write(Subject == null ? 0xFFFFFFFF : Subject.ActorID);
						w.Write(TargetActor == null ? 0xFFFFFFFF : TargetActor.ActorID);
						w.Write(TargetLocation.X);
						w.Write(TargetLocation.Y);
						w.Write(TargetString != null);
						if (TargetString != null)
							w.Write(TargetString);
						return ret.ToArray();
					}
			}
		}

		public static Order Deserialize(BinaryReader r)
		{
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var playerID = r.ReadUInt32();
						var order = r.ReadString();
						var subject = ActorFromUInt(r.ReadUInt32());
						var targetActor = ActorFromUInt(r.ReadUInt32());
						var targetLocation = new int2(r.ReadInt32(), 0);
						targetLocation.Y = r.ReadInt32();
						var targetString = null as string;
						if (r.ReadBoolean())
							targetString = r.ReadString();

						var player = Game.players.Where( x => x.Value.Index == playerID ).First().Value;
						return new Order( player, order, subject, targetActor, targetLocation, targetString);
					}
				default:
					throw new NotImplementedException();
			}
		}

		static Actor ActorFromUInt(uint aID)
		{
			if (aID == 0xFFFFFFFF) return null;
			return Game.world.Actors.Where(x => x.ActorID == aID).First();
		}

		// Named constructors for Orders.
		// Now that Orders are resolved by individual Actors, these are weird; you unpack orders manually, but not pack them.
		public static Order Chat(Player subject, string text)
		{
			return new Order(subject, "Chat", null, null, int2.Zero, text)
				{ IsImmediate = true };
		}

		public static Order Attack(Actor subject, Actor target)
		{
			return new Order(subject.Owner, "Attack", subject, target, int2.Zero, null);
		}

		public static Order Move(Actor subject, int2 target)
		{
			return new Order(subject.Owner, "Move", subject, null, target, null);
		}

		public static Order DeployMcv(Actor subject)
		{
			return new Order(subject.Owner, "DeployMcv", subject, null, int2.Zero, null);
		}

		public static Order PlaceBuilding(Player subject, int2 target, string buildingName)
		{
			return new Order(subject, "PlaceBuilding", null, null, target, buildingName);
		}

		public static Order DeliverOre(Actor subject, Actor target)
		{
			return new Order(subject.Owner, "DeliverOre", subject, target, int2.Zero, null);
		}

		public static Order Harvest(Actor subject, int2 target)
		{
			return new Order(subject.Owner, "Harvest", subject, null, target, null);
		}

		public static Order StartProduction(Player subject, string item)
		{
			return new Order(subject, "StartProduction", null, null, int2.Zero, item );
		}

		public static Order PauseProduction(Player subject, string item, bool pause)
		{
			return new Order( subject, "PauseProduction", null, null, new int2(pause ?1:0,0), item );
		}

		public static Order CancelProduction(Player subject, string item)
		{
			return new Order( subject, "CancelProduction", null, null, int2.Zero, item );
		}

		public static Order SetRallyPoint(Actor subject, int2 target)
		{
			return new Order(subject.Owner, "SetRallyPoint", subject, null, target, null );
		}
	}
}
