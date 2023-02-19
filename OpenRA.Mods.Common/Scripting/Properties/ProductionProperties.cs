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
using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Production")]
	public class ProductionProperties : ScriptActorProperties, Requires<ProductionInfo>
	{
		readonly Production[] productionTraits;

		public ProductionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			productionTraits = self.TraitsImplementing<Production>().ToArray();
		}

		[ScriptActorPropertyActivity]
		[Desc("Build a unit, ignoring the production queue. The activity will wait if the exit is blocked.",
			"If productionType is nil or unavailable, then an exit will be selected based on 'Buildable.BuildAtProductionType'.",
			"If 'Buildable.BuildAtProductionType' is not set either, a random exit will be selected.")]
		public void Produce(string actorType, string factionVariant = null, string productionType = null)
		{
			if (!Self.World.Map.Rules.Actors.TryGetValue(actorType, out var actorInfo))
				throw new LuaException($"Unknown actor type '{actorType}'");

			var bi = actorInfo.TraitInfo<BuildableInfo>();
			Self.QueueActivity(new WaitFor(() =>
			{
				// Go through all available traits and see which one successfully produces
				foreach (var p in productionTraits)
				{
					var type = productionType ?? bi.BuildAtProductionType;
					if (!string.IsNullOrEmpty(type) && !p.Info.Produces.Contains(type))
						continue;

					var inits = new TypeDictionary
					{
						new OwnerInit(Self.Owner),
						new FactionInit(factionVariant ?? BuildableInfo.GetInitialFaction(actorInfo, p.Faction))
					};

					if (p.Produce(Self, actorInfo, type, inits, 0))
						return true;
				}

				// We didn't produce anything, wait until we do
				return false;
			}));
		}
	}

	[ScriptPropertyGroup("Production")]
	public class RallyPointProperties : ScriptActorProperties, Requires<RallyPointInfo>
	{
		readonly RallyPoint rp;

		public RallyPointProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			rp = self.Trait<RallyPoint>();
		}

		[Desc("Query or set a factory's rally point.")]
		public CPos RallyPoint
		{
			get
			{
				if (rp.Path.Count > 0)
					return rp.Path.Last();

				var exit = Self.NearestExitOrDefault(Self.CenterPosition);
				if (exit != null)
					return Self.Location + exit.Info.ExitCell;

				return Self.Location;
			}
			set => rp.Path = new List<CPos> { value };
		}
	}

	[ScriptPropertyGroup("Production")]
	public class PrimaryBuildingProperties : ScriptActorProperties, Requires<PrimaryBuildingInfo>
	{
		readonly PrimaryBuilding pb;

		public PrimaryBuildingProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			pb = self.Trait<PrimaryBuilding>();
		}

		[Desc("Query or set the factory's primary building status.")]
		public bool IsPrimaryBuilding
		{
			get => pb.IsPrimary;
			set => pb.SetPrimaryProducer(Self, value);
		}
	}

	[ScriptPropertyGroup("Production")]
	public class ProductionQueueProperties : ScriptActorProperties, Requires<ProductionQueueInfo>, Requires<ScriptTriggersInfo>
	{
		readonly ProductionQueue[] queues;
		readonly ScriptTriggers triggers;

		public ProductionQueueProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			queues = self.TraitsImplementing<ProductionQueue>().Where(q => q.Enabled).ToArray();
			triggers = TriggerGlobal.GetScriptTriggers(self);
		}

		[Desc("Build the specified set of actors using a TD-style (per building) production queue. " +
			"The function will return true if production could be started, false otherwise. " +
			"If an actionFunc is given, it will be called as actionFunc(Actor[] actors) once " +
			"production of all actors has been completed.  The actors array is guaranteed to " +
			"only contain alive actors.")]
		public bool Build(string[] actorTypes, LuaFunction actionFunc = null)
		{
			if (triggers.HasAnyCallbacksFor(Trigger.OnProduction))
				return false;

			var queue = queues.Where(q => actorTypes.All(t => GetBuildableInfo(t).Queue.Contains(q.Info.Type)))
				.FirstOrDefault(q => !q.AllQueued().Any());

			if (queue == null)
				return false;

			if (actionFunc != null)
			{
				var player = Self.Owner;
				var squadSize = actorTypes.Length;
				var squad = new List<Actor>();
				var func = actionFunc.CopyReference() as LuaFunction;

				Action<Actor, Actor> productionHandler = (a, b) => { };
				productionHandler = (factory, unit) =>
				{
					if (player != factory.Owner)
					{
						triggers.OnProducedInternal -= productionHandler;
						return;
					}

					squad.Add(unit);
					if (squad.Count >= squadSize)
					{
						using (func)
						using (var luaSquad = squad.Where(u => !u.IsDead).ToArray().ToLuaValue(Context))
							func.Call(luaSquad).Dispose();

						triggers.OnProducedInternal -= productionHandler;
					}
				};

				triggers.OnProducedInternal += productionHandler;
			}

			foreach (var actorType in actorTypes)
				queue.ResolveOrder(Self, Order.StartProduction(Self, actorType, 1));

			return true;
		}

		[Desc("Check whether the factory's production queue that builds this type of actor is currently busy. " +
			"Note: it does not check whether this particular type of actor is being produced.")]
		public bool IsProducing(string actorType)
		{
			if (triggers.HasAnyCallbacksFor(Trigger.OnProduction))
				return true;

			return queues.Where(q => GetBuildableInfo(actorType).Queue.Contains(q.Info.Type))
				.Any(q => q.AllQueued().Any());
		}

		BuildableInfo GetBuildableInfo(string actorType)
		{
			var ri = Self.World.Map.Rules.Actors[actorType];
			var bi = ri.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				throw new LuaException($"Actor of type {actorType} cannot be produced");
			else
				return bi;
		}
	}

	[ScriptPropertyGroup("Production")]
	public class ClassicProductionQueueProperties : ScriptPlayerProperties, Requires<ClassicProductionQueueInfo>, Requires<ScriptTriggersInfo>
	{
		readonly Dictionary<string, Action<Actor, Actor>> productionHandlers;
		readonly Dictionary<string, ClassicProductionQueue> queues;

		public ClassicProductionQueueProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			productionHandlers = new Dictionary<string, Action<Actor, Actor>>();

			queues = new Dictionary<string, ClassicProductionQueue>();
			foreach (var q in player.PlayerActor.TraitsImplementing<ClassicProductionQueue>().Where(q => q.Enabled))
				queues.Add(q.Info.Type, q);

			void GlobalProductionHandler(Actor factory, Actor unit)
			{
				if (factory.Owner != player)
					return;

				var queue = GetBuildableInfo(unit.Info.Name).Queue.First();

				if (productionHandlers.ContainsKey(queue))
					productionHandlers[queue](factory, unit);
			}

			var triggers = TriggerGlobal.GetScriptTriggers(player.PlayerActor);
			triggers.OnOtherProducedInternal += GlobalProductionHandler;
		}

		[Desc("Build the specified set of actors using classic (RA-style) production queues. " +
			"The function will return true if production could be started, false otherwise. " +
			"If an actionFunc is given, it will be called as actionFunc(Actor[] actors) once " +
			"production of all actors has been completed. The actors array is guaranteed to " +
			"only contain alive actors. Note: This function will fail to work when called " +
			"during the first tick.")]
		public bool Build(string[] actorTypes, LuaFunction actionFunc = null)
		{
			var typeToQueueMap = new Dictionary<string, string>();
			foreach (var actorType in actorTypes.Distinct())
				typeToQueueMap.Add(actorType, GetBuildableInfo(actorType).Queue.First());

			var queueTypes = typeToQueueMap.Values.Distinct();

			if (queueTypes.Any(t => !queues.ContainsKey(t) || productionHandlers.ContainsKey(t)))
				return false;

			if (queueTypes.Any(t => queues[t].AllQueued().Any()))
				return false;

			if (actionFunc != null)
			{
				var squadSize = actorTypes.Length;
				var squad = new List<Actor>();
				var func = actionFunc.CopyReference() as LuaFunction;

				void ProductionHandler(Actor factory, Actor unit)
				{
					squad.Add(unit);
					if (squad.Count >= squadSize)
					{
						using (func)
						using (var luaSquad = squad.Where(u => !u.IsDead).ToArray().ToLuaValue(Context))
							func.Call(luaSquad).Dispose();

						foreach (var q in queueTypes)
							productionHandlers.Remove(q);
					}
				}

				foreach (var q in queueTypes)
					productionHandlers.Add(q, ProductionHandler);
			}

			foreach (var actorType in actorTypes)
			{
				var queue = queues[typeToQueueMap[actorType]];
				queue.ResolveOrder(queue.Actor, Order.StartProduction(queue.Actor, actorType, 1));
			}

			return true;
		}

		[Desc("Check whether the production queue that builds this type of actor is currently busy. " +
			"Note: it does not check whether this particular type of actor is being produced.")]
		public bool IsProducing(string actorType)
		{
			var queue = GetBuildableInfo(actorType).Queue.First();

			if (!queues.ContainsKey(queue))
				return true;

			return productionHandlers.ContainsKey(queue) || queues[queue].AllQueued().Any();
		}

		BuildableInfo GetBuildableInfo(string actorType)
		{
			var ri = Player.World.Map.Rules.Actors[actorType];
			var bi = ri.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				throw new LuaException($"Actor of type {actorType} cannot be produced");
			else
				return bi;
		}
	}
}
