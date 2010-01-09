using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using IjwFramework.Types;

namespace RulesConverter
{
	using PL = Dictionary<string, string>;

	class Program
	{
		static void Main(string[] args)
		{
			FileSystem.Mount(new Folder("./"));

			var ruleStreams = args
				.Where(a => a.EndsWith(".ini"))
				.Select(a => FileSystem.Open(a)).ToArray();

			var rules = new IniFile(ruleStreams);

			var outputFile = args.Single(a => a.EndsWith(".rul"));

			var categoryMap = new Dictionary<string,Pair<string,string>>
			{
				{ "VehicleTypes", Pair.New( "DefaultVehicle", "Vehicle" ) },
				{ "ShipTypes", Pair.New( "DefaultShip", "Ship" ) },
				{ "PlaneTypes", Pair.New( "DefaultPlane", "Plane" ) },
				{ "DefenseTypes", Pair.New( "DefaultDefense", "Defense" ) },
				{ "BuildingTypes", Pair.New( "DefaultBuilding", "Building" ) },
				{ "InfantryTypes", Pair.New( "DefaultInfantry", "Infantry" ) },
			};

			var traitMap = new Dictionary<string, PL>
			{
				{ "Unit", new PL {	
					{ "HP", "Strength" }, 
					{ "Armor", "Armor" }, 
					{ "Crewed", "Crewed" } } 
				},

				{ "Selectable", new PL {
					{ "Priority", "SelectionPriority" },
					{ "Voice", "Voice" },
					{ "@Bounds", "SelectionSize" } } 
				},

				{ "Mobile", new PL {
					{ "Sight", "Sight" },
					{ "ROT", "ROT" },
					{ "Speed", "Speed" } }
					//{ "MovementType", ... },
				},

				{ "Plane", new PL {
					{ "ROT", "ROT" },
					{ "Speed", "Speed" } }
				},

				{ "Helicopter", new PL {
					{ "ROT", "ROT" },
					{ "Speed", "Speed" } }
				},

				{ "RenderBuilding", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnit", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderBuildingCharge", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderBuildingOre", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderBuildingTurreted", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderInfantry", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitMuzzleFlash", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitReload", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitRotor", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitSpinner", new PL {
					{ "Image", "Image" } }
				},

				{ "RenderUnitTurreted", new PL {
					{ "Image", "Image" } }
				},
			};

			using (var writer = File.CreateText(outputFile))
			{
				foreach (var cat in categoryMap)
					foreach (var item in rules.GetSection(cat.Key).Select(a => a.Key))
					{
						var iniSection = rules.GetSection(item);
						writer.WriteLine("{0}:", item);
						writer.WriteLine("\tInherits: {0}", cat.Value.First);

						var techLevel = iniSection.GetValue("TechLevel", "-1");
						if (techLevel != "-1")
						{
							writer.WriteLine("\tBuildable:");
							writer.WriteLine("\t\tTechLevel: {0}", techLevel);
							writer.WriteLine("\t\tDescription: \"{0}\"", iniSection.GetValue("Description", ""));
							writer.WriteLine("\t\tTab: \"{0}\"", cat.Value.Second);
							writer.WriteLine("\t\tPrerequisites: [{0}]", iniSection.GetValue("Prerequisite", ""));
							writer.WriteLine("\t\tOwner: {0}", iniSection.GetValue("Owner", ""));
							writer.WriteLine("\t\tLongDesc: \"{0}\"", iniSection.GetValue("LongDesc", ""));
							writer.WriteLine("\t\tCost: {0}", iniSection.GetValue("Cost", ""));
							if (iniSection.Contains( "Icon" ))
								writer.WriteLine("\t\tIcon: {0}", iniSection.GetValue("Icon", ""));
						}

						var traits = iniSection.GetValue("Traits", "")
							.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

						if (iniSection.GetValue("Selectable", "yes") == "yes")
							traits.Add("Selectable");

						foreach (var t in traits)
						{
							writer.WriteLine("\t{0}:", t);
							if (traitMap.ContainsKey(t))
								foreach (var kv in traitMap[t])
								{
									var v = iniSection.GetValue(kv.Value, "");
									var fmt = "\t\t{0}: {1}";
									var k = kv.Key;
									if (k.StartsWith("@")) { k = k.Substring(1); fmt = "\t\t{0}: [{1}]"; }
									if (k.StartsWith("$")) { k = k.Substring(1); fmt = "\t\t{0}: \"{1}\""; }

									if (!string.IsNullOrEmpty(v)) writer.WriteLine(fmt, k, v);
								}
						}

						writer.WriteLine();
					}
			}
		}
	}
}
