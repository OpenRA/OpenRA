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

using System.Collections.Generic;

namespace OpenRA
{
	public sealed class HotkeyDefinition
	{
		public readonly string Name;
		public readonly Hotkey Default = Hotkey.Invalid;
		public readonly string Description = "";
		public readonly HashSet<string> Types = new();
		public readonly HashSet<string> Contexts = new();
		public readonly bool Readonly = false;
		public bool HasDuplicates { get; internal set; }

		public HotkeyDefinition(string name, MiniYaml node)
		{
			Name = name;

			if (!string.IsNullOrEmpty(node.Value))
				Default = FieldLoader.GetValue<Hotkey>("value", node.Value);

			var nodeDict = node.ToDictionary();

			if (nodeDict.TryGetValue("Description", out var descriptionYaml))
				Description = descriptionYaml.Value;

			if (nodeDict.TryGetValue("Types", out var typesYaml))
				Types = FieldLoader.GetValue<HashSet<string>>("Types", typesYaml.Value);

			if (nodeDict.TryGetValue("Contexts", out var contextYaml))
				Contexts = FieldLoader.GetValue<HashSet<string>>("Contexts", contextYaml.Value);

			if (nodeDict.TryGetValue("Platform", out var platformYaml))
			{
				var platformOverride = platformYaml.NodeWithKeyOrDefault(Platform.CurrentPlatform.ToString());
				if (platformOverride != null)
					Default = FieldLoader.GetValue<Hotkey>("value", platformOverride.Value.Value);
			}

			if (nodeDict.TryGetValue("Readonly", out var readonlyYaml))
				Readonly = FieldLoader.GetValue<bool>("Readonly", readonlyYaml.Value);
		}
	}
}
