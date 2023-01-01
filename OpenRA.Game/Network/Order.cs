#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Traits;

namespace OpenRA
{
	public enum OrderType : byte
	{
		Ack = 0x10,
		Ping = 0x20,
		SyncHash = 0x65,
		TickScale = 0x76,
		Disconnect = 0xBF,
		Handshake = 0xFE,
		Fields = 0xFF
	}

	[Flags]
	enum OrderFields : short
	{
		None = 0x0,
		Target = 0x01,
		ExtraActors = 0x02,
		TargetString = 0x04,
		Queued = 0x08,
		ExtraLocation = 0x10,
		ExtraData = 0x20,
		TargetIsCell = 0x40,
		Subject = 0x80,
		Grouped = 0x100
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
		// Length of orders with type OrderType.SyncHash
		public const int SyncHashOrderLength = 13;

		// Length of orders with type OrderType.Disconnect
		public const int DisconnectOrderLength = 5;

		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly bool Queued;
		public ref readonly Target Target => ref target;
		public readonly Actor[] GroupedActors;

		public string TargetString;
		public CPos ExtraLocation;
		public Actor[] ExtraActors;
		public uint ExtraData;
		public bool IsImmediate;
		public OrderType Type = OrderType.Fields;

		public bool SuppressVisualFeedback;
		public ref readonly Target VisualFeedbackTarget => ref visualFeedbackTarget;

		public Player Player => Subject?.Owner;

		readonly Target target;
		readonly Target visualFeedbackTarget;

