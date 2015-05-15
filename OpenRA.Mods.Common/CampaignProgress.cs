using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common
{
	public class CampaignProgress
	{
		private static string progressFile = Platform.ResolvePath("^", "cnc-progress.yaml");
		private static bool saveProgressFlag = false;
        private static string playedMission = "";

		public static void Init()
		{
			ModMetadata initialMod = null;
			ModMetadata.AllMods.TryGetValue(Game.Settings.Game.Mod, out initialMod);
			var mod = initialMod.Id;
			progressFile = Platform.ResolvePath("^", mod + "-progress.yaml");
		}

        public static void SetPlayedMission(string mission)
        {
            playedMission = mission;
        }

		public static void SetSaveProgressFlag()
		{
			saveProgressFlag = true;
		}

		public static void ResetSaveProgressFlag()
		{
			saveProgressFlag = false;
		}

		public static bool GetSaveProgressFlag()
		{
			return saveProgressFlag;
		}

        public static void SaveProgress(string faction)
        {
            SaveProgress(faction, playedMission);
        }

		public static void SaveProgress(string faction, string mission)
		{
			if (saveProgressFlag)
			{
				if (!GlobalFileSystem.Exists(progressFile))
					CreateProgressFile();
				var yaml = MiniYaml.FromFile(progressFile);
				foreach (var kv in yaml)
				{
					if (kv.Key.Equals(faction))
					{
						foreach (var node in kv.Value.Nodes)
						{
							if (node.Key.Equals("Mission"))
								node.Value.Value = mission.ToString();
						}
					}
				}

				yaml.WriteToFile(progressFile);
			}
		}

		public static string GetGdiProgress()
		{
			return GetMission("GDI");
		}

        public static string GetNodProgress()
		{
			return GetMission("Nod");
		}

        private static string GetMission(string faction)
		{
			if (!GlobalFileSystem.Exists(progressFile))
				CreateProgressFile();
			var yaml = MiniYaml.FromFile(progressFile);
			var mission = "";
			foreach (var kv in yaml)
			{
				if (kv.Key.Equals(faction))
				{
					foreach (var node in kv.Value.Nodes)
					{
						if (node.Key.Equals("Mission"))
							mission = node.Value.Value;
					}
				}
			}

			return (mission == null) ? "" : mission;
		}

		private static void CreateProgressFile()
		{
			var yaml = new List<MiniYamlNode>();
			var gdiNodes = new List<MiniYamlNode>();
			var nodNodes = new List<MiniYamlNode>();

			var gdiMission = new MiniYamlNode("Mission", "");
			gdiNodes.Add(gdiMission);
			var nodMission = new MiniYamlNode("Mission", "");
			nodNodes.Add(nodMission);

			var gdi = new MiniYaml(null, gdiNodes);
			var gdiNode = new MiniYamlNode("GDI", gdi);
			var nod = new MiniYaml(null, nodNodes);
			var nodNode = new MiniYamlNode("Nod", nod);

			yaml.Add(gdiNode);
			yaml.Add(nodNode);

			yaml.WriteToFile(progressFile);
		}
	}
}