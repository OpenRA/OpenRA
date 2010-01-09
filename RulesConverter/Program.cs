using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using IjwFramework.Types;
using Yaml;

namespace RulesConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			FileSystem.Mount(new Folder("./"));

			var ruleStreams = args
				.Where(a => a.EndsWith(".ini"))
				.Select(a => FileSystem.Open(a)).ToArray();

			var rules = new IniFile(ruleStreams);

			var outputFile = args.Single(a => a.EndsWith(".yaml"));

			var categoryMap = new Dictionary<string,Pair<string,string>>
			{
				{ "VehicleTypes", Pair.New( "DefaultVehicle", "Vehicle" ) },
				{ "ShipTypes", Pair.New( "DefaultShip", "Ship" ) },
				{ "PlaneTypes", Pair.New( "DefaultPlane", "Plane" ) },
				{ "DefenseTypes", Pair.New( "DefaultDefense", "Defense" ) },
				{ "BuildingTypes", Pair.New( "DefaultBuilding", "Building" ) },
				{ "InfantryTypes", Pair.New( "DefaultInfantry", "Infantry" ) },
			};

			using (var writer = File.CreateText(outputFile))
			{
				foreach (var cat in categoryMap)
					foreach (var item in rules.GetSection(cat.Key).Select(a => a.Key))
					{
						var iniSection = rules.GetSection(item);
						var yamlSection = new MappingNode(new Yaml.String(item), new Yaml.Null());
						var doc = new Yaml.Mapping(new[] { yamlSection });
						writer.Write(doc.Write());
					}
			}
		}
	}
}
