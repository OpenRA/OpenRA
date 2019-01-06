#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Actor")]
	public class ActorGlobal : ScriptGlobal
	{
		public ActorGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new actor. initTable specifies a list of key-value pairs that defines the initial parameters for the actor's traits.")]
		public Actor Create(string type, bool addToWorld, LuaTable initTable)
		{
			var initDict = new TypeDictionary();

			// Convert table entries into ActorInits
			foreach (var kv in initTable)
			{
				using (kv.Key)
				using (kv.Value)
				{
					// Find the requested type
					var typeName = kv.Key.ToString();
					var initType = Game.ModData.ObjectCreator.FindType(typeName + "Init");
					if (initType == null)
						throw new LuaException("Unknown initializer type '{0}'".F(typeName));

					// Cast it up to an IActorInit<T>
					var genericType = initType.GetInterfaces()
						.First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IActorInit<>));
					var innerType = genericType.GetGenericArguments().First();
					var valueType = innerType.IsEnum ? Enum.GetUnderlyingType(innerType) : innerType;

					// Try and coerce the table value to the required type
					object value;
					if (!kv.Value.TryGetClrValue(valueType, out value))
						throw new LuaException("Invalid data type for '{0}' (expected '{1}')".F(typeName, valueType.Name));

					// Construct the ActorInit. Phew!
					var test = initType.GetConstructor(new[] { innerType }).Invoke(new[] { value });
					initDict.Add(test);
				}
			}

			var owner = initDict.GetOrDefault<OwnerInit>();
			if (owner == null)
				throw new LuaException("Tried to create actor '{0}' with an invalid or no owner init!".F(type));

			// The actor must be added to the world at the end of the tick
			var a = Context.World.CreateActor(false, type, initDict);
			if (addToWorld)
				Context.World.AddFrameEndTask(w => w.Add(a));

			return a;
		}

		[Desc("Returns the build time (in ticks) of the requested unit type.",
			"An optional second value can be used to exactly specify the producing queue type.")]
		public int BuildTime(string type, string queue = null)
		{
			ActorInfo ai;
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var bi = ai.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
			{
				var valued = ai.TraitInfoOrDefault<ValuedInfo>();
				if (valued == null)
					return 0;
				else
					time = valued.Cost;
			}

			int pbi;
			if (queue != null)
			{
				var pqueue = Context.World.Map.Rules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => x.Type == queue)).FirstOrDefault();

				if (pqueue == null)
					throw new LuaException("The specified queue '{0}' does not exist!".F(queue));

				pbi = pqueue.BuildDurationModifier;
			}
			else
			{
				var pqueue = Context.World.Map.Rules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => bi.Queue.Contains(x.Type))).FirstOrDefault();

				if (pqueue == null)
					throw new LuaException("No actors can produce actor '{0}'!".F(type));

				pbi = pqueue.BuildDurationModifier;
			}

			time = time * bi.BuildDurationModifier * pbi / 10000;
			return time;
		}

		[Desc("Returns the cruise altitude of the requested unit type (zero if it is ground-based).")]
		public int CruiseAltitude(string type)
		{
			ActorInfo ai;
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var pi = ai.TraitInfoOrDefault<ICruiseAltitudeInfo>();
			return pi != null ? pi.GetCruiseAltitude().Length : 0;
		}

		public int Cost(string type)
		{
			ActorInfo ai;
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var vi = ai.TraitInfoOrDefault<ValuedInfo>();
			if (vi == null)
				throw new LuaException("Actor type '{0}' does not have the Valued trait required to get the Cost.".F(type));

			return vi.Cost;
		}
	}
}
