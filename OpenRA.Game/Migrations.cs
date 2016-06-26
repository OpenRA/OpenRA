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
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class Migrations : IGlobalModData
	{
		[FieldLoader.Ignore]
		readonly List<MiniYamlNode> rules;

		public Migrations(MiniYaml yaml)
		{
			rules = yaml.Nodes;
		}

		public bool Run()
		{
			var appliedRule = false;
			foreach (var rule in rules)
			{
				var path = Platform.ResolvePath(rule.Value.Value);
				if (!File.Exists(path))
					continue;

				var lengthNode = rule.Value.Nodes.FirstOrDefault(n => n.Key == "Length");
				if (lengthNode != null)
				{
					var matchLength = FieldLoader.GetValue<int>("Length", lengthNode.Value.Value);
					var actualLength = new FileInfo(path).Length;
					if (matchLength != actualLength)
						continue;
				}

				switch (rule.Key)
				{
					case "delete":
						Log.Write("debug", "Migration: Deleting file {0}", path);
						Console.WriteLine("Migration: Deleting file {0}", path);

						File.Delete(path);
						appliedRule = true;
						break;
					default:
						Log.Write("debug", "Unknown migration command {0} - ignoring", rule.Value.Value);
						break;
				}
			}

			return appliedRule;
		}
	}
}
