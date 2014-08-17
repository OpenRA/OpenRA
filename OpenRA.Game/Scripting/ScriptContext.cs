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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Eluant;
using OpenRA.FileSystem;
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
	public sealed class ScriptPropertyGroupAttribute : Attribute
	{
		public readonly string Category;
		public ScriptPropertyGroupAttribute(string category) { Category = category; }
	}

	public sealed class ScriptActorPropertyActivityAttribute : Attribute { }

	public abstract class ScriptActorProperties
	{
		protected readonly Actor self;
		protected readonly ScriptContext context;
		public ScriptActorProperties(ScriptContext context, Actor self)
		{
			this.self = self;
			this.context = context;
		}
	}

	public abstract class ScriptPlayerProperties
	{
		protected readonly Player player;
		protected readonly ScriptContext context;
		public ScriptPlayerProperties(ScriptContext context, Player player)
		{
			this.player = player;
			this.context = context;
		}
	}

	// For global-level bindings
	public abstract class ScriptGlobal : ScriptObjectWrapper
	{
		protected override string DuplicateKeyError(string memberName) { return "Table '{0}' defines multiple members '{1}'".F(Name, memberName); }
		protected override string MemberNotFoundError(string memberName) { return "Table '{0}' does not define a property '{1}'".F(Name, memberName); }

		public readonly string Name;
		public ScriptGlobal(ScriptContext context)
			: base(context)
		{
			// The 'this.' resolves the actual (subclass) type
			var type = this.GetType();
			var names = type.GetCustomAttributes<ScriptGlobalAttribute>(true);
			if (names.Count() != 1)
				throw new InvalidOperationException("[LuaGlobal] attribute not found for global table '{0}'".F(type));

			Name = names.First().Name;
			Bind(new[] { this });
		}
	}

	public sealed class ScriptGlobalAttribute : Attribute
	{
		public readonly string Name;
		public ScriptGlobalAttribute(string name) { Name = name; }
	}

	public sealed class ScriptContext : IDisposable
	{
		public World World { get; private set; }
		public WorldRenderer WorldRenderer { get; private set; }

		readonly MemoryConstrainedLuaRuntime runtime;
		readonly LuaFunction tick;

		// Restrict user scripts (excluding system libraries) to 50 MB of memory use
		const int MaxUserScriptMemory = 50 * 1024 * 1024;

		// Restrict the number of instructions that will be run per map function call
		const int MaxUserScriptInstructions = 1000000;

		readonly Type[] knownActorCommands;
		public readonly Cache<ActorInfo, Type[]> ActorCommands;
		public readonly Type[] PlayerCommands;

		bool disposed;

		public ScriptContext(World world, WorldRenderer worldRenderer,
			IEnumerable<string> scripts)
		{
			runtime = new MemoryConstrainedLuaRuntime();

			World = world;
			WorldRenderer = worldRenderer;
			knownActorCommands = Game.modData.ObjectCreator
				.GetTypesImplementing<ScriptActorProperties>()
				.ToArray();

			ActorCommands = new Cache<ActorInfo, Type[]>(FilterActorCommands);
			PlayerCommands = Game.modData.ObjectCreator
				.GetTypesImplementing<ScriptPlayerProperties>()
				.ToArray();

			runtime.DoBuffer(GlobalFileSystem.Open(Path.Combine("lua", "scriptwrapper.lua")).ReadAllText(), "scriptwrapper.lua").Dispose();
			tick = (LuaFunction)runtime.Globals["Tick"];

			// Register globals
			using (var fn = runtime.CreateFunctionFromDelegate((Action<string>)FatalError))
				runtime.Globals["FatalError"] = fn;

			runtime.Globals["MaxUserScriptInstructions"] = MaxUserScriptInstructions;

			using (var registerGlobal = (LuaFunction)runtime.Globals["RegisterSandboxedGlobal"])
			{
				using (var fn = runtime.CreateFunctionFromDelegate((Action<string>)Console.WriteLine))
					registerGlobal.Call("print", fn).Dispose();

				// Register global tables
				var bindings = Game.modData.ObjectCreator.GetTypesImplementing<ScriptGlobal>();
				foreach (var b in bindings)
				{
					var ctor = b.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(c =>
					{
						var p = c.GetParameters();
						return p.Length == 1 && p.First().ParameterType == typeof(ScriptContext);
					});

					if (ctor == null)
						throw new InvalidOperationException("{0} must define a constructor that takes a ScriptContext context parameter".F(b.Name));

					var binding = (ScriptGlobal)ctor.Invoke(new[] { this });
					using (var obj = binding.ToLuaValue(this))
						registerGlobal.Call(binding.Name, obj).Dispose();
				}
			}

			// System functions do not count towards the memory limit
			runtime.MaxMemoryUse = runtime.MemoryUse + MaxUserScriptMemory;

			using (var loadScript = (LuaFunction)runtime.Globals["ExecuteSandboxedScript"])
			{
				foreach (var s in scripts)
					loadScript.Call(s, GlobalFileSystem.Open(s).ReadAllText()).Dispose();
			}
		}

		bool error;
		public void FatalError(string message)
		{
			Console.WriteLine("Fatal Lua Error: {0}", message);
			Game.AddChatLine(Color.White, "Fatal Lua Error", message);
			error = true;
		}

		public void RegisterMapActor(string name, Actor a)
		{
			using (var registerGlobal = (LuaFunction)runtime.Globals["RegisterSandboxedGlobal"])
			{
				if (runtime.Globals.ContainsKey(name))
					throw new LuaException("The global name '{0}' is reserved, and may not be used by a map actor".F(name));

				using (var obj = a.ToLuaValue(this))
					registerGlobal.Call(name, obj).Dispose();
			}
		}

		public void WorldLoaded()
		{
			if (error)
				return;

			using (var worldLoaded = (LuaFunction)runtime.Globals["WorldLoaded"])
				worldLoaded.Call().Dispose();
		}

		public void Tick(Actor self)
		{
			if (error || disposed)
				return;

			using (new PerfSample("tick_lua"))
				tick.Call().Dispose();
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			if (runtime != null)
				runtime.Dispose();
		}

		static Type[] ExtractRequiredTypes(Type t)
		{
			// Returns the inner types of all the Requires<T> interfaces on this type
			var outer = t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>));

			return outer.SelectMany(i => i.GetGenericArguments()).ToArray();
		}

		static readonly object[] NoArguments = new object[0];
		Type[] FilterActorCommands(ActorInfo ai)
		{
			var method = typeof(TypeDictionary).GetMethod("Contains");
			return knownActorCommands.Where(c => ExtractRequiredTypes(c)
				.All(t => (bool)method.MakeGenericMethod(t).Invoke(ai.Traits, NoArguments)))
				.ToArray();
		}

		public LuaTable CreateTable() { return runtime.CreateTable(); }
	}
}
