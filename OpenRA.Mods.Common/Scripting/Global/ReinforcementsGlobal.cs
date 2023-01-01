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

using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Reinforcements")]
	public class ReinforcementsGlobal : ScriptGlobal
	{
		public ReinforcementsGlobal(ScriptContext context)
			: base(context)
		{
		}

		Actor CreateActor(Player owner, string actorType, bool addToWorld, CPos? entryLocation = null, CPos? nextLocation = null)
		{
			if (!Context.World.Map.Rules.Actors.TryGetValue(actorType, out var ai))
				throw new LuaException($"Unknown actor type '{actorType}'");

			var initDict = new TypeDictionary
			{
				new OwnerInit(owner)
			};

			if (entryLocation.HasValue)
			{
				initDict.Add(new LocationInit(entryLocation.Value));

				var pi = ai.TraitInfoOrDefault<AircraftInfo>();
				if (pi != null)
					initDict.Add(new CenterPositionInit(owner.World.Map.CenterOfCell(entryLocation.Value) + new WVec(0, 0, pi.CruiseAltitude.Length)));
			}

			if (entryLocation.HasValue && nextLocation.HasValue)
			{
				var facing = Context.World.Map.FacingBetween(CPos.Zero, CPos.Zero + (nextLocation.Value - entryLocation.Value), WAngle.Zero);
				initDict.Add(new FacingInit(facing));
			}

			return Context.World.CreateActor(addToWorld, actorType, initDict);
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
			"while the last one will be their destination. If actionFunc is given, " +
			"it will be executed once a unit has reached its destination. actionFunc " +
			"will be called as actionFunc(Actor actor). " +
			"Returns a table containing the deployed units.")]
		public Actor[] Reinforce(Player owner, string[] actorTypes, CPos[] entryPath, int interval = 25, LuaFunction actionFunc = null)
		{
			var actors = new List<Actor>();
			for (var i = 0; i < actorTypes.Length; i++)
			{
				var af = actionFunc != null ? (LuaFunction)actionFunc.CopyReference() : null;
				var actor = CreateActor(owner, actorTypes[i], false, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
				actors.Add(actor);

				var actionDelay = i * interval;
				Activity queuedActivity = null;
				if (af != null)
				{
					queuedActivity = new CallFunc(() =>
					{
						using (af)
						using (var a = actor.ToLuaValue(Context))
							af.Call(a);
					});
				}

				// We need to exclude the spawn location from the movement path
				var path = entryPath.Skip(1).ToArray();

				Context.World.AddFrameEndTask(w => w.Add(new SpawnActorEffect(actor, actionDelay, path, queuedActivity)));
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
			"actionFunc(Actor transport, Actor[] cargo). exitFunc will be called as exitFunc(Actor transport). " +
			"dropRange determines how many cells away the transport will try to land " +
			"if the actual destination is blocked (if the transport is an aircraft). " +
			"Returns a table in which the first value is the transport, " +
			"and the second a table containing the deployed units.")]
		public LuaTable ReinforceWithTransport(Player owner, string actorType, string[] cargoTypes, CPos[] entryPath, CPos[] exitPath = null,
			LuaFunction actionFunc = null, LuaFunction exitFunc = null, int dropRange = 3)
		{
			var transport = CreateActor(owner, actorType, true, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
			var cargo = transport.TraitOrDefault<Cargo>();

			var passengers = new List<Actor>();
			if (cargo != null && cargoTypes != null)
			{
				foreach (var cargoType in cargoTypes)
				{
					var passenger = CreateActor(owner, cargoType, false, entryPath[0]);
					passengers.Add(passenger);
					cargo.Load(transport, passenger);
				}
			}

			for (var i = 1; i < entryPath.Length; i++)
				Move(transport, entryPath[i]);

			if (actionFunc != null)
			{
				var af = (LuaFunction)actionFunc.CopyReference();
				transport.QueueActivity(new CallFunc(() =>
				{
					using (af)
					using (LuaValue t = transport.ToLuaValue(Context), p = passengers.ToArray().ToLuaValue(Context))
						af.Call(t, p);
				}));
			}
			else
			{
				var aircraft = transport.TraitOrDefault<Aircraft>();

				// Scripted cargo aircraft must turn to default position before unloading.
				// TODO: pass facing through UnloadCargo instead.
				if (aircraft != null)
					transport.QueueActivity(new Land(transport, Target.FromCell(transport.World, entryPath.Last()), WDist.FromCells(dropRange), aircraft.Info.InitialFacing));

				if (cargo != null)
					transport.QueueActivity(new UnloadCargo(transport, WDist.FromCells(dropRange)));
			}

			if (exitFunc != null)
			{
				var ef = (LuaFunction)exitFunc.CopyReference();
				transport.QueueActivity(new CallFunc(() =>
				{
					using (ef)
					using (var t = transport.ToLuaValue(Context))
						ef.Call(t);
				}));
			}
			else if (exitPath != null)
			{
				foreach (var wpt in exitPath)
					Move(transport, wpt);

				transport.QueueActivity(new RemoveSelf());
			}

			var ret = Context.CreateTable();
			using (LuaValue
				tKey = 1,
				tValue = transport.ToLuaValue(Context),
				pKey = 2,
				pValue = passengers.ToArray().ToLuaValue(Context))
			{
				ret.Add(tKey, tValue);
				ret.Add(pKey, pValue);
			}

			return ret;
		}
	}
}
