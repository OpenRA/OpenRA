#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractLanguageStringsCommand : IUtilityCommand
	{
		public string Name { get { return "--extract-language-strings"; } }

		[Desc("Extract translatable strings that are not yet localized and update chrome layout.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.modData = modData;
			Game.modData.RulesetCache.LoadDefaultRules();

			var types = Game.modData.ObjectCreator.GetTypes();
			var translatableFields = types.SelectMany(t => t.GetFields())
				.Where(f => f.HasAttribute<TranslateAttribute>()).Distinct();

			foreach (var filename in Game.modData.Manifest.ChromeLayout)
			{
				Console.WriteLine("# {0}:", filename);
				var yaml = MiniYaml.FromFile(filename);
				ExtractLanguageStringsCommand.FromChromeLayout(ref yaml, null,
					translatableFields.Select(t => t.Name).Distinct(), null);
				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			// TODO: Properties can also be translated.
		}

		internal static void FromChromeLayout(ref List<MiniYamlNode> nodes, MiniYamlNode parent, IEnumerable<string> translatables, string container)
		{
			var parentNode = parent != null ? parent.Key.Split('@') : null;
			var parentType = parent != null ? parentNode.First() : null;
			var parentLabel = parent != null ? parentNode.Last() : null;

			if ((parentType == "Background" || parentType == "Container") && parentLabel.IsUppercase())
				container = parentLabel;

			foreach (var node in nodes)
			{
				var alreadyTranslated = node.Value.Value != null && node.Value.Value.Contains('@');
				if (translatables.Contains(node.Key) && !alreadyTranslated)
				{
					var translationKey = "{0}-{1}-{2}".F(container.Replace('_', '-'), parentLabel.Replace('_', '-'), node.Key.ToUpper());
					Console.WriteLine("\t{0}: {1}", translationKey, node.Value.Value);
					node.Value.Value = "@{0}@".F(translationKey);
				}

				FromChromeLayout(ref node.Value.Nodes, node, translatables, container);
			}
		}
	}
}
