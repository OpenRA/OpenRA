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
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace OpenRA.UtilityCommands
{
	sealed class ExtractChromeStringsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--extract-chrome-strings"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Extract translatable strings that are not yet localized and update chrome layout.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var types = modData.ObjectCreator.GetTypes();
			var translatableFields = types.SelectMany(t => t.GetFields())
				.Where(f => f.HasAttribute<TranslationReferenceAttribute>()).Distinct();

			foreach (var chromeLayout in modData.Manifest.ChromeLayout)
			{
				modData.ModFiles.TryGetPackageContaining(chromeLayout, out var chromePackage, out var chromeName);
				var chromePath = Path.Combine(chromePackage.Name, chromeName);

				var fluentFolder = modData.Manifest.Id + "|languages";
				var fluentPackage = modData.ModFiles.OpenPackage(fluentFolder);
				var fluentPath = Path.Combine(fluentPackage.Name, "chrome/en.ftl");
				Directory.CreateDirectory(Path.GetDirectoryName(fluentPath));

				using (var fluentWriter = new StreamWriter(fluentPath, append: true))
				{
					fluentWriter.WriteLine($"## {chromeLayout}");
					var yamlBuilder = MiniYaml.FromFile(chromePath, false).Select(n => new MiniYamlNodeBuilder(n)).ToList();
					FromChromeLayout(yamlBuilder, null, translatableFields.Select(t => t.Name).Distinct().ToArray(), null, fluentWriter);

					using (var chromeLayoutWriter = new StreamWriter(chromePath))
						chromeLayoutWriter.WriteLine(yamlBuilder.WriteToString());
				}
			}
		}

		internal static void FromChromeLayout(List<MiniYamlNodeBuilder> nodes, MiniYamlNodeBuilder parent, string[] translatables, string container, StreamWriter fluentFile)
		{
			var parentNode = parent != null && parent.Key != null ? parent.Key.Split('@') : null;
			var parentType = parent != null && parent.Key != null ? parentNode.First() : null;
			var parentLabel = parent != null && parent.Key != null ? parentNode.Last() : null;

			if ((parentType == "Background" || parentType == "Container") && char.IsUpper(parentLabel, 0))
				container = parentLabel;

			foreach (var node in nodes)
			{
				if (translatables.Contains(node.Key) && parentLabel != null)
				{
					var isLowercase = node.Value.Value != null && node.Value.Value == node.Value.Value.ToLowerInvariant();
					var alreadyTranslated = isLowercase && node.Value.Value.Any(c => c == '-');
					if (alreadyTranslated)
					{
						Console.WriteLine("Skipping " + node.Value.Value + " because it is already translated.");
						continue;
					}

					var widgetType = parentType.ToLowerInvariant().Replace('_', '-')
						.Replace("labelforinput", "label")
						.Replace("labelwithhighlight", "label")
						.Replace("dropdownbutton", "dropdown")
						.Replace("checkboxbutton", "checkbox")
						.Replace("menubutton", "button")
						.Replace("worldbutton", "button")
						.Replace("productiontypebutton", "button");

					var translationKey = widgetType;
					var fieldName = node.Key.ToLowerInvariant().Replace("text", "");
					if (!string.IsNullOrEmpty(fieldName))
						translationKey = $"{translationKey}-{fieldName}";

					var parentPart = parentLabel.ToLowerInvariant().Replace('_', '-');
					if (container != null)
					{
						var containerType = container.ToLowerInvariant().Replace('_', '-');
						translationKey = $"{translationKey}-{containerType}-{parentPart}";
					}
					else
						translationKey = $"{translationKey}-{parentPart}";

					var translationValue = node.Value.Value.Replace("\\n", "\n    ");

					if (parentType == "LabelWithHighlight" || node.Key == "TooltipDesc")
						translationValue = translationValue.Replace("{", "<").Replace("}", ">");

					if (translationValue.All(c => c == '%' || char.IsDigit(c)))
						continue;

					if (!translationValue.Any(char.IsLetterOrDigit))
						continue;

					fluentFile.WriteLine($"{translationKey} = {translationValue}");
					node.Value.Value = translationKey;
				}

				FromChromeLayout(node.Value.Nodes, node, translatables, container, fluentFile);
			}
		}
	}
}
