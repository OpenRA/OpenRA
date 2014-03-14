#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Lint
{
	static class YamlChecker
	{
		static int errors = 0;

		static void EmitError(string e)
		{
			Console.WriteLine("OpenRA.Lint(1,1): Error: {0}", e);
			++errors;
		}

		static void EmitWarning(string e)
		{
			Console.WriteLine("OpenRA.Lint(1,1): Warning: {0}", e);
		}

		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: OpenRA.Lint.exe MOD [MAP] [--verbose]");
				return 0;
			}

			try
			{
				var options = args.Where(a => a.StartsWith("-"));
				var mod = args.Where(a => !options.Contains(a)).First();
				var map = args.Where(a => !options.Contains(a)).Skip(1).FirstOrDefault();
				var verbose = options.Contains("-v") || options.Contains("--verbose");

				// bind some nonfatal error handling into FieldLoader, so we don't just *explode*.
				ObjectCreator.MissingTypeAction = s => EmitError("Missing Type: {0}".F(s));
				FieldLoader.UnknownFieldAction = (s, f) => EmitError("FieldLoader: Missing field `{0}` on `{1}`".F(s, f.Name));

				AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;
				Game.modData = new ModData(mod);

				var maps = new Map[] { new Map() };
				if (!string.IsNullOrEmpty(map))
					maps = new Map[] { new Map(map) };
				else
				{
					Game.modData.LoadMaps();
					maps = Game.modData.AvailableMaps.Values.ToArray();
				}

				foreach (var testMap in maps)
				{
					if (testMap.Rules.Count < 1)
					{
						if (verbose)
							Console.WriteLine("No custom rules detected. Omitting Map: {0}".F(testMap.Title));

						continue;
					}

					if (verbose)
						Console.WriteLine("Map: {0}".F(testMap.Title));
					Rules.LoadRules(Game.modData.Manifest, testMap);

					foreach (var customPassType in Game.modData.ObjectCreator
						.GetTypesImplementing<ILintPass>())
					{
						try
						{
							var customPass = (ILintPass)Game.modData.ObjectCreator
								.CreateBasic(customPassType);
	
							if (verbose)
								Console.WriteLine("Pass: {0}".F(customPassType.ToString()));
	
							customPass.Run(EmitError, EmitWarning, testMap);
						}
						catch (Exception e)
						{
							EmitError("Failed with exception: {0}".F(e));
						}
					}
				}

				if (errors > 0)
				{
					Console.WriteLine("Errors: {0}", errors);
					return 1;
				}

				return 0;
			}
			catch (Exception e)
			{
				EmitError("Failed with exception: {0}".F(e));
				return 1;
			}
		}
	}
}
