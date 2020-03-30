#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractWeaponDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--weapon-docs"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[VERSION]", "Generate weaponry documentation in MarkDown format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			Console.WriteLine(
				"This documentation is aimed at modders. It displays a template for weapon definitions " +
				"as well as its contained types (warheads and projectiles) with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", version);
			Console.WriteLine();

			var toc = new StringBuilder();
			var doc = new StringBuilder();

			var currentNamespace = "";

			var objectCreator = utility.ModData.ObjectCreator;
			var weaponInfo = objectCreator.GetTypesImplementing<WeaponInfo>();
			var warheads = objectCreator.GetTypesImplementing<IWarhead>().OrderBy(t => t.Namespace);
			var projectiles = objectCreator.GetTypesImplementing<IProjectileInfo>().OrderBy(t => t.Namespace);

			var weaponTypes = Enumerable.Concat(weaponInfo, Enumerable.Concat(projectiles, warheads));
			foreach (var t in weaponTypes)
			{
				// skip helpers like TraitInfo<T>
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue;

				if (currentNamespace != t.Namespace)
				{
					currentNamespace = t.Namespace;
					doc.AppendLine();
					doc.AppendLine("## {0}".F(currentNamespace));
					toc.AppendLine("* [{0}](#{1})".F(currentNamespace, currentNamespace.Replace(".", "").ToLowerInvariant()));
				}

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				toc.AppendLine("  * [{0}](#{1})".F(traitName, traitName.ToLowerInvariant()));
				doc.AppendLine();
				doc.AppendLine("### {0}".F(traitName));

				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				foreach (var line in traitDescLines)
					doc.AppendLine(line);

				var infos = FieldLoader.GetTypeLoadInfo(t);
				if (!infos.Any())
					continue;

				doc.AppendLine("<table>");
				doc.AppendLine("<tr><th>Property</th><th>Default Value</th><th>Type</th><th>Description</th></tr>");

				var liveTraitInfo = t == typeof(WeaponInfo) ? null : objectCreator.CreateBasic(t);
				foreach (var info in infos)
				{
					var fieldDescLines = info.Field.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = Util.FriendlyTypeName(info.Field.FieldType);
					var defaultValue = liveTraitInfo == null ? "" : FieldSaver.SaveField(liveTraitInfo, info.Field.Name).Value.Value;
					doc.Append("<tr><td>{0}</td><td>{1}</td><td>{2}</td>".F(info.YamlName, defaultValue, fieldType));
					doc.Append("<td>");

					foreach (var line in fieldDescLines)
						doc.Append(line + " ");

					doc.AppendLine("</td></tr>");
				}

				doc.AppendLine("</table>");
			}

			Console.Write(toc.ToString());
			Console.Write(doc.ToString());
		}
	}
}
