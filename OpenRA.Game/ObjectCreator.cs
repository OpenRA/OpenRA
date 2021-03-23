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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class ObjectCreator : IDisposable
	{
		// .NET does not support unloading assemblies, so mod libraries will leak across mod changes.
		// This tracks the assemblies that have been loaded since game start so that we don't load multiple copies
		static readonly Dictionary<string, Assembly> ResolvedAssemblies = new Dictionary<string, Assembly>();

		readonly Cache<string, Type> typeCache;
		readonly Cache<Type, ConstructorInfo> ctorCache;
		readonly (Assembly Assembly, string Namespace)[] assemblies;

		public ObjectCreator(Manifest manifest, InstalledMods mods)
		{
			typeCache = new Cache<string, Type>(FindType);
			ctorCache = new Cache<Type, ConstructorInfo>(GetCtor);

			// Allow mods to load types from the core Game assembly, and any additional assemblies they specify.
			// Assemblies can only be loaded from directories to avoid circular dependencies on package loaders.
			var assemblyList = new List<Assembly>() { typeof(Game).Assembly };
			foreach (var path in manifest.Assemblies)
			{
				var resolvedPath = FileSystem.FileSystem.ResolveAssemblyPath(path, manifest, mods);
				if (resolvedPath == null)
					throw new FileNotFoundException("Assembly `{0}` not found.".F(path));

				LoadAssembly(assemblyList, resolvedPath);
			}

			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
			assemblies = assemblyList.SelectMany(asm => asm.GetNamespaces().Select(ns => (asm, ns))).ToArray();
		}

		void LoadAssembly(List<Assembly> assemblyList, string resolvedPath)
		{
			// .NET doesn't provide any way of querying the metadata of an assembly without either:
			//   (a) loading duplicate data into the application domain, breaking the world.
			//   (b) crashing if the assembly has already been loaded.
			// We can't check the internal name of the assembly, so we'll work off the data instead
			var hash = CryptoUtil.SHA1Hash(File.ReadAllBytes(resolvedPath));

			if (!ResolvedAssemblies.TryGetValue(hash, out var assembly))
			{
#if MONO
				assembly = Assembly.LoadFile(resolvedPath);
				ResolvedAssemblies.Add(hash, assembly);

				// Allow mods to use libraries.
				var assemblyPath = Path.GetDirectoryName(resolvedPath);
				if (assemblyPath != null)
				{
					foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
					{
						var depedencyPath = Path.Combine(assemblyPath, referencedAssembly.Name + ".dll");
						if (File.Exists(depedencyPath))
							LoadAssembly(assemblyList, depedencyPath);
					}
				}
#else
				var loader = new AssemblyLoader(resolvedPath);
				assembly = loader.LoadDefaultAssembly();
				ResolvedAssemblies.Add(hash, assembly);
#endif
			}

			assemblyList.Add(assembly);
		}

		Assembly ResolveAssembly(object sender, ResolveEventArgs e)
		{
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
				if (a.FullName == e.Name)
					return a;

			return assemblies?.Select(a => a.Assembly).FirstOrDefault(a => a.FullName == e.Name);
		}

		// Only used by the linter to prevent exceptions from being thrown during a lint run
		public static Action<string> MissingTypeAction = null;

		public T CreateObject<T>(string className)
		{
			return CreateObject<T>(className, new Dictionary<string, object>());
		}

		public T CreateObject<T>(string className, Dictionary<string, object> args)
		{
			var type = typeCache[className];
			if (type == null)
			{
				// HACK: The linter does not want to crash but only print an error instead
				if (MissingTypeAction != null)
					MissingTypeAction(className);
				else
					throw new InvalidOperationException("Cannot locate type: {0}".F(className));

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
				.Select(pair => pair.Assembly.GetType(pair.Namespace + "." + className, false))
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
			return assemblies.Select(ma => ma.Assembly).Distinct()
				.SelectMany(ma => ma.GetTypes());
		}

		public TLoader[] GetLoaders<TLoader>(IEnumerable<string> formats, string name)
		{
			var loaders = new List<TLoader>();
			foreach (var format in formats)
			{
				var loader = FindType(format + "Loader");
				if (loader == null || !loader.GetInterfaces().Contains(typeof(TLoader)))
					throw new InvalidOperationException("Unable to find a {0} loader for type '{1}'.".F(name, format));

				loaders.Add((TLoader)CreateBasic(loader));
			}

			return loaders.ToArray();
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
