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
		public readonly Cursor Cursor;

		Order(Player player, string orderString, Actor subject, Actor targetActor, int2 targetLocation, string targetString, Cursor cursor)
		{
			this.Player = player;
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Cursor = cursor;
		}

		public byte[] Serialize()
		{
			switch (OrderString)
			{
				// Format:
				//		u32   : player, with msb set. (if msb is clear, not an order)
				//		u8    : orderID.
				//		            0xFF: Full serialized order.
				//		varies: rest of order.
				default:
					// TODO: specific serializers for specific orders.
					{
						var ret = new MemoryStream();
						var w = new BinaryWriter(ret);
						w.Write((uint)Player.Index | 0x80000000u);
						w.Write((byte)0xFF);
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

		public static Order Deserialize(BinaryReader r, uint first)
		{
			if ((first >> 31) == 0) return null;

			var player = Game.players.Where(x => x.Value.Index == (first & 0x7FFFFFFF)).First().Value;
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var order = r.ReadString();
						var subject = ActorFromUInt(r.ReadUInt32());
						var targetActor = ActorFromUInt(r.ReadUInt32());
						var targetLocation = new int2(r.ReadInt32(), 0);
						targetLocation.Y = r.ReadInt32();
						var targetString = null as string;
						if (r.ReadBoolean())
							targetString = r.ReadString();
						return new Order(player, order, subject, targetActor, targetLocation, targetString, null);
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

		public static Order Attack(Actor subject, Actor target)
		{
			return new Order(subject.Owner, "Attack", subject, target, int2.Zero, null, Cursor.Attack);
		}

		public static Order Move(Actor subject, int2 target, bool isBlocked)
		{
			return new Order(subject.Owner, "Move", subject, null, target, null, 
				isBlocked ? Cursor.MoveBlocked : Cursor.Move);
		}

		public static Order DeployMcv(Actor subject, bool isBlocked)
		{
			return new Order(subject.Owner, "DeployMcv", subject, null, int2.Zero, isBlocked ? "nop" : null, 
				isBlocked ? Cursor.DeployBlocked : Cursor.Deploy);
		}

		public static Order PlaceBuilding(Player subject, int2 target, string buildingName)
		{
			return new Order(subject, "PlaceBuilding", null, null, target, buildingName, Cursor.Default);
		}

		public static Order DeliverOre(Actor subject, Actor target)
		{
			return new Order(subject.Owner, "DeliverOre", subject, target, int2.Zero, null, Cursor.Enter);
		}

		public static Order StartProduction(Player subject, string item)
		{
			return new Order(subject, "StartProduction", null, null, int2.Zero, item, Cursor.Default );
		}
	}
}
