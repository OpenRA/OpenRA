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
using OpenRA.FileSystem;

namespace OpenRA.Utility
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				PrintUsage(null);
				return;
			}

			AppDomain.CurrentDomain.AssemblyResolve += GlobalFileSystem.ResolveAssembly;

			Log.LogPath = Platform.ResolvePath("^", "Logs");
			Log.AddChannel("perf", null);
			Log.AddChannel("debug", null);

			var modName = args[0];
			if (!ModMetadata.AllMods.Keys.Contains(modName))
			{
				PrintUsage(null);
				return;
			}

			var modData = new ModData(modName);
			args = args.Skip(1).ToArray();
			var actions = new Dictionary<string, Action<ModData, string[]>>();
			foreach (var commandType in modData.ObjectCreator.GetTypesImplementing<IUtilityCommand>())
			{
				var command = (IUtilityCommand)Activator.CreateInstance(commandType);
				actions.Add(command.Name, command.Run);
			}

			try
			{
				var action = Exts.WithDefault((a,b) => PrintUsage(actions), () => actions[args[0]]);
				action(modData, args);
			}
			catch (Exception e)
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", args.JoinWith(" "));
				Log.Write("utility", "{0}", e);

				Console.WriteLine("Error: Utility application crashed. See utility.log for details");
				throw;
			}
		}

		static void PrintUsage(IDictionary<string, Action<ModData, string[]>> actions)
		{
			Console.WriteLine("Run `OpenRA.Utility.exe [MOD]` to see a list of available commands.");
			Console.WriteLine("The available mods are: " + string.Join(", ", ModMetadata.AllMods.Keys));
			Console.WriteLine();

			if (actions == null)
				return;
			foreach (var a in actions)
			{
				var descParts = a.Value.Method.GetCustomAttributes<DescAttribute>(true)
					.SelectMany(d => d.Lines);

				if (!descParts.Any())
					continue;

				var args = descParts.Take(descParts.Count() - 1).JoinWith(" ");
				var desc = descParts.Last();

				Console.WriteLine("  {0} {1}    ({2})", a.Key, args, desc);
			}
		}

		static string GetNamedArg(string[] args, string arg)
		{
			if (args.Length < 2)
				return null;

			var i = Array.IndexOf(args, arg);
			if (i < 0 || i == args.Length - 1)  // doesnt exist, or doesnt have a value.
				return null;

			return args[i + 1];
		}
	}
}
