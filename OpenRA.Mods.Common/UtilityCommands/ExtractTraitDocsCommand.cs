#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractTraitDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--docs";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[VERSION]", "Generate trait documentation in MarkDown format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			Console.WriteLine(
				"This documentation is aimed at modders. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				$"automatically generated for version {version} of OpenRA.");
			Console.WriteLine();

			var doc = new StringBuilder();
			var currentNamespace = "";

			foreach (var t in Game.ModData.ObjectCreator.GetTypesImplementing<TraitInfo>().OrderBy(t => t.Namespace))
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				if (currentNamespace != t.Namespace)
				{
					currentNamespace = t.Namespace;
					doc.AppendLine();
					doc.AppendLine($"## {currentNamespace}");
				}

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				doc.AppendLine();
				doc.AppendLine($"### {traitName}");
				foreach (var line in traitDescLines)
					doc.AppendLine(line);

				var requires = RequiredTraitTypes(t);
				var reqCount = requires.Length;
				if (reqCount > 0)
				{
					if (t.HasAttribute<DescAttribute>())
						doc.AppendLine();

					doc.Append($"Requires trait{(reqCount > 1 ? "s" : "")}: ");

					var i = 0;
					foreach (var require in requires)
					{
						var n = require.Name;
						var name = n.EndsWith("Info") ? n.Remove(n.Length - 4, 4) : n;
						doc.Append($"[`{name}`](#{name.ToLowerInvariant()}){(i + 1 == reqCount ? ".\n" : ", ")}");
						i++;
					}
				}

				var infos = FieldLoader.GetTypeLoadInfo(t);
				if (!infos.Any())
					continue;
				doc.AppendLine();
				doc.AppendLine("| Property | Default Value | Type | Description |");
				doc.AppendLine("| -------- | --------------| ---- | ----------- |");
				var liveTraitInfo = Game.ModData.ObjectCreator.CreateBasic(t);
				foreach (var info in infos)
				{
					var fieldDescLines = info.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = Util.FriendlyTypeName(info.Field.FieldType);
					var loadInfo = info.Field.GetCustomAttributes<FieldLoader.SerializeAttribute>(true).FirstOrDefault();
					var defaultValue = loadInfo != null && loadInfo.Required ? "*(required)*" : FieldSaver.SaveField(liveTraitInfo, info.Field.Name).Value.Value;
					doc.Append($"| {info.YamlName} | {defaultValue} | {fieldType} | ");
					foreach (var line in fieldDescLines)
						doc.Append(line + " ");
					doc.AppendLine("|");
				}
			}

			Console.Write(doc.ToString());
		}

		static Type[] RequiredTraitTypes(Type t)
		{
			return t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>))
				.SelectMany(i => i.GetGenericArguments())
				.Where(i => !i.IsInterface && !t.IsSubclassOf(i))
				.OrderBy(i => i.Name)
				.ToArray();
		}
	}
}
