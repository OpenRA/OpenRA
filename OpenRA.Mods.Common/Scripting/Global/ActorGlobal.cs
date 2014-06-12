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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Buildings;
using OpenRA.Mods.Common.Air;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Scripting
{
	[ScriptGlobal("Actor")]
	public class ActorGlobal : ScriptGlobal
	{
		public ActorGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new actor. initTable specifies a list of key-value pairs that definite initial parameters for the actor's traits.")]
		public Actor Create(string type, bool addToWorld, LuaTable initTable)
		{
			var initDict = new TypeDictionary();

			// Convert table entries into ActorInits
			foreach (var kv in initTable)
			{
				// Find the requested type
				var typeName = kv.Key.ToString();
				var initType = Game.modData.ObjectCreator.FindType(typeName + "Init");
				if (initType == null)
					throw new LuaException("Unknown initializer type '{0}'".F(typeName));

				// Cast it up to an IActorInit<T>
				var genericType = initType.GetInterfaces()
					.First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IActorInit<>));
				var innerType = genericType.GetGenericArguments().First();

				// Try and coerce the table value to the required type
				object value;
				if (!kv.Value.TryGetClrValue(innerType, out value))
					throw new LuaException("Invalid data type for '{0}' (expected '{1}')".F(typeName, innerType.Name));

				// Construct the ActorInit. Phew!
				var test = initType.GetConstructor(new[] { innerType }).Invoke(new[] { value });
				initDict.Add(test);
			}

			// The actor must be added to the world at the end of the tick
			var a = context.World.CreateActor(false, type, initDict);
			if (addToWorld)
				context.World.AddFrameEndTask(w => w.Add(a));

			return a;
		}

		[Desc("Returns the build time (in ticks) of the requested unit type")]
		public int BuildTime(string type)
		{
			ActorInfo ai;
			if (!context.World.Map.Rules.Actors.TryGetValue(type, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			return ai.GetBuildTime();
		}

		[Desc("Returns the cruise altitude of the requested unit type (zero if it ground-based).")]
		public int CruiseAltitude(string type)
		{
			ActorInfo ai;
			if (!context.World.Map.Rules.Actors.TryGetValue(type, out ai))
				throw new LuaException("Unknown actor type '{0}'".F(type));

			var pi = ai.Traits.GetOrDefault<PlaneInfo>();
			return pi != null ? pi.CruiseAltitude.Range : 0;
		}
	}
}
