#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Scripting;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Reinforcements")]
	public class ReinforcementsGlobal : ScriptGlobal
	{
		public ReinforcementsGlobal(ScriptContext context) : base(context) { }

		Actor CreateActor(Player owner, string actorType, bool addToWorld, CPos? entryLocation = null, CPos? nextLocation = null)
		{
			ActorInfo ai;
			if (!context.World.Map.Rules.Actors.TryGetValue(actorType, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(actorType));

			var initDict = new TypeDictionary();

			initDict.Add(new OwnerInit(owner));

			if (entryLocation.HasValue)
			{
				var pi = ai.Traits.GetOrDefault<AircraftInfo>();
				initDict.Add(new CenterPositionInit(owner.World.Map.CenterOfCell(entryLocation.Value) + new WVec(0, 0, pi != null ? pi.CruiseAltitude.Range : 0)));
				initDict.Add(new LocationInit(entryLocation.Value));
			}

			if (entryLocation.HasValue && nextLocation.HasValue)
				initDict.Add(new FacingInit(context.World.Map.FacingBetween(CPos.Zero, CPos.Zero + (nextLocation.Value - entryLocation.Value), 0)));

			var actor = context.World.CreateActor(addToWorld, actorType, initDict);

			return actor;
		}

		void Move(Actor actor, CPos dest)
		{
			if (actor.HasTrait<Aircraft>())
			{
				if (actor.HasTrait<Helicopter>())
					actor.QueueActivity(new HeliFly(actor, Target.FromCell(actor.World, dest)));
				else
					actor.QueueActivity(new Fly(actor, Target.FromCell(actor.World, dest)));
			}
			else
			{
				actor.QueueActivity(new Move.Move(dest, 2));
			}
		}

		[Desc("Send reinforcements consisting of multiple units. Supports ground-based, naval and air units. " +
		      "The first member of the 'entryPath' array will be the units' spawnpoint, " +
		      "while the last one will be their destination.  If 'actionFunc' is given, " +
		      "it will be executed once a unit has reached its destination. 'actionFunc' " +
		      "will be called as 'actionFunc(Actor actor)'")]
		public LuaTable Reinforce(Player owner, LuaTable actorTypes, LuaTable entryPath, int interval = 25, LuaFunction actionFunc = null)
		{
			var actors = new List<Actor>();
			for (var i = 1; i <= actorTypes.Count; i++)
			{
				string actorType;
				if (!(actorTypes[i].TryGetClrValue<String>(out actorType)))
					throw new LuaException("Invalid data in actorTypes array");

				CPos entry, next = new CPos();
				if (!(entryPath[1].TryGetClrValue<CPos>(out entry)
					&& (entryPath.Count < 2 || entryPath[2].TryGetClrValue<CPos>(out next))))
					throw new LuaException("Invalid data in entryPath array");

				var actor = CreateActor(owner, actorType, false, entry, entryPath.Count > 1 ? next : (CPos?)null);
				actors.Add(actor);

				var ep = entryPath.CopyReference() as LuaTable;
				var af = actionFunc != null ? actionFunc.CopyReference() as LuaFunction : null;

				var actionDelay = (i - 1) * interval;
				Action actorAction = () =>
				{
					context.World.Add(actor);
					for (var j = 2; j <= ep.Count; j++)
					{
						CPos wpt;
						if (!(ep[j].TryGetClrValue<CPos>(out wpt)))
							throw new LuaException("Invalid data in entryPath array");

						Move(actor, wpt);
					}
					ep.Dispose();

					if (af != null)
					    actor.QueueActivity(new CallFunc(() =>
						{
							af.Call(actor.ToLuaValue(context));
							af.Dispose();
						}));
				};

				context.World.AddFrameEndTask(w => w.Add(new DelayedAction(actionDelay, actorAction)));
			}
			return actors.Select(a => a.ToLuaValue(context)).ToLuaTable(context);
		}

		[Desc("Send reinforcements in a transport. A transport can be a ground unit (APC etc.), ships and aircraft. " +
		      "The first member of the 'entryPath' array will be the spawnpoint for the transport, " +
		      "while the last one will be its destination. The last member of the 'exitPath' array " +
		      "is be the place where the transport will be removed from the game. When the transport " +
		      "has reached the destination, it will unload its cargo unless a custom 'actionFunc' has " +
		      "been supplied. Afterwards, the transport will follow the 'exitPath' and leave the map, " +
		      "unless a custom 'exitFunc' has been supplied. 'actionFunc' will be called as " +
		      "'actionFunc(Actor transport, Actor[] cargo). 'exitFunc' will be called as 'exitFunc(Actor transport)'.")]
		public LuaTable ReinforceWithTransport(Player owner, string actorType, LuaTable cargoTypes, LuaTable entryPath, LuaTable exitPath = null,
			LuaFunction actionFunc = null, LuaFunction exitFunc = null)
		{
			CPos entry, next = new CPos();
			if (!(entryPath[1].TryGetClrValue<CPos>(out entry)
				&& (entryPath.Count < 2 || entryPath[2].TryGetClrValue<CPos>(out next))))
				throw new LuaException("Invalid data in entryPath array");

			var transport = CreateActor(owner, actorType, true, entry, entryPath.Count > 1 ? next : (CPos?)null);
			var cargo = transport.TraitOrDefault<Cargo>();

			var passengers = context.CreateTable();

			if (cargo != null && cargoTypes != null)
			{
				for (var i = 1; i <= cargoTypes.Count; i++)
				{
					string cargoType;
					if (!(cargoTypes [i].TryGetClrValue<String>(out cargoType)))
						throw new LuaException("Invalid data in cargoTypes array");

					var passenger = CreateActor(owner, cargoType, false);
					passengers.Add(passengers.Count + 1, passenger.ToLuaValue(context));
					cargo.Load(transport, passenger);
				}
			}

			for (var i = 2; i <= entryPath.Count; i++)
			{
				CPos wpt;
				if (!(entryPath[i].TryGetClrValue<CPos>(out wpt)))
					throw new LuaException("Invalid data in entryPath array");

				Move(transport, wpt);
			}

			if (actionFunc != null)
			{
				var af = actionFunc.CopyReference() as LuaFunction;
				transport.QueueActivity(new CallFunc(() =>
				{
					af.Call(transport.ToLuaValue(context), passengers);
					af.Dispose();
				}));
			}
			else
			{
				var heli = transport.TraitOrDefault<Helicopter>();
				if (heli != null)
				{
					transport.QueueActivity(new Turn(heli.Info.InitialFacing));
					transport.QueueActivity(new HeliLand(true));
					transport.QueueActivity(new Wait(15));
				}
				if (cargo != null)
				{
					transport.QueueActivity(new UnloadCargo(transport, true));
					transport.QueueActivity(new WaitFor(() => cargo.IsEmpty(transport)));
				}
				transport.QueueActivity(new Wait(heli != null ? 50 : 25));
			}

			if (exitFunc != null)
			{
				var ef = exitFunc.CopyReference() as LuaFunction;
				transport.QueueActivity(new CallFunc(() =>
				{
					ef.Call(transport.ToLuaValue(context));
					ef.Dispose();
				}));
			}
			else if (exitPath != null)
			{
				for (var i = 1; i <= exitPath.Count; i++)
				{
					CPos wpt;
					if (!(exitPath[i].TryGetClrValue<CPos>(out wpt)))
						throw new LuaException("Invalid data in exitPath array.");

					Move(transport, wpt);
				}
				transport.QueueActivity(new RemoveSelf());
			}

			var ret = context.CreateTable();
			ret.Add(1, transport.ToLuaValue(context));
			ret.Add(2, passengers);
			return ret;
		}
	}
}