#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using System.Reflection;
using System.Runtime.Serialization;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Actor")]
	public class ActorGlobal : ScriptGlobal
	{
		public ActorGlobal(ScriptContext context)
			: base(context) { }

		ActorInit CreateInit(string initName, LuaValue value)
		{
			// Find the requested type
			var initInstance = initName.Split(ActorInfo.TraitInstanceSeparator);
			var initType = Game.ModData.ObjectCreator.FindType(initInstance[0] + "Init");
			if (initType == null)
				throw new LuaException("Unknown initializer type '{0}'".F(initInstance[0]));

			// Construct the ActorInit.
			var init = (ActorInit)FormatterServices.GetUninitializedObject(initType);
			if (initInstance.Length > 1)
				initType.GetField("InstanceName").SetValue(init, initInstance[1]);

			var compositeInit = init as CompositeActorInit;
			var tableValue = value as LuaTable;
			if (tableValue != null && compositeInit != null)
			{
				var args = compositeInit.InitializeArgs();
				var initValues = new Dictionary<string, object>();
				foreach (var kv in tableValue)
				{
					using (kv.Key)
					using (kv.Value)
					{
						var key = kv.Key.ToString();
						if (!args.TryGetValue(key, out var type))
							throw new LuaException("Unknown initializer type '{0}.{1}'".F(initInstance[0], key));

						var isActorReference = type == typeof(ActorInitActorReference);
						if (isActorReference)
							type = kv.Value is LuaString ? typeof(string) : typeof(Actor);

						if (!kv.Value.TryGetClrValue(type, out var clrValue))
							throw new LuaException("Invalid data type for '{0}.{1}' (expected {2}, got {3})".F(initInstance[0], key, type.Name, kv.Value.WrappedClrType()));

						if (isActorReference)
							clrValue = type == typeof(string) ? new ActorInitActorReference((string)clrValue) : new ActorInitActorReference((Actor)clrValue);

						initValues[key] = clrValue;
					}
				}

				compositeInit.Initialize(initValues);
				return init;
			}

			// HACK: Backward compatibility for legacy int facings
			var facingInit = init as FacingInit;
			if (facingInit != null)
			{
				if (value.TryGetClrValue(out int facing))
				{
					facingInit.Initialize(WAngle.FromFacing(facing));
					Game.Debug("Initializing Facing with integers is deprecated. Use Angle instead.");
					return facingInit;
				}
			}

			var initializers = initType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(m => m.Name == "Initialize" && m.GetParameters().Length == 1);

			foreach (var initializer in initializers)
			{
				var parameterType = initializer.GetParameters().First().ParameterType;
				var valueType = parameterType.IsEnum ? Enum.GetUnderlyingType(parameterType) : parameterType;

				// Try and coerce the table value to the required type
				if (!value.TryGetClrValue(valueType, out var clrValue))
					continue;

				initializer.Invoke(init, new[] { clrValue });

				return init;
			}

			var types = initializers.Select(y => y.GetParameters()[0].ParameterType.Name).JoinWith(", ");
			throw new LuaException("Invalid data type for '{0}' (expected one of {1})".F(initInstance[0], types));
		}

		[Desc("Create a new actor. initTable specifies a list of key-value pairs that defines the initial parameters for the actor's traits.")]
		public Actor Create(string type, bool addToWorld, LuaTable initTable)
		{
			var initDict = new TypeDictionary();

			// Convert table entries into ActorInits
			foreach (var kv in initTable)
			{
				using (kv.Key)
				using (kv.Value)
					initDict.Add(CreateInit(kv.Key.ToString(), kv.Value));
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
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out var ai))
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
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out var ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var pi = ai.TraitInfoOrDefault<ICruiseAltitudeInfo>();
			return pi != null ? pi.GetCruiseAltitude().Length : 0;
		}

		public int Cost(string type)
		{
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out var ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var vi = ai.TraitInfoOrDefault<ValuedInfo>();
			if (vi == null)
				throw new LuaException("Actor type '{0}' does not have the Valued trait required to get the Cost.".F(type));

			return vi.Cost;
		}
	}
}
