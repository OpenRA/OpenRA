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
			var move = actor.TraitOrDefault<IMove>();
			if (move == null)
				return;

			actor.QueueActivity(move.MoveTo(dest, 2));
		}

		[Desc("Send reinforcements consisting of multiple units. Supports ground-based, naval and air units. " +
			"The first member of the entryPath array will be the units' spawnpoint, " +
			"while the last one will be their destination.  If actionFunc is given, " +
			"it will be executed once a unit has reached its destination. actionFunc " +
			"will be called as actionFunc(Actor actor)")]
		public Actor[] Reinforce(Player owner, string[] actorTypes, CPos[] entryPath, int interval = 25, LuaFunction actionFunc = null)
		{
			var actors = new List<Actor>();
			for (var i = 0; i < actorTypes.Length; i++)
			{
				var af = actionFunc != null ? actionFunc.CopyReference() as LuaFunction : null;
				var actor = CreateActor(owner, actorTypes[i], false, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
				actors.Add(actor);

				var actionDelay = i * interval;
				Action actorAction = () =>
				{
					context.World.Add(actor);
					for (var j = 1; j < entryPath.Length; j++)
						Move(actor, entryPath[j]);

					if (af != null)
					{
					    actor.QueueActivity(new CallFunc(() =>
						{
							af.Call(actor.ToLuaValue(context));
							af.Dispose();
						}));
					}
				};

				context.World.AddFrameEndTask(w => w.Add(new DelayedAction(actionDelay, actorAction)));
			}

			return actors.ToArray();
		}

		[Desc("Send reinforcements in a transport. A transport can be a ground unit (APC etc.), ships and aircraft. " +
			"The first member of the entryPath array will be the spawnpoint for the transport, " +
			"while the last one will be its destination. The last member of the exitPath array " +
			"is be the place where the transport will be removed from the game. When the transport " +
			"has reached the destination, it will unload its cargo unless a custom actionFunc has " +
			"been supplied. Afterwards, the transport will follow the exitPath and leave the map, " +
			"unless a custom exitFunc has been supplied. actionFunc will be called as " +
			"actionFunc(Actor transport, Actor[] cargo). exitFunc will be called as exitFunc(Actor transport).")]
		public LuaTable ReinforceWithTransport(Player owner, string actorType, string[] cargoTypes, CPos[] entryPath, CPos[] exitPath = null,
			LuaFunction actionFunc = null, LuaFunction exitFunc = null)
		{
			var transport = CreateActor(owner, actorType, true, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
			var cargo = transport.TraitOrDefault<Cargo>();

			var passengers = new List<Actor>();
			if (cargo != null && cargoTypes != null)
			{
				foreach (var cargoType in cargoTypes)
				{
					var passenger = CreateActor(owner, cargoType, false);
					passengers.Add(passenger);
					cargo.Load(transport, passenger);
				}
			}

			for (var i = 1; i < entryPath.Length; i++)
				Move(transport, entryPath[i]);

			if (actionFunc != null)
			{
				var af = actionFunc.CopyReference() as LuaFunction;
				transport.QueueActivity(new CallFunc(() =>
				{
					af.Call(transport.ToLuaValue(context), passengers.ToArray().ToLuaValue(context));
					af.Dispose();
				}));
			}
			else
			{
				var heli = transport.TraitOrDefault<Helicopter>();
				if (heli != null)
				{
					transport.QueueActivity(new Turn(transport, heli.Info.InitialFacing));
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
				foreach (var wpt in exitPath)
					Move(transport, wpt);

				transport.QueueActivity(new RemoveSelf());
			}

			var ret = context.CreateTable();
			ret.Add(1, transport.ToLuaValue(context));
			ret.Add(2, passengers.ToArray().ToLuaValue(context));
			return ret;
		}
	}
}