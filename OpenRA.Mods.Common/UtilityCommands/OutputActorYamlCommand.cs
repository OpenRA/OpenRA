using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class DisplayMiniYamlTreeActor : IUtilityCommand
	{
		static readonly List<MiniYamlNode> NoMapRules = new List<MiniYamlNode>();

		string IUtilityCommand.Name { get { return "--actor-yaml"; } }

		[Desc("ACTOR [PATH/TO/MAP.oramap]", "Display the merged miniyaml tree for the given actor type.")]
		void IUtilityCommand.Run(ModData modData, string[] args)
		{
			Game.ModData = modData;
			var actorName = args[1];

			var additionalNodes = args.Length == 3 ? new Map(args[2]).RuleDefinitions : NoMapRules;
			var tree = LoadYamlRules(modData.Manifest.Rules, additionalNodes);

			MiniYaml actorMiniYaml;
			if (!tree.TryGetValue(actorName, out actorMiniYaml))
			{
				Console.WriteLine("Could not find actor '{0}'.", actorName);
				Environment.Exit(1);
			}

			Console.WriteLine(actorName + ":");
			RecursivePrintMiniYamlNodes(actorMiniYaml.Nodes, 1);
		}

		void RecursivePrintMiniYamlNodes(List<MiniYamlNode> nodes, int indent)
		{
			foreach (var n in nodes)
			{
				Console.WriteLine(new string('\t', indent) + "{0}: {1}", n.Key, n.Value.Value);
				RecursivePrintMiniYamlNodes(n.Value.Nodes, indent + 1);
			}
		}

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2 || args.Length == 3;
		}

		Dictionary<string, MiniYaml> LoadYamlRules(string[] files, List<MiniYamlNode> additionalNodes)
		{
			return MiniYaml.Merge(files.Select(s => MiniYaml.FromStream(Game.ModData.ModFiles.Open(s))).Append(additionalNodes))
				.ToDictionaryWithConflictLog(n => n.Key, n => n.Value, "LoadYamlRules", null, null);
		}
	}
}
