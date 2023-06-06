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
using System.Collections.ObjectModel;
using OpenRA.Support;

namespace OpenRA.Mods.Common.Lint
{
	sealed class CheckChromeIntegerExpressions : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var filename in modData.Manifest.ChromeLayout)
				CheckInner(MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename), filename, emitError);
		}

		void CheckInner(List<MiniYamlNode> nodes, string filename, Action<string> emitError)
		{
			var substitutions = new Dictionary<string, int>();
			var readOnlySubstitutions = new ReadOnlyDictionary<string, int>(substitutions);

			foreach (var node in nodes)
			{
				if (node.Value == null)
					continue;

				if (node.Key == "X" || node.Key == "Y" || node.Key == "Width" || node.Key == "Height")
				{
					try
					{
						FieldLoader.GetValue<IntegerExpression>(node.Key, node.Value.Value);
					}
					catch (YamlException e)
					{
						emitError($"Failed to parse integer expression in {node}: {e.Message}");
					}
				}

				if (node.Value.Nodes != null)
					CheckInner(node.Value.Nodes, filename, emitError);
			}
		}
	}
}
