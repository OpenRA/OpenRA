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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface ISuppressInitExport { }

	public class ActorReference : IEnumerable
	{
		public string Type;
		public TypeDictionary InitDict
		{
			get { return initDict.Value; }
		}

		Lazy<TypeDictionary> initDict;

		public ActorReference(string type)
			: this(type, new Dictionary<string, MiniYaml>()) { }

		public ActorReference(string type, Dictionary<string, MiniYaml> inits)
		{
			Type = type;
			initDict = Exts.Lazy(() =>
			{
				var dict = new TypeDictionary();
				foreach (var i in inits)
					dict.Add(LoadInit(i.Key, i.Value));
				return dict;
			});
		}

		static ActorInit LoadInit(string initName, MiniYaml initYaml)
		{
			var initInstance = initName.Split(ActorInfo.TraitInstanceSeparator);
			var type = Game.ModData.ObjectCreator.FindType(initInstance[0] + "Init");
			if (type == null)
				throw new InvalidDataException("Unknown initializer type '{0}Init'".F(initInstance[0]));

			var init = (ActorInit)FormatterServices.GetUninitializedObject(type);
			if (initInstance.Length > 1)
				type.GetField("InstanceName").SetValue(init, initInstance[1]);

			var loader = type.GetMethod("Initialize", new[] { typeof(MiniYaml) });
			if (loader == null)
				throw new InvalidDataException("{0}Init does not define a yaml-assignable type.".F(initInstance[0]));

			loader.Invoke(init, new[] { initYaml });
			return init;
		}

		public MiniYaml Save(Func<ActorInit, bool> initFilter = null)
		{
			var ret = new MiniYaml(Type);
			foreach (var o in InitDict)
			{
				var init = o as ActorInit;
				if (init == null || o is ISuppressInitExport)
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

		// for initialization syntax
		public void Add(object o) { InitDict.Add(o); }
		public IEnumerator GetEnumerator() { return InitDict.GetEnumerator(); }

		public ActorReference Clone()
		{
			var clone = new ActorReference(Type);
			foreach (var init in InitDict)
				clone.InitDict.Add(init);

			return clone;
		}
	}
}
