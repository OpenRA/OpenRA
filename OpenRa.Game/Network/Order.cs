using System;
using System.IO;
using System.Linq;

namespace OpenRa
{
	public sealed class Order
	{
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly Actor TargetActor;
		public readonly int2 TargetLocation;
		public readonly string TargetString;
		public bool IsImmediate;
		
		public Player Player { get { return Subject.Owner; } }

		public Order(string orderString, Actor subject, 
			Actor targetActor, int2 targetLocation, string targetString)
		{
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
		}

		public Order(string orderString, Actor subject) 
			: this(orderString, subject, null, int2.Zero, null) { }
		public Order(string orderString, Actor subject, Actor targetActor)
			: this(orderString, subject, targetActor, int2.Zero, null) { }
		public Order(string orderString, Actor subject, int2 targetLocation)
			: this(orderString, subject, null, targetLocation, null) { }
		public Order(string orderString, Actor subject, string targetString)
			: this(orderString, subject, null, int2.Zero, targetString) { }
		public Order(string orderString, Actor subject, Actor targetActor, int2 targetLocation)
			: this(orderString, subject, targetActor, targetLocation, null) { }
		public Order(string orderString, Actor subject, Actor targetActor, string targetString)
			: this(orderString, subject, targetActor, int2.Zero, targetString) { }
		public Order(string orderString, Actor subject, int2 targetLocation, string targetString)
			: this(orderString, subject, null, targetLocation, targetString) { }

		public byte[] Serialize()
		{
			if (IsImmediate)		/* chat, whatever */
			{
				var ret = new MemoryStream();
				var w = new BinaryWriter(ret);
				w.Write((byte)0xfe);
				w.Write((uint)Player.Index);
				w.Write(OrderString);
				w.Write(TargetString);
				return ret.ToArray();
			}

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
						w.Write(OrderString);
						w.Write(UIntFromActor(Subject));
						w.Write(UIntFromActor(TargetActor));
						w.Write(TargetLocation.X);
						w.Write(TargetLocation.Y);
						w.Write(TargetString != null);
						if (TargetString != null)
							w.Write(TargetString);
						return ret.ToArray();
					}
			}
		}

		static Player LookupPlayer(World world, uint index)
		{
			return world.players
				.Where(x => x.Value.Index == index)
				.First().Value;
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var order = r.ReadString();
						var subjectId = r.ReadUInt32();
						var targetActorId = r.ReadUInt32();
						var targetLocation = new int2(r.ReadInt32(), 0);
						targetLocation.Y = r.ReadInt32();
						var targetString = null as string;
						if (r.ReadBoolean())
							targetString = r.ReadString();

						Actor subject, targetActor;
						if( !TryGetActorFromUInt( world, subjectId, out subject ) || !TryGetActorFromUInt( world, targetActorId, out targetActor ) )
							return null;

						return new Order( order, subject, targetActor, targetLocation, targetString);
					}

				case 0xfe:
					{
						var playerID = r.ReadUInt32();
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order( name, LookupPlayer( world, playerID ).PlayerActor, data ) { IsImmediate = true };
					}

				default:
					throw new NotImplementedException();
			}
		}

		static uint UIntFromActor(Actor a)
		{
			if (a == null) return 0xffffffff;
			return a.ActorID;
		}

		static bool TryGetActorFromUInt(World world, uint aID, out Actor ret )
		{
			if( aID == 0xFFFFFFFF )
			{
				ret = null;
				return true;
			}
			else
			{
				foreach( var a in world.Actors.Where( x => x.ActorID == aID ) )
				{
					ret = a;
					return true;
				}
				ret = null;
				return false;
			}
		}

		// Named constructors for Orders.
		// Now that Orders are resolved by individual Actors, these are weird; you unpack orders manually, but not pack them.
		public static Order Chat(Player subject, string text)
		{
			return new Order("Chat", subject.PlayerActor, text) { IsImmediate = true };
		}

		public static Order StartProduction(Player subject, string item)
		{
			return new Order("StartProduction", subject.PlayerActor, item );
		}

		public static Order PauseProduction(Player subject, string item, bool pause)
		{
			return new Order("PauseProduction", subject.PlayerActor, new int2( pause ? 1 : 0, 0 ), item);
		}

		public static Order CancelProduction(Player subject, string item)
		{
			return new Order("CancelProduction", subject.PlayerActor, item);
		}
	}
}
