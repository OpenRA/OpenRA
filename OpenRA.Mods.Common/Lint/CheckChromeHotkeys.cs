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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Lint
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ChromeLogicArgsHotkeys : Attribute
	{
		public string[] LogicArgKeys;
		public ChromeLogicArgsHotkeys(params string[] logicArgKeys)
		{
			LogicArgKeys = logicArgKeys;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class CustomLintableHotkeyNames : Attribute { }

	class CheckChromeHotkeys : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			// Build the list of valid hotkey names
			var namedKeys = modData.Hotkeys.Definitions.Select(d => d.Name).ToArray();

			// Build the list of widget keys to validate
			var checkWidgetFields = modData.ObjectCreator.GetTypesImplementing<Widget>()
				.SelectMany(w => w.GetFields()
					.Where(f => f.FieldType == typeof(HotkeyReference))
					.Select(f => Pair.New(w.Name.Substring(0, w.Name.Length - 6), f.Name)))
				.ToArray();

			var customLintMethods = new Dictionary<string, List<string>>();

			foreach (var w in modData.ObjectCreator.GetTypesImplementing<Widget>())
			{
				foreach (var m in w.GetMethods().Where(m => m.HasAttribute<CustomLintableHotkeyNames>()))
				{
					var p = m.GetParameters();
					if (p.Length == 3 && p[0].ParameterType == typeof(MiniYamlNode) && p[1].ParameterType == typeof(Action<string>)
							&& p[2].ParameterType == typeof(Action<string>))
						customLintMethods.GetOrAdd(w.Name.Substring(0, w.Name.Length - 6)).Add(m.Name);
				}
			}

			foreach (var filename in modData.Manifest.ChromeLayout)
			{
				var yaml = MiniYaml.FromStream(modData.DefaultFileSystem.Open(filename), filename);
				CheckInner(modData, namedKeys, checkWidgetFields, customLintMethods, yaml, filename, null, emitError, emitWarning);
			}
		}

		void CheckInner(ModData modData, string[] namedKeys, Pair<string, string>[] checkWidgetFields, Dictionary<string, List<string>> customLintMethods,
			List<MiniYamlNode> nodes, string filename, MiniYamlNode parent, Action<string> emitError, Action<string> emitWarning)
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

				// Check runtime-defined hotkey names
				List<string> checkMethods;
				var widgetType = node.Key.Split('@')[0];
				if (customLintMethods.TryGetValue(widgetType, out checkMethods))
				{
					var type = modData.ObjectCreator.FindType(widgetType + "Widget");
					var keyNames = checkMethods.SelectMany(m => (IEnumerable<string>)type.GetMethod(m).Invoke(null, new object[] { node, emitError, emitWarning }));

					Hotkey unused;
					foreach (var name in keyNames)
						if (!namedKeys.Contains(name) && !Hotkey.TryParse(name, out unused))
							emitError("{0} refers to a Key named `{1}` that does not exist".F(node.Location, name));
				}

				// Logic classes can declare the data key names that specify hotkeys
				if (node.Key == "Logic" && node.Value.Nodes.Any())
				{
					var typeNames = FieldLoader.GetValue<string[]>(node.Key, node.Value.Value);
					var checkArgKeys = new List<string>();
					foreach (var typeName in typeNames)
					{
						var type = Game.ModData.ObjectCreator.FindType(typeName);
						if (type == null)
							continue;

						checkArgKeys.AddRange(type.GetCustomAttributes<ChromeLogicArgsHotkeys>(true).SelectMany(x => x.LogicArgKeys));
					}

					Hotkey unused;
					foreach (var n in node.Value.Nodes)
						if (checkArgKeys.Contains(n.Key))
							if (!namedKeys.Contains(n.Value.Value) && !Hotkey.TryParse(n.Value.Value, out unused))
								emitError("{0} {1}:{2} refers to a Key named `{3}` that does not exist".F(filename, node.Value.Value, n.Key, n.Value.Value));
				}

				if (node.Value.Nodes != null)
					CheckInner(modData, namedKeys, checkWidgetFields, customLintMethods, node.Value.Nodes, filename, node, emitError, emitWarning);
			}
		}
	}
}