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

			var outputFile = args.Single(a => !a.EndsWith(".ini"));

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
					{ "Crewed", "Crewed" },
					{ "InitialFacing", "InitialFacing" },
					{ "Sight", "Sight" },
					{ "WaterBound", "WaterBound" } }
				},

				{ "Selectable", new PL {
					{ "Priority", "SelectionPriority" },
					{ "Voice", "Voice" },
					{ "@Bounds", "SelectionSize" } } 
				},

				{ "Mobile", new PL {
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

				{ "RenderUnitSpinner", new PL {
					{ "Image", "Image" },
					{ "Offset", "PrimaryOffset" } }
				},

				{ "RenderUnitRotor", new PL {
					{ "Image", "Image" },
					{ "PrimaryOffset", "RotorOffset" },
					{ "SecondaryOffset", "RotorOffset2" } }
				},

				{ "Buildable", new PL {
					{ "TechLevel", "TechLevel" },
					{ "Tab", "$Tab" },
					{ "@Prerequisites", "Prerequisite" },
					{ "BuiltAt", "BuiltAt" },
					{ "Owner", "Owner" },
					{ "Cost", "Cost" },
					{ "Icon", "Icon" },
					{ "$Description", "Description" },
					{ "$LongDesc", "LongDesc" } }
				},

				{ "Cargo", new PL { 
					{ "@PassengerTypes", "PassengerTypes" },
					{ "Passengers", "Passengers" },
					{ "UnloadFacing", "UnloadFacing" } }
				},

				{ "LimitedAmmo", new PL {
					{ "Ammo", "Ammo" } }
				},

				{ "Building", new PL {
					{ "Power", "Power" },
					{ "RequiresPower", "Powered" },
					{ "Footprint", "Footprint" },
					{ "@Dimensions", "Dimensions" },
					{ "Capturable", "Capturable" },
					{ "Repairable",  "Repairable" }, 
					{ "BaseNormal", "BaseNormal" },
					{ "Adjacent", "Adjacent" },
					{ "Bib", "Bib" },
					{ "HP", "Strength" }, 
					{ "Armor", "Armor" }, 
					{ "Crewed", "Crewed" },
					{ "WaterBound", "WaterBound" },
					{ "InitialFacing", "InitialFacing" },
					{ "Sight", "Sight" },
					{ "Unsellable", "Unsellable" } }
				},

				{ "StoresOre", new PL {
					{ "Pips", "OrePips" },
					{ "Capacity", "Storage" } }
				},

				{ "Harvester", new PL {
					{ "Pips", "OrePips" } }
					//{ "Capacity"
				},

				{ "AttackBase", new PL {
					{ "PrimaryWeapon", "Primary" },
					{ "SecondaryWeapon", "SecondaryWeapon" },
					{ "PrimaryOffset", "PrimaryOffset" },
					{ "SecondaryOffset", "SecondaryOffset" },
					{ "PrimaryLocalOffset", "PrimaryLocalOffset" },
					{ "SecondaryLocalOffset", "SecondaryLocalOffset" },
					{ "MuzzleFlash", "MuzzleFlash" },		// maybe
					{ "Recoil", "Recoil"},
					{ "FireDelay", "FireDelay" } }
				},

				{ "Production", new PL {
					{ "SpawnOffset", "SpawnOffset" },
					{ "Produces", "Produces" } }
				},

				{ "Minelayer", new PL {
					{ "Mine", "Primary" } }
				},
			};

			traitMap["RenderUnit"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingCharge"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingOre"] = traitMap["RenderBuilding"];
			traitMap["RenderBuildingTurreted"] = traitMap["RenderBuilding"];
			traitMap["RenderInfantry"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitMuzzleFlash"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitReload"] = traitMap["RenderBuilding"];
			traitMap["RenderUnitTurreted"] = traitMap["RenderBuilding"];

			traitMap["AttackTurreted"] = traitMap["AttackBase"];
			traitMap["AttackPlane"] = traitMap["AttackBase"];
			traitMap["AttackHeli"] = traitMap["AttackBase"];

			using (var writer = File.CreateText(outputFile))
			{
				foreach (var cat in categoryMap)
					try
					{
						foreach (var item in rules.GetSection(cat.Key).Select(a => a.Key))
						{
							var iniSection = rules.GetSection(item);
							writer.WriteLine("{0}:", item);
							writer.WriteLine("\tInherits: {0}", cat.Value.First);

							var traits = iniSection.GetValue("Traits", "")
								.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

							if (iniSection.GetValue("Selectable", "yes") == "yes")
								traits.Insert(0, "Selectable");

							if (iniSection.GetValue("TechLevel", "-1") != "-1")
								traits.Insert(0, "Buildable");

							foreach (var t in traits)
							{
								writer.WriteLine("\t{0}:", t);

								if (traitMap.ContainsKey(t))
									foreach (var kv in traitMap[t])
									{
										var v = kv.Value == "$Tab" ? cat.Value.Second : iniSection.GetValue(kv.Value, "");
										var fmt = "\t\t{0}: {1}";
										var k = kv.Key;
										if (k.StartsWith("@")) { k = k.Substring(1); /*fmt = "\t\t{0}: [{1}]";*/ }
										if (k.StartsWith("$")) { k = k.Substring(1); fmt = "\t\t{0}: \"{1}\""; }

										if (!string.IsNullOrEmpty(v)) writer.WriteLine(fmt, k, v);
									}
							}

							writer.WriteLine();
						}
					}
					catch { }
			}

			var yaml = MiniYaml.FromFile( outputFile );
			yaml.OptimizeInherits( MiniYaml.FromFile( "defaults.yaml" ) );
			yaml.WriteToFile( outputFile );
		}
	}
}
