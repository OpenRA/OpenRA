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
using OpenRA.FileSystem;

namespace OpenRA
{
	public sealed class HotkeyManager
	{
		readonly Dictionary<string, Hotkey> settings;
		readonly Dictionary<string, HotkeyDefinition> definitions = new Dictionary<string, HotkeyDefinition>();
		readonly Dictionary<string, Hotkey> keys = new Dictionary<string, Hotkey>();

		public HotkeyManager(IReadOnlyFileSystem fileSystem, Dictionary<string, Hotkey> settings, Manifest manifest)
		{
			this.settings = settings;

			var keyDefinitions = MiniYaml.Load(fileSystem, manifest.Hotkeys, null);
			foreach (var kd in keyDefinitions)
			{
				var definition = new HotkeyDefinition(kd.Key, kd.Value);
				definitions[kd.Key] = definition;
				keys[kd.Key] = definition.Default;
			}

			foreach (var kv in settings)
			{
				if (definitions.ContainsKey(kv.Key) && !definitions[kv.Key].Readonly)
					keys[kv.Key] = kv.Value;
			}

			foreach (var hd in definitions)
				hd.Value.HasDuplicates = GetFirstDuplicate(hd.Value, this[hd.Value.Name].GetValue()) != null;
		}

		internal Func<Hotkey> GetHotkeyReference(string name)
		{
			// Is this a mod-defined hotkey?
			if (keys.ContainsKey(name))
				return () => keys[name];

			// Try and parse as a hardcoded definition
			if (!Hotkey.TryParse(name, out var key))
				key = Hotkey.Invalid;

			return () => key;
		}

		public void Set(string name, Hotkey value)
		{
			if (!definitions.TryGetValue(name, out var definition))
				return;

			if (definition.Readonly)
				return;

			keys[name] = value;
			if (value != definition.Default)
				settings[name] = value;
			else
				settings.Remove(name);

			var hadDuplicates = definition.HasDuplicates;
			definition.HasDuplicates = GetFirstDuplicate(definition, this[definition.Name].GetValue()) != null;

			if (hadDuplicates || definition.HasDuplicates)
			{
				foreach (var hd in definitions)
				{
					if (hd.Value == definition)
						continue;

					hd.Value.HasDuplicates = GetFirstDuplicate(hd.Value, this[hd.Value.Name].GetValue()) != null;
				}
			}
		}

		public HotkeyDefinition GetFirstDuplicate(HotkeyDefinition definition, Hotkey value)
		{
			if (definition == null)
				return null;

			foreach (var kv in keys)
			{
				if (kv.Key == definition.Name)
					continue;

				if (kv.Value == value && definitions[kv.Key].Contexts.Overlaps(definition.Contexts))
					return definitions[kv.Key];
			}

			return null;
		}

		public HotkeyReference this[string name] => new HotkeyReference(GetHotkeyReference(name));

		public IEnumerable<HotkeyDefinition> Definitions => definitions.Values;
	}
}
