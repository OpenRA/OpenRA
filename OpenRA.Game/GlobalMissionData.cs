#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class GlobalMissionData
	{
		readonly string missionDataFile;

		public readonly Dictionary<string, string> SavedMissionData = new Dictionary<string, string>();

		public readonly Dictionary<string, object> Sections;

		// A direct clone of the file loaded from disk.
		// Any changes will be merged over this on save,
		// allowing us to persist any unknown configuration keys
		readonly List<MiniYamlNode> yamlCache = new List<MiniYamlNode>();

		public GlobalMissionData(string file, Arguments args)
		{
			missionDataFile = file;
			Sections = new Dictionary<string, object>()
			{
				{ "SavedMissionData", SavedMissionData },
			};

			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			try
			{
				FieldLoader.UnknownFieldAction = (s, f) => Console.WriteLine("Ignoring unknown field `{0}` on `{1}`".F(s, f.Name));

				if (File.Exists(missionDataFile))
				{
					yamlCache = MiniYaml.FromFile(missionDataFile);
					foreach (var yamlSection in yamlCache)
					{
						object settingsSection;
						if (Sections.TryGetValue(yamlSection.Key, out settingsSection))
							LoadSectionYaml(yamlSection.Value, settingsSection);
					}
				}

				// Override with commandline args
				foreach (var kv in Sections)
					foreach (var f in kv.Value.GetType().GetFields())
						if (args.Contains(kv.Key + "." + f.Name))
							FieldLoader.LoadField(kv.Value, f.Name, args.GetValue(kv.Key + "." + f.Name, ""));
			}
			finally
			{
				FieldLoader.UnknownFieldAction = err1;
				FieldLoader.InvalidValueAction = err2;
			}
		}

		public void Save()
		{
			foreach (var kv in Sections)
			{
				var sectionYaml = yamlCache.FirstOrDefault(x => x.Key == kv.Key);
				if (sectionYaml == null)
				{
					sectionYaml = new MiniYamlNode(kv.Key, new MiniYaml(""));
					yamlCache.Add(sectionYaml);
				}

				var defaultValues = Activator.CreateInstance(kv.Value.GetType());
				var fields = FieldLoader.GetTypeLoadInfo(kv.Value.GetType());
				foreach (var fli in fields)
				{
					var serialized = FieldSaver.FormatValue(kv.Value, fli.Field);
					var defaultSerialized = FieldSaver.FormatValue(defaultValues, fli.Field);

					// Fields with their default value are not saved in the settings yaml
					// Make sure that we erase any previously defined custom values
					if (serialized == defaultSerialized)
						sectionYaml.Value.Nodes.RemoveAll(n => n.Key == fli.YamlName);
					else
					{
						// Update or add the custom value
						var fieldYaml = sectionYaml.Value.Nodes.FirstOrDefault(n => n.Key == fli.YamlName);
						if (fieldYaml != null)
							fieldYaml.Value.Value = serialized;
						else
							sectionYaml.Value.Nodes.Add(new MiniYamlNode(fli.YamlName, new MiniYaml(serialized)));
					}
				}
			}

			yamlCache.WriteToFile(missionDataFile);
		}

		static void LoadSectionYaml(MiniYaml yaml, object section)
		{
			var defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s, t, f) =>
			{
				var ret = defaults.GetType().GetField(f).GetValue(defaults);
				Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s, t.Name, f, ret));
				return ret;
			};

			FieldLoader.Load(section, yaml);
		}
	}
}
