#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;
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
							emitError("{0} refers to a logic object `{1}` that does not exist".F(filename, typeName));
						else if (!typeof(ChromeLogic).IsAssignableFrom(type))
							emitError("{0} refers to a logic object `{1}` that does not inherit from ChromeLogic".F(filename, typeName));
					}
				}

				if (node.Value.Nodes != null)
					CheckInner(node.Value.Nodes, filename, emitError);
			}
		}
	}
}