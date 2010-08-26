#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public sealed class Order
	{
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly Actor TargetActor;
		public readonly int2 TargetLocation;
		public readonly string TargetString;
		public readonly bool Queued;
		public bool IsImmediate;
		
		public Player Player { get { return Subject.Owner; } }

		public Order(string orderString, Actor subject, 
			Actor targetActor, int2 targetLocation, string targetString, bool queued)
		{
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Queued = queued;
		}

		public Order(string orderString, Actor subject) 
			: this(orderString, subject, null, int2.Zero, null, false) { }
		public Order(string orderString, Actor subject, Actor targetActor)
			: this(orderString, subject, targetActor, int2.Zero, null, false) { }
		public Order(string orderString, Actor subject, int2 targetLocation)
			: this(orderString, subject, null, targetLocation, null, false) { }
		public Order(string orderString, Actor subject, int2 targetLocation, bool queued)
			: this(orderString, subject, null, targetLocation, null, queued) { }
		public Order(string orderString, Actor subject, string targetString)
			: this(orderString, subject, null, int2.Zero, targetString, false) { }
		public Order(string orderString, Actor subject, Actor targetActor, int2 targetLocation)
			: this(orderString, subject, targetActor, targetLocation, null, false) { }
		public Order(string orderString, Actor subject, Actor targetActor, string targetString)
			: this(orderString, subject, targetActor, int2.Zero, targetString, false) { }
		public Order(string orderString, Actor subject, int2 targetLocation, string targetString)
			: this(orderString, subject, null, targetLocation, targetString, false) { }

		public byte[] Serialize()
		{
			if (IsImmediate)		/* chat, whatever */
			{
				var ret = new MemoryStream();
				var w = new BinaryWriter(ret);
				w.Write((byte)0xfe);
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
						w.Write(Queued);
						return ret.ToArray();
					}
			}
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
						var queued = r.ReadBoolean();

						Actor subject, targetActor;
						if( !TryGetActorFromUInt( world, subjectId, out subject ) || !TryGetActorFromUInt( world, targetActorId, out targetActor ) )
							return null;

						return new Order( order, subject, targetActor, targetLocation, targetString, queued);
					}

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order( name, null, data ) { IsImmediate = true };
					}

				default:
					throw new NotImplementedException();
			}
		}
		
		public override string ToString()
		{
			return "OrderString: \"{0}\" \n\t Subject: \"{1}\". \n\t TargetActor: \"{2}\" \n\t TargetLocation: {3}." +
				"\n\t TargetString: \"{4}\".\n\t IsImmediate: {5}.\n\t Player(PlayerName): {6}\n".F(
			    		OrderString, Subject, TargetActor.Info.Name , TargetLocation, TargetString, IsImmediate, Player.PlayerName);
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
		public static Order Chat(string text)
		{
			return new Order("Chat", null, text) { IsImmediate = true };
		}

		public static Order TeamChat(string text)
		{
			return new Order("TeamChat", null, text) { IsImmediate = true };
		}
		
		public static Order Command(string text)
		{
			return new Order("Command", null, text) { IsImmediate = true };	
		}

		public static Order StartProduction(Actor subject, string item, int count)
		{
			return new Order("StartProduction", subject, new int2( count, 0 ), item );
		}

		public static Order PauseProduction(Actor subject, string item, bool pause)
		{
			return new Order("PauseProduction", subject, new int2( pause ? 1 : 0, 0 ), item);
		}

		public static Order CancelProduction(Actor subject, string item)
		{
			return new Order("CancelProduction", subject, item);
		}
		
		public static Order StartProduction(Player subject, string item, int count)
		{
			return new Order("StartProduction", subject.PlayerActor, new int2( count, 0 ), item );
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
