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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public interface ISuppressInitExport { }

	public class ActorReference : IEnumerable
	{
		public string Type;
		readonly Lazy<TypeDictionary> initDict;

		internal TypeDictionary InitDict => initDict.Value;

		public ActorReference(string type)
			: this(type, new Dictionary<string, MiniYaml>()) { }

		public ActorReference(string type, Dictionary<string, MiniYaml> inits)
		{
			Type = type;
			initDict = Exts.Lazy(() =>
			{
				var dict = new TypeDictionary();
				foreach (var i in inits)
				{
					var init = LoadInit(i.Key, i.Value);
					if (init is ISingleInstanceInit && dict.Contains(init.GetType()))
						throw new InvalidDataException($"Duplicate initializer '{init.GetType().Name}'");

					dict.Add(init);
				}

				return dict;
			});
		}

		public ActorReference(string type, TypeDictionary inits)
		{
			Type = type;
			initDict = new Lazy<TypeDictionary>(() =>
			{
				var dict = new TypeDictionary();
				foreach (var i in inits)
					dict.Add(i);
				return dict;
			});
		}

		static ActorInit LoadInit(string initName, MiniYaml initYaml)
		{
			var initInstance = initName.Split(ActorInfo.TraitInstanceSeparator);
			var type = Game.ModData.ObjectCreator.FindType(initInstance[0] + "Init");
			if (type == null)
				throw new InvalidDataException($"Unknown initializer type '{initInstance[0]}Init'");

			var init = (ActorInit)FormatterServices.GetUninitializedObject(type);
			if (initInstance.Length > 1)
				type.GetField(nameof(ActorInit.InstanceName)).SetValue(init, initInstance[1]);

			var loader = type.GetMethod("Initialize", new[] { typeof(MiniYaml) });
			if (loader == null)
				throw new InvalidDataException($"{initInstance[0]}Init does not define a yaml-assignable type.");

			loader.Invoke(init, new[] { initYaml });
			return init;
		}

		public MiniYaml Save(Func<ActorInit, bool> initFilter = null)
		{
			var ret = new MiniYaml(Type);
			foreach (var o in initDict.Value)
			{
				if (!(o is ActorInit init) || o is ISuppressInitExport)
					continue;

				if (initFilter != null && !initFilter(init))
					continue;

				var initTypeName = init.GetType().Name;
				var initName = initTypeName.Substring(0, initTypeName.Length - 4);
				if (!string.IsNullOrEmpty(init.InstanceName))
					initName += ActorInfo.TraitInstanceSeparator + init.InstanceName;

				ret.Nodes.Add(new MiniYamlNode(initName, init.Save()));
			}

			return ret;
		}

		public IEnumerator GetEnumerator() { return initDict.Value.GetEnumerator(); }

		public ActorReference Clone()
		{
			var clone = new ActorReference(Type);
			foreach (var init in initDict.Value)
				clone.initDict.Value.Add(init);

			return clone;
		}

		public void Add(ActorInit init)
		{
			if (init is ISingleInstanceInit && InitDict.Contains(init.GetType()))
				throw new InvalidDataException($"Duplicate initializer '{init.GetType().Name}'");

			InitDict.Add(init);
		}

		public void Remove(ActorInit o) { initDict.Value.Remove(o); }

		public int RemoveAll<T>() where T : ActorInit
		{
			var removed = 0;
			foreach (var o in initDict.Value.WithInterface<T>().ToList())
			{
				removed++;
				initDict.Value.Remove(o);
			}

			return removed;
		}

		public IEnumerable<T> GetAll<T>() where T : ActorInit
		{
			return initDict.Value.WithInterface<T>();
		}

		public T GetOrDefault<T>(TraitInfo info) where T : ActorInit
		{
			var inits = initDict.Value.WithInterface<T>();

			// Traits tagged with an instance name prefer inits with the same name.
			// If a more specific init is not available, fall back to an unnamed init.
			// If duplicate inits are defined, take the last to match standard yaml override expectations
			if (info != null && !string.IsNullOrEmpty(info.InstanceName))
				return inits.LastOrDefault(i => i.InstanceName == info.InstanceName) ??
				       inits.LastOrDefault(i => string.IsNullOrEmpty(i.InstanceName));

			// Untagged traits will only use untagged inits
			return inits.LastOrDefault(i => string.IsNullOrEmpty(i.InstanceName));
		}

		public T Get<T>(TraitInfo info) where T : ActorInit
		{
			var init = GetOrDefault<T>(info);
			if (init == null)
				throw new InvalidOperationException($"TypeDictionary does not contain instance of type `{typeof(T)}`");

			return init;
		}

		public U GetValue<T, U>(TraitInfo info) where T : ValueActorInit<U>
		{
			return Get<T>(info).Value;
		}

		public U GetValue<T, U>(TraitInfo info, U fallback) where T : ValueActorInit<U>
		{
			var init = GetOrDefault<T>(info);
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>(TraitInfo info) where T : ActorInit { return GetOrDefault<T>(info) != null; }

		public T GetOrDefault<T>() where T : ActorInit, ISingleInstanceInit
		{
			return initDict.Value.GetOrDefault<T>();
		}

		public T Get<T>() where T : ActorInit, ISingleInstanceInit
		{
			return initDict.Value.Get<T>();
		}

		public U GetValue<T, U>() where T : ValueActorInit<U>, ISingleInstanceInit
		{
			return Get<T>().Value;
		}

		public U GetValue<T, U>(U fallback) where T : ValueActorInit<U>, ISingleInstanceInit
		{
			var init = GetOrDefault<T>();
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>() where T : ActorInit, ISingleInstanceInit { return GetOrDefault<T>() != null; }
	}
}
