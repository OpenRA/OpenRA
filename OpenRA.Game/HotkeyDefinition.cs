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
using System.Linq;

namespace OpenRA
{
	public sealed class HotkeyDefinition
	{
		public readonly string Name;
		public readonly Hotkey Default = Hotkey.Invalid;
		public readonly string Description = "";
		public readonly HashSet<string> Types = new HashSet<string>();
		public readonly HashSet<string> Contexts = new HashSet<string>();
		public readonly bool Readonly = false;
		public bool HasDuplicates { get; internal set; }

		public HotkeyDefinition(string name, MiniYaml node)
		{
			Name = name;

			if (!string.IsNullOrEmpty(node.Value))
				Default = FieldLoader.GetValue<Hotkey>("value", node.Value);

			var descriptionNode = node.Nodes.FirstOrDefault(n => n.Key == "Description");
			if (descriptionNode != null)
				Description = descriptionNode.Value.Value;

			var typesNode = node.Nodes.FirstOrDefault(n => n.Key == "Types");
			if (typesNode != null)
				Types = FieldLoader.GetValue<HashSet<string>>("Types", typesNode.Value.Value);

			var contextsNode = node.Nodes.FirstOrDefault(n => n.Key == "Contexts");
			if (contextsNode != null)
				Contexts = FieldLoader.GetValue<HashSet<string>>("Contexts", contextsNode.Value.Value);

			var platformNode = node.Nodes.FirstOrDefault(n => n.Key == "Platform");
			if (platformNode != null)
			{
				var platformOverride = platformNode.Value.Nodes.FirstOrDefault(n => n.Key == Platform.CurrentPlatform.ToString());
				if (platformOverride != null)
					Default = FieldLoader.GetValue<Hotkey>("value", platformOverride.Value.Value);
			}

			var readonlyNode = node.Nodes.FirstOrDefault(n => n.Key == "Readonly");
			if (readonlyNode != null)
				Readonly = FieldLoader.GetValue<bool>("Readonly", readonlyNode.Value.Value);
		}
	}
}
