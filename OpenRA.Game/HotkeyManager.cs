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
				keys[kv.Key] = kv.Value;
		}

		internal Func<Hotkey> GetHotkeyReference(string name)
		{
			// Is this a mod-defined hotkey?
			if (keys.ContainsKey(name))
				return () => keys[name];

			// Try and parse as a hardcoded definition
			Hotkey key;
			if (!Hotkey.TryParse(name, out key))
				key = Hotkey.Invalid;

			return () => key;
		}

		public void Set(string name, Hotkey value)
		{
			HotkeyDefinition definition;
			if (!definitions.TryGetValue(name, out definition))
				return;

			keys[name] = value;
			if (value != definition.Default)
				settings[name] = value;
			else
				settings.Remove(name);
		}

		public HotkeyReference this[string name]
		{
			get
			{
				return new HotkeyReference(GetHotkeyReference(name));
			}
		}

		public IEnumerable<HotkeyDefinition> Definitions { get { return definitions.Values; } }
	}
}
