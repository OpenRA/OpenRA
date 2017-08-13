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
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	class CheckChromeHotkeys : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			// Build the list of valid key names
			// For now they are hardcoded, but this will change.
			var namedKeys = typeof(KeySettings).GetFields()
				.Where(x => x.Name.EndsWith("Key", StringComparison.Ordinal))
				.Select(x => x.Name.Substring(0, x.Name.Length - 3))
				.ToArray();

			// Build the list of widget keys to validate
			var checkWidgetFields = modData.ObjectCreator.GetTypesImplementing<Widget>()
				.SelectMany(w => w.GetFields()
					.Where(f => f.FieldType == typeof(NamedHotkey))
					.Select(f => Pair.New(w.Name.Substring(0, w.Name.Length - 6), f.Name)))
				.ToArray();

			foreach (var filename in modData.Manifest.ChromeLayout)
				CheckInner(modData, namedKeys, checkWidgetFields, MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename), filename, null, emitError);
		}

		void CheckInner(ModData modData, string[] namedKeys, Pair<string, string>[] checkWidgetFields,
			List<MiniYamlNode> nodes, string filename, MiniYamlNode parent, Action<string> emitError)
		{
			foreach (var node in nodes)
			{
				if (node.Value == null)
					continue;

				foreach (var x in checkWidgetFields)
				{
					if (node.Key == x.Second && parent != null && parent.Key.StartsWith(x.First, StringComparison.Ordinal))
					{
						// Keys are valid if they refer to a named key or can be parsed as a regular Hotkey.
						Hotkey unused;
						if (!namedKeys.Contains(node.Value.Value) && !Hotkey.TryParse(node.Value.Value, out unused))
							emitError("{0} refers to a Key named `{1}` that does not exist".F(node.Location, node.Value.Value));
					}
				}

				if (node.Value.Nodes != null)
					CheckInner(modData, namedKeys, checkWidgetFields, node.Value.Nodes, filename, node, emitError);
			}
		}
	}
}