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
		readonly KeySettings settings;
		readonly Dictionary<string, HotkeyDefinition> keys = new Dictionary<string, HotkeyDefinition>();

		public HotkeyManager(IReadOnlyFileSystem fileSystem, KeySettings settings, Manifest manifest)
		{
			this.settings = settings;

			var keyDefinitions = MiniYaml.Load(fileSystem, manifest.Hotkeys, null);
			foreach (var kd in keyDefinitions)
				keys[kd.Key] = new HotkeyDefinition(kd.Key, kd.Value);
		}

		internal Func<Hotkey> GetHotkeyReference(string name)
		{
			var ret = settings.GetHotkeyReference(name);
			if (ret != null)
				return ret;

			HotkeyDefinition keyDefinition;
			if (keys.TryGetValue(name, out keyDefinition))
				return () => keyDefinition.Default;

			// Not a mod-defined hotkey, so try and parse as a hardcoded definition
			Hotkey key;
			if (!Hotkey.TryParse(name, out key))
				key = Hotkey.Invalid;

			return () => key;
		}

		public void Set(string name, Hotkey value)
		{
			var field = settings.GetType().GetField(name + "Key");
			if (field == null)
				return;

			field.SetValue(settings, value);
		}

		public HotkeyReference this[string name]
		{
			get
			{
				return new HotkeyReference(GetHotkeyReference(name));
			}
		}

		public IEnumerable<HotkeyDefinition> Definitions { get { return keys.Values; } }
	}
}
