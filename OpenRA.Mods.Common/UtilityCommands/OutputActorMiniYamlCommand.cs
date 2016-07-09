using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class OutputActorMiniYamlCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--actor-yaml"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2 || args.Length == 3;
		}

		[Desc("ACTOR-TYPE [PATH/TO/MAP]", "Display the finalized, merged MiniYaml tree for the given actor type. Input values are case-sensitive.")]
		void IUtilityCommand.Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var actorType = args[1];
			string mapPath = null;

			Map map = null;
			if (args.Length == 3)
				try
				{
					mapPath = args[2];
					map = new Map(modData, modData.ModFiles.OpenPackage(mapPath, new Folder(".")));
				}
				catch (InvalidDataException)
				{
					Console.WriteLine("Could not load map '{0}'.", mapPath);
					Environment.Exit(2);
				}

			var fs = map ?? modData.DefaultFileSystem;
			var topLevelNodes = MiniYaml.Load(fs, modData.Manifest.Rules, map == null ? null : map.RuleDefinitions);

			var result = topLevelNodes.FirstOrDefault(n => n.Key == actorType);
			if (result == null)
			{
				Console.WriteLine("Could not find actor '{0}' (name is case-sensitive).", actorType);
				Environment.Exit(1);
			}

			Console.WriteLine(result.Value.Nodes.WriteToString());
		}
	}
}
