#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;

namespace OpenRA
{
	public class ObjectCreator
	{
		readonly Cache<string, Type> typeCache;
		readonly Cache<Type, ConstructorInfo> ctorCache;
		readonly Pair<Assembly, string>[] assemblies;

		public ObjectCreator(Manifest manifest)
		{
			typeCache = new Cache<string, Type>(FindType);
			ctorCache = new Cache<Type, ConstructorInfo>(GetCtor);

			// All the core namespaces
			var asms = typeof(Game).Assembly.GetNamespaces() // Game
				.Select(c => Pair.New(typeof(Game).Assembly, c))
				.ToList();

			// Namespaces from each mod assembly
			foreach (var a in manifest.Assemblies)
			{
				var asm = Assembly.LoadFile(Path.GetFullPath(a));
				asms.AddRange(asm.GetNamespaces().Select(ns => Pair.New(asm, ns)));
			}

			assemblies = asms.ToArray();
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
			using (var e = ctors.GetEnumerator())
			{
				if (!e.MoveNext())
					return null;
				var ctor = e.Current;
				if (!e.MoveNext())
					return ctor;
			}
			throw new InvalidOperationException("ObjectCreator: UseCtor on multiple constructors; invalid.");
		}

		public object CreateBasic(Type type)
		{
			return type.GetConstructor(new Type[0]).Invoke(new object[0]);
		}

		public object CreateUsingArgs(ConstructorInfo ctor, Dictionary<string, object> args)
		{
			var p = ctor.GetParameters();
			var a = new object[p.Length];
			for (int i = 0; i < p.Length; i++)
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
			return assemblies.Select(ma => ma.First).Distinct()
				.SelectMany(ma => ma.GetTypes()
				.Where(t => t != it && it.IsAssignableFrom(t)));
		}

		[AttributeUsage(AttributeTargets.Constructor)]
		public sealed class UseCtorAttribute : Attribute { }
	}
}
