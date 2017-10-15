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
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA
{
	[Flags]
	enum OrderFields : byte
	{
		Target = 0x01,
		TargetString = 0x04,
		Queued = 0x08,
		ExtraLocation = 0x10,
		ExtraData = 0x20
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
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly bool Queued;
		public readonly Target Target;
		public string TargetString;
		public CPos ExtraLocation;
		public uint ExtraData;
		public bool IsImmediate;
		public bool SuppressVisualFeedback;
		public Actor VisualFeedbackTarget;

		/// <summary>
		/// DEPRECATED: Use Target instead.
		/// </summary>
		public Actor TargetActor { get { return Target.SerializableActor; } }

		/// <summary>
		/// DEPRECATED: Use Target instead.
		/// </summary>
		public CPos TargetLocation
		{
			get
			{
				return Target.SerializableCell.HasValue ? Target.SerializableCell.Value : CPos.Zero;
			}
		}

		public Player Player { get { return Subject != null ? Subject.Owner : null; } }

		Order(string orderString, Actor subject, Target target, string targetString, bool queued, CPos extraLocation, uint extraData)
		{
			OrderString = orderString;
			Subject = subject;
			Target = target;
			TargetString = targetString;
			Queued = queued;
			ExtraLocation = extraLocation;

			// TODO: remove FrozenActor ID after the various ResolveOrders that rely on it are updated 
			ExtraData = target.Type == TargetType.FrozenActor ? target.FrozenActor.ID : extraData;
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			var magic = r.ReadByte();
			switch (magic)
			{
				case 0xFF:
					{
						var order = r.ReadString();
						var subjectId = r.ReadUInt32();
						var flags = (OrderFields)r.ReadByte();

						Actor subject = null;
						if (world != null)
							TryGetActorFromUInt(world, subjectId, out subject);

						var target = Target.Invalid;
						if (flags.HasField(OrderFields.Target))
						{
							switch ((TargetType)r.ReadByte())
							{
								case TargetType.Actor:
									{
										Actor targetActor;
										if (world != null && TryGetActorFromUInt(world, r.ReadUInt32(), out targetActor))
											target = Target.FromActor(targetActor);
										break;
									}

								case TargetType.FrozenActor:
									{
										Actor playerActor;
										if (world == null || !TryGetActorFromUInt(world, r.ReadUInt32(), out playerActor))
											break;

										var frozenLayer = playerActor.TraitOrDefault<FrozenActorLayer>();
										if (frozenLayer == null)
											break;

										var frozen = frozenLayer.FromID(r.ReadUInt32());
										if (frozen != null)
											target = Target.FromFrozenActor(frozen);

										break;
									}

								case TargetType.Terrain:
									{
										if (world != null)
											target = Target.FromCell(world, (CPos)r.ReadInt2());

										break;
									}
							}
						}

						var targetString = flags.HasField(OrderFields.TargetString) ? r.ReadString() : null;
						var queued = flags.HasField(OrderFields.Queued);
						var extraLocation = (CPos)(flags.HasField(OrderFields.ExtraLocation) ? r.ReadInt2() : int2.Zero);
						var extraData = flags.HasField(OrderFields.ExtraData) ? r.ReadUInt32() : 0;

						if (world == null)
							return new Order(order, null, target, targetString, queued, extraLocation, extraData);

						if (subject == null)
							return null;

						return new Order(order, subject, target, targetString, queued, extraLocation, extraData);
					}

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order(name, null, false) { IsImmediate = true, TargetString = data };
					}

				default:
					{
						Log.Write("debug", "Received unknown order with magic {0}", magic);
						return null;
					}
			}
		}

		static uint UIntFromActor(Actor a)
		{
			if (a == null) return uint.MaxValue;
			return a.ActorID;
		}

		static bool TryGetActorFromUInt(World world, uint aID, out Actor ret)
		{
			if (aID == uint.MaxValue)
			{
				ret = null;
				return true;
			}

			ret = world.GetActorById(aID);
			return ret != null;
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
			: this(null, null, Target.Invalid, null, false, CPos.Zero, 0) { }

		public Order(string orderString, Actor subject, bool queued)
			: this(orderString, subject, Target.Invalid, null, queued, CPos.Zero, 0) { }

		public Order(string orderString, Actor subject, Target target, bool queued)
			: this(orderString, subject, target, null, queued, CPos.Zero, 0) { }

		public Order(string orderstring, Order order)
			: this(orderstring, order.Subject, order.Target,
				   order.TargetString, order.Queued, order.ExtraLocation, order.ExtraData) { }

		public byte[] Serialize()
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
					{
						var ret = new MemoryStream();
						var w = new BinaryWriter(ret);
						w.Write((byte)0xFF);
						w.Write(OrderString);
						w.Write(UIntFromActor(Subject));

						OrderFields fields = 0;
						if (Target.SerializableType != TargetType.Invalid) fields |= OrderFields.Target;
						if (TargetString != null) fields |= OrderFields.TargetString;
						if (Queued) fields |= OrderFields.Queued;
						if (ExtraLocation != CPos.Zero) fields |= OrderFields.ExtraLocation;
						if (ExtraData != 0) fields |= OrderFields.ExtraData;

						w.Write((byte)fields);

						if (Target.SerializableType != TargetType.Invalid)
							w.Write((byte)Target.Type);

						switch (Target.SerializableType)
						{
							case TargetType.Actor:
								w.Write(UIntFromActor(Target.SerializableActor));
								break;
							case TargetType.FrozenActor:
								w.Write(Target.FrozenActor.Owner.PlayerActor.ActorID);
								w.Write(Target.FrozenActor.ID);
								break;
							case TargetType.Terrain:
								// SerializableCell is guaranteed to be non-null if Type == TargetType.Terrain
								w.Write(Target.SerializableCell.Value);
								break;
						}

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
				OrderString, Subject, TargetActor != null ? TargetActor.Info.Name : null, TargetLocation, TargetString, IsImmediate, Player != null ? Player.PlayerName : null);
		}
	}
}
