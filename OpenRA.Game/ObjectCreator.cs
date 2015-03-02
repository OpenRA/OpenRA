#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using ProtoBuf;

namespace OpenRA
{
	public class ObjectCreator
	{
		readonly Cache<string, Type> typeCache;
		readonly Cache<Type, ConstructorInfo> ctorCache;
		readonly Pair<Assembly, string>[] assemblies;
		readonly Type[] protoTypes;

		public ObjectCreator(IEnumerable<Assembly> asms)
		{
			typeCache = new Cache<string, Type>(FindType);
			ctorCache = new Cache<Type, ConstructorInfo>(GetCtor);

			assemblies = asms.SelectMany(a => a.GetNamespaces()
				.Select(ns => Pair.New(a, ns))).ToArray();

			var ti = GetTypes()
				.Where(i => Attribute.IsDefined(i, typeof(ProtoContractAttribute)))
				.OrderBy(o => o.FullName);

			protoTypes = new Type[] { null }.Concat(ti).ToArray();
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

		Type ProtoContractFromID(int i)
		{
			return (i > 0 && i < protoTypes.Length) ? protoTypes[i] : null;
		}

		int IDFromProtoContract(Type t)
		{
			return protoTypes.Contains(t) ? protoTypes.IndexOf(t) : 0;
		}

		public void SerializeProto(Stream stream, object obj)
		{
			Serializer.NonGeneric.SerializeWithLengthPrefix(
				stream, obj, PrefixStyle.Base128, IDFromProtoContract(obj.GetType()));
		}

		public object DeserializeProto(Stream stream)
		{
			object obj;
			Serializer.NonGeneric.TryDeserializeWithLengthPrefix(
				stream, PrefixStyle.Base128, i => ProtoContractFromID(i), out obj);
			return obj;
		}

		[AttributeUsage(AttributeTargets.Constructor)]
		public sealed class UseCtorAttribute : Attribute { }
	}
}
