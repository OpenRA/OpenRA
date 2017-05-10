#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;

namespace OpenRA
{
	public sealed class ObjectCreator : IDisposable
	{
		// .NET does not support unloading assemblies, so mod libraries will leak across mod changes.
		// This tracks the assemblies that have been loaded since game start so that we don't load multiple copies
		static readonly Dictionary<string, Assembly> ResolvedAssemblies = new Dictionary<string, Assembly>();

		readonly Cache<string, Type> typeCache;
		readonly Cache<Type, ConstructorInfo> ctorCache;
		readonly Pair<Assembly, string>[] assemblies;
		readonly bool isMonoRuntime = Type.GetType("Mono.Runtime") != null;

		public ObjectCreator(Manifest manifest, FileSystem.FileSystem modFiles)
		{
			typeCache = new Cache<string, Type>(FindType);
			ctorCache = new Cache<Type, ConstructorInfo>(GetCtor);

			// Allow mods to load types from the core Game assembly, and any additional assemblies they specify.
			var assemblyList = new List<Assembly>() { typeof(Game).Assembly };
			foreach (var path in manifest.Assemblies)
			{
				var data = modFiles.Open(path).ReadAllBytes();

				// .NET doesn't provide any way of querying the metadata of an assembly without either:
				//   (a) loading duplicate data into the application domain, breaking the world.
				//   (b) crashing if the assembly has already been loaded.
				// We can't check the internal name of the assembly, so we'll work off the data instead
				var hash = CryptoUtil.SHA1Hash(data);

				Assembly assembly;
				if (!ResolvedAssemblies.TryGetValue(hash, out assembly))
				{
					// HACK: SharpDevelop crashes when trying to load the .pdb using Assembly.Load
					if (Process.GetProcessesByName("SharpDevelop").Any())
					{
						using (Stream stream = modFiles.Open(path))
						{
							var fs = stream as FileStream;
							assembly = fs != null ? Assembly.LoadFile(fs.Name) : Assembly.Load(data);
						}
					}
					else
					{
						Stream symbolStream = null;
						var hasSymbols = false;

						// Mono has its own symbol format.
						if (isMonoRuntime)
							hasSymbols = modFiles.TryOpen(path + ".mdb", out symbolStream);

						// .NET uses .pdb files.
						else
							hasSymbols = modFiles.TryOpen(path.Substring(0, path.Length - 4) + ".pdb", out symbolStream);

						assembly = hasSymbols ? Assembly.Load(data, symbolStream.ReadAllBytes()) : Assembly.Load(data);
					}

					ResolvedAssemblies.Add(hash, assembly);
				}

				assemblyList.Add(assembly);
			}

			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
			assemblies = assemblyList.SelectMany(asm => asm.GetNamespaces().Select(ns => Pair.New(asm, ns))).ToArray();
		}

		Assembly ResolveAssembly(object sender, ResolveEventArgs e)
		{
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
				if (a.FullName == e.Name)
					return a;

			if (assemblies == null)
				return null;

			return assemblies.Select(a => a.First).FirstOrDefault(a => a.FullName == e.Name);
		}

		public static Action<string> MissingTypeAction =
			s => { throw new InvalidOperationException("Cannot locate type: {0}".F(s)); };

		public T CreateObject<T>(string className)
		{
			return CreateObject<T>(className, new Dictionary<string, object>());
		}

		public T CreateObject<T>(string className, Dictionary<string, object> args)
		{
			var type = typeCache[className];
			if (type == null)
			{
				MissingTypeAction(className);
				return default(T);
			}

			var ctor = ctorCache[type];
			if (ctor == null)
				return (T)CreateBasic(type);
			else
				return (T)CreateUsingArgs(ctor, args);
		}

		public Type FindType(string className)
		{
			return assemblies
				.Select(pair => pair.First.GetType(pair.Second + "." + className, false))
				.FirstOrDefault(t => t != null);
		}

		public ConstructorInfo GetCtor(Type type)
		{
			var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
			var ctors = type.GetConstructors(flags).Where(x => x.HasAttribute<UseCtorAttribute>());
			if (ctors.Count() > 1)
				throw new InvalidOperationException("ObjectCreator: UseCtor on multiple constructors; invalid.");
			return ctors.FirstOrDefault();
		}

		public object CreateBasic(Type type)
		{
			return type.GetConstructor(new Type[0]).Invoke(new object[0]);
		}

		public object CreateUsingArgs(ConstructorInfo ctor, Dictionary<string, object> args)
		{
			var p = ctor.GetParameters();
			var a = new object[p.Length];
			for (var i = 0; i < p.Length; i++)
			{
				var key = p[i].Name;
				if (!args.ContainsKey(key)) throw new InvalidOperationException("ObjectCreator: key `{0}' not found".F(key));
				a[i] = args[key];
			}

			return ctor.Invoke(a);
		}

		public IEnumerable<Type> GetTypesImplementing<T>()
		{
			var it = typeof(T);
			return GetTypes().Where(t => t != it && it.IsAssignableFrom(t));
		}

		public IEnumerable<Type> GetTypes()
		{
			return assemblies.Select(ma => ma.First).Distinct()
				.SelectMany(ma => ma.GetTypes());
		}

		~ObjectCreator()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposing)
				AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
		}

		[AttributeUsage(AttributeTargets.Constructor)]
		public sealed class UseCtorAttribute : Attribute { }
	}
}
