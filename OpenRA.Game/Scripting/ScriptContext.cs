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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Eluant;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Scripting
{
	// Tag interfaces specifying the type of bindings to create
	public interface IScriptBindable { }

	// For objects that need the context to create their bindings
	public interface IScriptNotifyBind
	{
		void OnScriptBind(ScriptContext context);
	}

	// For traitinfos that provide actor / player commands
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ScriptPropertyGroupAttribute : Attribute
	{
		public readonly string Category;
		public ScriptPropertyGroupAttribute(string category) { Category = category; }
	}

	// For property groups that are safe to initialize invoke on destroyed actors
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExposedForDestroyedActors : Attribute { }

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class ScriptActorPropertyActivityAttribute : Attribute { }

	public abstract class ScriptActorProperties
	{
		protected readonly Actor Self;
		protected readonly ScriptContext Context;

		protected ScriptActorProperties(ScriptContext context, Actor self)
		{
			Self = self;
			Context = context;
		}
	}

	public abstract class ScriptPlayerProperties
	{
		protected readonly Player Player;
		protected readonly ScriptContext Context;

		protected ScriptPlayerProperties(ScriptContext context, Player player)
		{
			Player = player;
			Context = context;
		}
	}

	/// <summary>
	/// Provides global bindings in Lua code.
	/// </summary>
	/// <remarks>
	/// Instance methods and properties declared in derived classes will be made available in Lua. Use
	/// <see cref="ScriptGlobalAttribute"/> on your derived class to specify the name exposed in Lua. It is recommended
	/// to apply <see cref="DescAttribute"/> against each method or property to provide a description of what it does.
	///
	/// Any parameters to your method that are <see cref="LuaValue"/>s will be disposed automatically when your method
	/// completes. If you need to return any of these values, or need them to live longer than your method, you must
	/// use <see cref="LuaValue.CopyReference"/> to get your own copy of the value. Any copied values you return will
	/// be disposed automatically, but you assume responsibility for disposing any other copies.
	/// </remarks>
	public abstract class ScriptGlobal : ScriptObjectWrapper
	{
		protected override string DuplicateKeyError(string memberName) { return $"Table '{Name}' defines multiple members '{memberName}'"; }
		protected override string MemberNotFoundError(string memberName) { return $"Table '{Name}' does not define a property '{memberName}'"; }

		public readonly string Name;

		protected ScriptGlobal(ScriptContext context)
			: base(context)
		{
			// GetType resolves the actual (subclass) type
			var type = GetType();
			var names = type.GetCustomAttributes<ScriptGlobalAttribute>(true);
			if (names.Length != 1)
				throw new InvalidOperationException($"[ScriptGlobal] attribute not found for global table '{type}'");

			Name = names.First().Name;
			Bind(new[] { this });
		}

		protected IEnumerable<T> FilteredObjects<T>(IEnumerable<T> objects, LuaFunction filter)
		{
			if (filter != null)
			{
				objects = objects.Where(a =>
				{
					using (var luaObject = a.ToLuaValue(Context))
					using (var filterResult = filter.Call(luaObject))
					using (var result = filterResult.First())
						return result.ToBoolean();
				});
			}

			return objects;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ScriptGlobalAttribute : Attribute
	{
		public readonly string Name;
		public ScriptGlobalAttribute(string name) { Name = name; }
	}

	public sealed class ScriptContext : IDisposable
	{
		// Restrict user scripts (excluding system libraries) to 50 MB of memory use
		const int MaxUserScriptMemory = 50 * 1024 * 1024;

		// Restrict the number of instructions that will be run per map function call
		const int MaxUserScriptInstructions = 1000000;

		public World World { get; }
		public WorldRenderer WorldRenderer { get; }

		readonly MemoryConstrainedLuaRuntime runtime;
		readonly LuaFunction tick;

		readonly Type[] knownActorCommands;
		public readonly Cache<ActorInfo, Type[]> ActorCommands;
		public readonly Type[] PlayerCommands;

		public string ErrorMessage;

		bool disposed;

		public ScriptContext(World world, WorldRenderer worldRenderer,
			IEnumerable<string> scripts)
		{
			runtime = new MemoryConstrainedLuaRuntime();

			Log.AddChannel("lua", "lua.log");

			World = world;
			WorldRenderer = worldRenderer;
			knownActorCommands = Game.ModData.ObjectCreator
				.GetTypesImplementing<ScriptActorProperties>()
				.ToArray();

			ActorCommands = new Cache<ActorInfo, Type[]>(FilterActorCommands);

			var knownPlayerCommands = Game.ModData.ObjectCreator
				.GetTypesImplementing<ScriptPlayerProperties>()
				.ToArray();
			PlayerCommands = FilterCommands(world.Map.Rules.Actors[SystemActors.Player], knownPlayerCommands);

			// Safe functions for http://lua-users.org/wiki/SandBoxes
			// assert, error have been removed as well as albeit safe
			var allowedGlobals = new string[]
			{
				"ipairs", "next", "pairs",
				"pcall", "select", "tonumber", "tostring", "type", "unpack", "xpcall",
				"math", "string", "table"
			};

			foreach (var fieldName in runtime.Globals.Keys)
				if (!allowedGlobals.Contains(fieldName.ToString()))
					runtime.Globals[fieldName] = null;

			var forbiddenMath = new string[]
			{
				"random", // not desync safe, unsuitable
				"randomseed" // maybe unsafe as it affects the host RNG
			};

			var mathGlobal = (LuaTable)runtime.Globals["math"];
			foreach (var mathFunction in mathGlobal.Keys)
				if (forbiddenMath.Contains(mathFunction.ToString()))
					mathGlobal[mathFunction] = null;

			// Register globals
			runtime.Globals["EngineDir"] = Platform.EngineDir;

			using (var fn = runtime.CreateFunctionFromDelegate((Action<string>)FatalError))
				runtime.Globals["FatalError"] = fn;

			runtime.Globals["MaxUserScriptInstructions"] = MaxUserScriptInstructions;

			using (var fn = runtime.CreateFunctionFromDelegate((Action<string>)LogDebugMessage))
				runtime.Globals["print"] = fn;

			// Register global tables
			var bindings = Game.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>();
			foreach (var b in bindings)
			{
				var ctor = b.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(c =>
				{
					var p = c.GetParameters();
					return p.Length == 1 && p.First().ParameterType == typeof(ScriptContext);
				});

				if (ctor == null)
					throw new InvalidOperationException($"{b.Name} must define a constructor that takes a {nameof(ScriptContext)} context parameter");

				var binding = (ScriptGlobal)ctor.Invoke(new[] { this });
				using (var obj = binding.ToLuaValue(this))
					runtime.Globals.Add(binding.Name, obj);
			}

			// System functions do not count towards the memory limit
			runtime.MaxMemoryUse = runtime.MemoryUse + MaxUserScriptMemory;

			try
			{
				foreach (var script in scripts)
					runtime.DoBuffer(world.Map.Open(script).ReadAllText(), script).Dispose();
			}
			catch (Exception e)
			{
				FatalError(e);
				return;
			}

			tick = runtime.Globals["Tick"] as LuaFunction;
		}

		void LogDebugMessage(string message)
		{
			Console.WriteLine($"Lua debug: {message}");
			Log.Write("lua", message);
		}

		public bool FatalErrorOccurred { get; private set; }
		public void FatalError(Exception e)
		{
			ErrorMessage = e.Message;

			Console.WriteLine($"Fatal Lua Error: {e.Message}");
			Console.WriteLine(e.StackTrace);

			Log.Write("lua", $"Fatal Lua Error: {e.Message}");
			Log.Write("lua", e.StackTrace);

			FatalErrorOccurred = true;

			World.AddFrameEndTask(w => World.EndGame());
		}

		void FatalError(string message)
		{
			var stacktrace = new StackTrace().ToString();

			Console.WriteLine($"Fatal Lua Error: {message}");
			Console.WriteLine(stacktrace);

			Log.Write("lua", message);
			Log.Write("lua", stacktrace);

			FatalErrorOccurred = true;

			World.AddFrameEndTask(w => World.EndGame());
		}

		public void RegisterMapActor(string name, Actor a)
		{
			if (runtime.Globals.ContainsKey(name))
				throw new LuaException($"The global name '{name}' is reserved, and may not be used by a map actor");

			using (var obj = a.ToLuaValue(this))
				runtime.Globals.Add(name, obj);
		}

		public void WorldLoaded()
		{
			if (FatalErrorOccurred || runtime.Globals["WorldLoaded"] is not LuaFunction worldLoaded)
				return;

			try
			{
				worldLoaded.Call().Dispose();
			}
			catch (LuaException e)
			{
				FatalError(e);
			}
			finally
			{
				worldLoaded?.Dispose();
			}
		}

		public void Tick()
		{
			if (FatalErrorOccurred || disposed || tick == null)
				return;

			try
			{
				using (new PerfSample("tick_lua"))
					tick.Call().Dispose();
			}
			catch (LuaException e)
			{
				FatalError(e);
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			runtime?.Dispose();
		}

		static IEnumerable<Type> ExtractRequiredTypes(Type t)
		{
			// Returns the inner types of all the Requires<T> interfaces on this type
			var outer = t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>));

			return outer.SelectMany(i => i.GetGenericArguments());
		}

		static readonly object[] NoArguments = Array.Empty<object>();
		Type[] FilterActorCommands(ActorInfo ai)
		{
			return FilterCommands(ai, knownActorCommands);
		}

		Type[] FilterCommands(ActorInfo ai, Type[] knownCommands)
		{
			var method = typeof(ActorInfo).GetMethod(nameof(ActorInfo.HasTraitInfo));
			return knownCommands.Where(c => ExtractRequiredTypes(c)
				.All(t => (bool)method.MakeGenericMethod(t).Invoke(ai, NoArguments)))
				.ToArray();
		}

		public LuaTable CreateTable() { return runtime.CreateTable(); }
	}
}
