#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		string IUtilityCommand.Name { get { return "--docs"; } }

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
				"automatically generated for version {0} of OpenRA.", version);
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
					doc.AppendLine("## {0}".F(currentNamespace));
				}

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				doc.AppendLine();
				doc.AppendLine("### {0}".F(traitName));
				foreach (var line in traitDescLines)
					doc.AppendLine(line);

				var requires = RequiredTraitTypes(t);
				var reqCount = requires.Length;
				if (reqCount > 0)
				{
					if (t.HasAttribute<DescAttribute>())
						doc.AppendLine();

					doc.Append("Requires trait{0}: ".F(reqCount > 1 ? "s" : ""));

					var i = 0;
					foreach (var require in requires)
					{
						var n = require.Name;
						var name = n.EndsWith("Info") ? n.Remove(n.Length - 4, 4) : n;
						doc.Append("[`{0}`](#{1}){2}".F(name, name.ToLowerInvariant(), i + 1 == reqCount ? ".\n" : ", "));
						i++;
					}
				}

				var infos = FieldLoader.GetTypeLoadInfo(t);
				if (!infos.Any())
					continue;
				doc.AppendLine("<table>");
				doc.AppendLine("<tr><th>Property</th><th>Default Value</th><th>Type</th><th>Description</th></tr>");
				var liveTraitInfo = Game.ModData.ObjectCreator.CreateBasic(t);
				foreach (var info in infos)
				{
					var fieldDescLines = info.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = Util.FriendlyTypeName(info.Field.FieldType);
					var loadInfo = info.Field.GetCustomAttributes<FieldLoader.SerializeAttribute>(true).FirstOrDefault();
					var defaultValue = loadInfo != null && loadInfo.Required ? "<em>(required)</em>" : FieldSaver.SaveField(liveTraitInfo, info.Field.Name).Value.Value;
					doc.Append("<tr><td>{0}</td><td>{1}</td><td>{2}</td>".F(info.YamlName, defaultValue, fieldType));
					doc.Append("<td>");
					foreach (var line in fieldDescLines)
						doc.Append(line + " ");
					doc.AppendLine("</td></tr>");
				}

				doc.AppendLine("</table>");
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
