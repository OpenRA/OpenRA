#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.Network;
using ProtoBuf;

namespace OpenRA
{
	[ProtoContract]
	sealed class LegacyOrder
	{
		[ProtoMember(1)] public string OrderString;
		[ProtoMember(2)] public uint SubjectID;
		[ProtoMember(3)] public bool Queued;
		[ProtoMember(4)] public uint TargetActorID;
		[ProtoMember(5)] public CPos TargetLocation;
		[ProtoMember(6)] public string TargetString;
		[ProtoMember(7)] public CPos ExtraLocation;
		[ProtoMember(8)] public uint ExtraData;
	}

	public sealed class Order
	{
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly bool Queued;
		public Actor TargetActor;
		public CPos TargetLocation;
		public string TargetString;
		public CPos ExtraLocation;
		public uint ExtraData;
		public bool IsImmediate;
		public bool SuppressVisualFeedback;

		public Player Player { get { return Subject != null ? Subject.Owner : null; } }

		Order(string orderString, Actor subject,
			Actor targetActor, CPos targetLocation, string targetString, bool queued, CPos extraLocation, uint extraData)
		{
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Queued = queued;
			this.ExtraLocation = extraLocation;
			this.ExtraData = extraData;
		}

		public static Order Deserialize(World world, BinaryReader r, ObjectCreator oc)
		{
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var o = (LegacyOrder)oc.DeserializeProto(r.BaseStream);

						if (world == null)
							return new Order(o.OrderString, null, null, o.TargetLocation, o.TargetString, o.Queued, o.ExtraLocation, o.ExtraData);

						Actor subject, targetActor;
						if (!TryGetActorFromUInt(world, o.SubjectID, out subject) || !TryGetActorFromUInt(world, o.TargetActorID, out targetActor))
							return null;

						return new Order(o.OrderString, subject, targetActor, o.TargetLocation, o.TargetString, o.Queued, o.ExtraLocation, o.ExtraData);
					}

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order(name, null, false) { IsImmediate = true, TargetString = data };
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

		static bool TryGetActorFromUInt(World world, uint aID, out Actor ret)
		{
			if (aID == 0xFFFFFFFF)
			{
				ret = null;
				return true;
			}
			else
			{
				foreach (var a in world.Actors.Where(x => x.ActorID == aID))
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
		public static Order Chat(bool team, string text)
		{
			return new Order(team ? "TeamChat" : "Chat", null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order HandshakeResponse(string text)
		{
			return new Order("HandshakeResponse", null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order Pong(string pingTime)
		{
			return new Order("Pong", null, false) { IsImmediate = true, TargetString = pingTime };
		}

		public static Order PauseGame(bool paused)
		{
			return new Order("PauseGame", null, false) { TargetString = paused ? "Pause" : "UnPause" };
		}

		public static Order Command(string text)
		{
			return new Order("Command", null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order StartProduction(Actor subject, string item, int count)
		{
			return new Order("StartProduction", subject, false) { ExtraData = (uint)count, TargetString = item };
		}

		public static Order PauseProduction(Actor subject, string item, bool pause)
		{
			return new Order("PauseProduction", subject, false) { ExtraData = pause ? 1u : 0u, TargetString = item };
		}

		public static Order CancelProduction(Actor subject, string item, int count)
		{
			return new Order("CancelProduction", subject, false) { ExtraData = (uint)count, TargetString = item };
		}

		// For scripting special powers
		public Order()
			: this(null, null, null, CPos.Zero, null, false, CPos.Zero, 0) { }

		public Order(string orderString, Actor subject, bool queued)
			: this(orderString, subject, null, CPos.Zero, null, queued, CPos.Zero, 0) { }

		public Order(string orderstring, Order order)
			: this(orderstring, order.Subject, order.TargetActor, order.TargetLocation,
				   order.TargetString, order.Queued, order.ExtraLocation, order.ExtraData) { }

		public byte[] Serialize(ObjectCreator oc)
		{
			if (IsImmediate)
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
				/*
				 * Format:
				 * u8: orderID.
				 * 0xFF: Full serialized order.
				 * varies: rest of order.
				 */
				default:
					// TODO: specific serializers for specific orders.
					{
						var ret = new MemoryStream();
						var w = new BinaryWriter(ret);
						w.Write((byte)0xFF);

						var o = new LegacyOrder()
						{
							OrderString = OrderString,
							SubjectID = UIntFromActor(Subject),
							TargetActorID = UIntFromActor(TargetActor),
							TargetLocation = TargetLocation,
							TargetString = TargetString,
							Queued = Queued,
							ExtraLocation = ExtraLocation,
							ExtraData = ExtraData
						};

						oc.SerializeProto(ret, o);

						return ret.ToArray();
					}
			}
		}

		public override string ToString()
		{
			return ("OrderString: \"{0}\" \n\t Subject: \"{1}\". \n\t TargetActor: \"{2}\" \n\t TargetLocation: {3}." +
				"\n\t TargetString: \"{4}\".\n\t IsImmediate: {5}.\n\t Player(PlayerName): {6}\n").F(
				OrderString, Subject, TargetActor != null ? TargetActor.Info.Name : null, TargetLocation, TargetString, IsImmediate, Player != null ? Player.PlayerName : null);
		}
	}
}
