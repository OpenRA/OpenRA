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

using System.IO;
using System.Runtime.CompilerServices;
using OpenRA.Primitives;

namespace OpenRA
{
	public static class ActorInitLoader
	{
		public static TypeDictionary LoadInits(MiniYaml yaml)
		{
			var dict = new TypeDictionary();
			foreach (var i in yaml.Nodes)
			{
				var init = LoadInit(i.Key, i.Value);
				if (init is ISingleInstanceInit && dict.Contains(init.GetType()))
					throw new InvalidDataException($"Duplicate initializer '{init.GetType().Name}'");

				dict.Add(init);
			}

			return dict;
		}

		public static ActorInit LoadInit(string initName, MiniYaml initYaml)
		{
			var initInstance = initName.Split(ActorInfo.TraitInstanceSeparator);
			var type = Game.ModData.ObjectCreator.FindType(initInstance[0] + "Init");
			if (type == null)
				throw new InvalidDataException($"Unknown initializer type '{initInstance[0]}Init'");

			var init = (ActorInit)RuntimeHelpers.GetUninitializedObject(type);
			if (initInstance.Length > 1)
				type.GetField(nameof(ActorInit.InstanceName)).SetValue(init, initInstance[1]);

			var loader = type.GetMethod("Initialize", [typeof(MiniYaml)]);
			if (loader == null)
				throw new InvalidDataException($"{initInstance[0]}Init does not define a yaml-assignable type.");

			loader.Invoke(init, [initYaml]);
			return init;
		}
	}
}
