#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA
{
	[Flags]
	enum OrderFields : byte
	{
		None = 0,
		TargetActor    = 1 << 0,
		TargetLocation = 1 << 1,
		TargetString   = 1 << 2,
		Queued         = 1 << 3,
		ExtraLocation  = 1 << 4,
		ExtraData      = 1 << 5,
	}

	static class OrderFieldsExts
	{
		public static bool HasField(this OrderFields of, OrderFields f)
		{
			return (of & f) != 0;
		}
	}

	public sealed class Order
	{
		public readonly OrderCode ID;
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

		Order(OrderCode orderID, Actor subject,
					Actor targetActor, CPos targetLocation, string targetString, bool queued, CPos extraLocation, uint extraData)
		{
			this.ID = orderID;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Queued = queued;
			this.ExtraLocation = extraLocation;
			this.ExtraData = extraData;
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var order = (OrderCode)r.ReadByte();
						var subjectId = r.ReadUInt32();
						var flags = (OrderFields)r.ReadByte();

						var targetActorId = flags.HasField(OrderFields.TargetActor) ? r.ReadUInt32() : 0xffffffff;
						var targetLocation = (CPos)(flags.HasField(OrderFields.TargetLocation) ? r.ReadInt2() : int2.Zero);
						var targetString = flags.HasField(OrderFields.TargetString) ? r.ReadString() : null;
						var queued = flags.HasField(OrderFields.Queued);
						var extraLocation = (CPos)(flags.HasField(OrderFields.ExtraLocation) ? r.ReadInt2() : int2.Zero);
						var extraData = flags.HasField(OrderFields.ExtraData) ? r.ReadUInt32() : 0;

						if (world == null)
							return new Order(order, null, null, targetLocation, targetString, queued, extraLocation, extraData);

						Actor subject, targetActor;
						if (!TryGetActorFromUInt(world, subjectId, out subject) || !TryGetActorFromUInt(world, targetActorId, out targetActor))
							return null;

						return new Order(order, subject, targetActor, targetLocation, targetString, queued, extraLocation, extraData);
					}

				case 0xfe:
					{
						var name = (OrderCode)r.ReadByte();
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
			return new Order(team ? OrderCode.TeamChat : OrderCode.Chat, null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order HandshakeResponse(string text)
		{
			return new Order(OrderCode.HandshakeResponse, null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order Pong(string pingTime)
		{
			return new Order(OrderCode.Pong, null, false) { IsImmediate = true, TargetString = pingTime };
		}

		public static Order PauseGame(bool paused)
		{
			return new Order(OrderCode.PauseGame, null, false) { TargetString = paused ? "Pause" : "UnPause" };
		}

		public static Order Command(string text)
		{
			return new Order(OrderCode.Command, null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order StartProduction(Actor subject, string item, int count)
		{
			return new Order(OrderCode.StartProduction, subject, false) { ExtraData = (uint)count, TargetString = item };
		}

		public static Order PauseProduction(Actor subject, string item, bool pause)
		{
			return new Order(OrderCode.PauseProduction, subject, false) { ExtraData = pause ? 1u : 0u, TargetString = item };
		}

		public static Order CancelProduction(Actor subject, string item, int count)
		{
			return new Order(OrderCode.CancelProduction, subject, false) { ExtraData = (uint)count, TargetString = item };
		}

		// For scripting special powers
		public Order()
			: this(OrderCode.None, null, null, CPos.Zero, null, false, CPos.Zero, 0) { }

		public Order(OrderCode orderID, Actor subject, bool queued)
			: this(orderID, subject, null, CPos.Zero, null, queued, CPos.Zero, 0) { }

		public Order(OrderCode orderID, Order order)
			: this(orderID, order.Subject, order.TargetActor, order.TargetLocation,
				   order.TargetString, order.Queued, order.ExtraLocation, order.ExtraData) { }

		public byte[] Serialize()
		{
			if (IsImmediate)
			{
				var ret = new MemoryStream();
				var w = new BinaryWriter(ret);
				w.Write((byte)0xfe);
				w.Write((byte)ID);
				w.Write(TargetString);
				return ret.ToArray();
			}

			switch (ID)
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
						w.Write((byte)ID);
						w.Write(UIntFromActor(Subject));

						OrderFields fields = 0;
						if (TargetActor != null) fields |= OrderFields.TargetActor;
						if (TargetLocation != CPos.Zero) fields |= OrderFields.TargetLocation;
						if (TargetString != null) fields |= OrderFields.TargetString;
						if (Queued) fields |= OrderFields.Queued;
						if (ExtraLocation != CPos.Zero) fields |= OrderFields.ExtraLocation;
						if (ExtraData != 0) fields |= OrderFields.ExtraData;

						w.Write((byte)fields);

						if (TargetActor != null)
							w.Write(UIntFromActor(TargetActor));
						if (TargetLocation != CPos.Zero)
							w.Write(TargetLocation);
						if (TargetString != null)
							w.Write(TargetString);
						if (ExtraLocation != CPos.Zero)
							w.Write(ExtraLocation);
						if (ExtraData != 0)
							w.Write(ExtraData);

						return ret.ToArray();
					}
			}
		}

		public override string ToString()
		{
			return ("OrderString: \"{0}\" \n\t Subject: \"{1}\". \n\t TargetActor: \"{2}\" \n\t TargetLocation: {3}." +
				"\n\t TargetString: \"{4}\".\n\t IsImmediate: {5}.\n\t Player(PlayerName): {6}\n").F(
				ID, Subject, TargetActor != null ? TargetActor.Info.Name : null, TargetLocation, TargetString, IsImmediate, Player != null ? Player.PlayerName : null);
		}

		public static OrderCode CodeFromString(string orderString)
		{
			if (string.IsNullOrEmpty(orderString))
				return OrderCode.None;
			return Enum<OrderCode>.Parse(orderString);
		}
	}
}
