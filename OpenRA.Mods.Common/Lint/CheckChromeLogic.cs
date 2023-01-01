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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	class CheckChromeLogic : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var filename in modData.Manifest.ChromeLayout)
				CheckInner(MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename), filename, emitError);
		}

		void CheckInner(List<MiniYamlNode> nodes, string filename, Action<string> emitError)
		{
			foreach (var node in nodes)
			{
				if (node.Value == null)
					continue;

				if (node.Key == "Logic")
				{
					var typeNames = FieldLoader.GetValue<string[]>(node.Key, node.Value.Value);
					foreach (var typeName in typeNames)
					{
						var type = Game.ModData.ObjectCreator.FindType(typeName);
						if (type == null)
							emitError($"{filename} refers to a logic object `{typeName}` that does not exist");
						else if (!typeof(ChromeLogic).IsAssignableFrom(type))
							emitError($"{filename} refers to a logic object `{typeName}` that does not inherit from ChromeLogic");
					}
				}

				if (node.Value.Nodes != null)
					CheckInner(node.Value.Nodes, filename, emitError);
			}
		}
	}
}