		Order(string orderString, Actor subject, in Target target, string targetString, bool queued, Actor[] extraActors, CPos extraLocation, uint extraData, Actor[] groupedActors = null)
		{
			OrderString = orderString ?? "";
			Subject = subject;
			this.target = target;
			TargetString = targetString;
			Queued = queued;
			ExtraActors = extraActors;
			ExtraLocation = extraLocation;
			ExtraData = extraData;
			GroupedActors = groupedActors;
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			try
			{
				var type = (OrderType)r.ReadByte();
				switch (type)
				{
					case OrderType.Fields:
					{
						var order = r.ReadString();
						var flags = (OrderFields)r.ReadInt16();

						Actor subject = null;
						if (flags.HasField(OrderFields.Subject))
						{
							var subjectId = r.ReadUInt32();
							if (world != null)
								TryGetActorFromUInt(world, subjectId, out subject);
						}

						var target = Target.Invalid;
						if (flags.HasField(OrderFields.Target))
						{
							switch ((TargetType)r.ReadByte())
							{
								case TargetType.Actor:
								{
									var actorID = r.ReadUInt32();
									var actorGeneration = r.ReadInt32();
									if (world != null && TryGetActorFromUInt(world, actorID, out var targetActor))
										target = Target.FromSerializedActor(targetActor, actorGeneration);

									break;
								}

								case TargetType.FrozenActor:
								{
									var playerActorID = r.ReadUInt32();
									var frozenActorID = r.ReadUInt32();

									if (world == null || !TryGetActorFromUInt(world, playerActorID, out var playerActor))
										break;

									if (playerActor.Owner.FrozenActorLayer == null)
										break;

									var frozen = playerActor.Owner.FrozenActorLayer.FromID(frozenActorID);
									if (frozen != null)
										target = Target.FromFrozenActor(frozen);

									break;
								}

								case TargetType.Terrain:
								{
									if (flags.HasField(OrderFields.TargetIsCell))
									{
										var cell = new CPos(r.ReadInt32());
										var subCell = (SubCell)r.ReadByte();
										if (world != null)
											target = Target.FromCell(world, cell, subCell);
									}
									else
									{
										var pos = new WPos(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
										target = Target.FromPos(pos);
									}

									break;
								}
							}
						}

						var targetString = flags.HasField(OrderFields.TargetString) ? r.ReadString() : null;
						var queued = flags.HasField(OrderFields.Queued);

						Actor[] extraActors = null;
						if (flags.HasField(OrderFields.ExtraActors))
						{
							var count = r.ReadInt32();
							if (world != null)
								extraActors = Exts.MakeArray(count, _ => world.GetActorById(r.ReadUInt32()));
							else
								r.ReadBytes(4 * count);
						}

						var extraLocation = flags.HasField(OrderFields.ExtraLocation) ? new CPos(r.ReadInt32()) : CPos.Zero;
						var extraData = flags.HasField(OrderFields.ExtraData) ? r.ReadUInt32() : 0;

						Actor[] groupedActors = null;
						if (flags.HasField(OrderFields.Grouped))
						{
							var count = r.ReadInt32();
							if (world != null)
								groupedActors = Exts.MakeArray(count, _ => world.GetActorById(r.ReadUInt32()));
							else
								r.ReadBytes(4 * count);
						}

						if (world == null)
							return new Order(order, null, target, targetString, queued, extraActors, extraLocation, extraData, groupedActors);

						if (subject == null && flags.HasField(OrderFields.Subject))
							return null;

						return new Order(order, subject, target, targetString, queued, extraActors, extraLocation, extraData, groupedActors);
					}

					case OrderType.Handshake:
					{
						var name = r.ReadString();
						var targetString = r.ReadString();

						return new Order(name, null, false) { Type = OrderType.Handshake, TargetString = targetString };
					}

					default:
					{
						Log.Write("debug", "Received unknown order with type {0}", type);
						return null;
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Caught exception while processing order");
				Log.Write("debug", e.ToString());

				// HACK: this can hopefully go away in the future
				TextNotificationsManager.Debug("Ignoring malformed order that would have crashed the game");
				TextNotificationsManager.Debug("Please file a bug report and include the replay from this match");

				return null;
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
		public static Order Chat(string text, uint teamNumber = 0)
		{
			return new Order("Chat", null, false) { IsImmediate = true, TargetString = text, ExtraData = teamNumber };
		}

		public static Order FromTargetString(string order, string targetString, bool isImmediate)
		{
			return new Order(order, null, false) { IsImmediate = isImmediate, TargetString = targetString };
		}

		public static Order FromGroupedOrder(Order grouped, Actor subject)
		{
			return new Order(grouped.OrderString, subject, grouped.Target, grouped.TargetString, grouped.Queued, grouped.ExtraActors, grouped.ExtraLocation, grouped.ExtraData);
		}

		public static Order Command(string text)
		{
			return new Order("Command", null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order StartProduction(Actor subject, string item, int count, bool queued = true)
		{
			return new Order("StartProduction", subject, queued) { ExtraData = (uint)count, TargetString = item };
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
			: this(null, null, Target.Invalid, null, false, null, CPos.Zero, 0) { }

		public Order(string orderString, Actor subject, bool queued, Actor[] extraActors = null, Actor[] groupedActors = null)
			: this(orderString, subject, Target.Invalid, null, queued, extraActors, CPos.Zero, 0, groupedActors) { }

		public Order(string orderString, Actor subject, in Target target, bool queued, Actor[] extraActors = null, Actor[] groupedActors = null)
			: this(orderString, subject, target, null, queued, extraActors, CPos.Zero, 0, groupedActors) { }

		public Order(string orderString, Actor subject, Target target, Target visualFeedbackTarget, bool queued)
			: this(orderString, subject, target, null, queued, null, CPos.Zero, 0, null)
		{
			this.visualFeedbackTarget = visualFeedbackTarget;
		}

		public byte[] Serialize()
		{
			var minLength = 1 + OrderString.Length + 1;
			if (Type == OrderType.Handshake)
				minLength += TargetString.Length + 1;
			else if (Type == OrderType.Fields)
				minLength += 4 + 2 + 13 + (TargetString != null ? TargetString.Length + 1 : 0) + 4 + 4 + 4;

			if (ExtraActors != null)
				minLength += ExtraActors.Length * 4;

			// ProtocolVersion.Orders and the associated documentation MUST be updated if the serialized format changes
			var ret = new MemoryStream(minLength);
			var w = new BinaryWriter(ret);

			w.Write((byte)Type);
			w.Write(OrderString);

			switch (Type)
			{
				case OrderType.Handshake:
				{
					// Changing the Handshake order format will break cross-version switching
					// Don't do this unless you really have to!
					w.Write(TargetString ?? "");

					break;
				}

				case OrderType.Fields:
				{
					var fields = OrderFields.None;
					if (Subject != null)
						fields |= OrderFields.Subject;

					if (TargetString != null)
						fields |= OrderFields.TargetString;

					if (ExtraData != 0)
						fields |= OrderFields.ExtraData;

					if (Target.SerializableType != TargetType.Invalid)
						fields |= OrderFields.Target;

					if (Queued)
						fields |= OrderFields.Queued;

					if (GroupedActors != null)
						fields |= OrderFields.Grouped;

					if (ExtraActors != null)
						fields |= OrderFields.ExtraActors;

					if (ExtraLocation != CPos.Zero)
						fields |= OrderFields.ExtraLocation;

					if (Target.SerializableCell != null)
						fields |= OrderFields.TargetIsCell;

					w.Write((short)fields);

					if (fields.HasField(OrderFields.Subject))
						w.Write(UIntFromActor(Subject));

					if (fields.HasField(OrderFields.Target))
					{
						w.Write((byte)Target.SerializableType);
						switch (Target.SerializableType)
						{
							case TargetType.Actor:
								w.Write(UIntFromActor(Target.SerializableActor));
								w.Write(Target.SerializableGeneration);
								break;
							case TargetType.FrozenActor:
								w.Write(Target.FrozenActor.Viewer.PlayerActor.ActorID);
								w.Write(Target.FrozenActor.ID);
								break;
							case TargetType.Terrain:
								if (fields.HasField(OrderFields.TargetIsCell))
								{
									w.Write(Target.SerializableCell.Value.Bits);
									w.Write((byte)Target.SerializableSubCell);
								}
								else
								{
									w.Write(Target.SerializablePos.X);
									w.Write(Target.SerializablePos.Y);
									w.Write(Target.SerializablePos.Z);
								}

								break;
						}
					}

					if (fields.HasField(OrderFields.TargetString))
						w.Write(TargetString);

					if (fields.HasField(OrderFields.ExtraActors))
					{
						w.Write(ExtraActors.Length);
						foreach (var a in ExtraActors)
							w.Write(UIntFromActor(a));
					}

					if (fields.HasField(OrderFields.ExtraLocation))
						w.Write(ExtraLocation.Bits);

					if (fields.HasField(OrderFields.ExtraData))
						w.Write(ExtraData);

					if (fields.HasField(OrderFields.Grouped))
					{
						w.Write(GroupedActors.Length);
						foreach (var a in GroupedActors)
							w.Write(UIntFromActor(a));
					}

					break;
				}

				default:
					throw new InvalidDataException($"Cannot serialize order type {Type}");
			}

			return ret.ToArray();
		}

		public override string ToString()
		{
			return $"OrderString: \"{OrderString}\" \n\t Type: \"{Type}\".  \n\t Subject: \"{Subject}\". \n\t Target: \"{Target}\"." +
					$"\n\t TargetString: \"{TargetString}\".\n\t IsImmediate: {IsImmediate}.\n\t Player(PlayerName): {Player?.PlayerName}\n";
		}
	}
}
