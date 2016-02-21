#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Production")]
	public class ProductionProperties : ScriptActorProperties, Requires<ProductionInfo>
	{
		readonly Production p;

		public ProductionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			p = self.Trait<Production>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Build a unit, ignoring the production queue. The activity will wait if the exit is blocked.")]
		public void Produce(string actorType, string factionVariant = null)
		{
			ActorInfo actorInfo;
			if (!Self.World.Map.Rules.Actors.TryGetValue(actorType, out actorInfo))
				throw new LuaException("Unknown actor type '{0}'".F(actorType));

			Self.QueueActivity(new WaitFor(() => p.Produce(Self, actorInfo, factionVariant)));
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
			get { return rp.Location; }
			set { rp.Location = value; }
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
			get { return pb.IsPrimary; }
			set { pb.SetPrimaryProducer(Self, value); }
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
				.FirstOrDefault(q => q.CurrentItem() == null);

			if (queue == null)
				return false;

			if (actionFunc != null)
			{
				var player = Self.Owner;
				var squadSize = actorTypes.Length;
				var squad = new List<Actor>();
				var func = actionFunc.CopyReference() as LuaFunction;

				Action<Actor, Actor> productionHandler = (_, __) => { };
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
				.Any(q => q.CurrentItem() != null);
		}

		BuildableInfo GetBuildableInfo(string actorType)
		{
			var ri = Self.World.Map.Rules.Actors[actorType];
			var bi = ri.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				throw new LuaException("Actor of type {0} cannot be produced".F(actorType));
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

			Action<Actor, Actor> globalProductionHandler = (factory, unit) =>
			{
				if (factory.Owner != player)
					return;

				var queue = GetBuildableInfo(unit.Info.Name).Queue.First();

				if (productionHandlers.ContainsKey(queue))
					productionHandlers[queue](factory, unit);
			};

			var triggers = TriggerGlobal.GetScriptTriggers(player.PlayerActor);
			triggers.OnOtherProducedInternal += globalProductionHandler;
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

			if (queueTypes.Any(t => queues[t].CurrentItem() != null))
				return false;

			if (actionFunc != null)
			{
				var squadSize = actorTypes.Length;
				var squad = new List<Actor>();
				var func = actionFunc.CopyReference() as LuaFunction;

				Action<Actor, Actor> productionHandler = (factory, unit) =>
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
				};

				foreach (var q in queueTypes)
					productionHandlers.Add(q, productionHandler);
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

			return productionHandlers.ContainsKey(queue) || queues[queue].CurrentItem() != null;
		}

		BuildableInfo GetBuildableInfo(string actorType)
		{
			var ri = Player.World.Map.Rules.Actors[actorType];
			var bi = ri.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				throw new LuaException("Actor of type {0} cannot be produced".F(actorType));
			else
				return bi;
		}
	}
}