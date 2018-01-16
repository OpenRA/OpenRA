#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenRA
{
	using UtilityActions = Dictionary<string, KeyValuePair<Action<Utility, string[]>, Func<string[], bool>>>;

	[Serializable]
	public class NoSuchCommandException : Exception
	{
		public readonly string Command;
		public NoSuchCommandException(string command)
			: base("No such command '{0}'".F(command))
		{
			Command = command;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Command", Command);
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Log.AddChannel("perf", null);
			Log.AddChannel("debug", null);

			Game.InitializeSettings(Arguments.Empty);

			var envModSearchPaths = Environment.GetEnvironmentVariable("MOD_SEARCH_PATHS");
			var modSearchPaths = !string.IsNullOrWhiteSpace(envModSearchPaths) ?
				FieldLoader.GetValue<string[]>("MOD_SEARCH_PATHS", envModSearchPaths) :
				new[] { Path.Combine(".", "mods") };

			if (args.Length == 0)
			{
				PrintUsage(new InstalledMods(modSearchPaths, new string[0]), null);
				return;
			}

			var modId = args[0];
			var explicitModPaths = new string[0];
			if (File.Exists(modId) || Directory.Exists(modId))
			{
				explicitModPaths = new[] { modId };
				modId = Path.GetFileNameWithoutExtension(modId);
			}

			var mods = new InstalledMods(modSearchPaths, explicitModPaths);
			if (!mods.Keys.Contains(modId))
			{
				PrintUsage(mods, null);
				return;
			}

			var modData = new ModData(mods[modId], mods);
			var utility = new Utility(modData, mods);
			args = args.Skip(1).ToArray();
			var actions = new UtilityActions();
			foreach (var commandType in modData.ObjectCreator.GetTypesImplementing<IUtilityCommand>())
			{
				var command = (IUtilityCommand)Activator.CreateInstance(commandType);
				var kvp = new KeyValuePair<Action<Utility, string[]>, Func<string[], bool>>(command.Run, command.ValidateArguments);
				actions.Add(command.Name, kvp);
			}

			if (args.Length == 0)
			{
				PrintUsage(mods, actions);
				return;
			}

			try
			{
				var command = args[0];
				if (!actions.ContainsKey(command))
					throw new NoSuchCommandException(command);

				var action = actions[command].Key;
				var validateActionArgs = actions[command].Value;

				if (validateActionArgs.Invoke(args))
				{
					action.Invoke(utility, args);
				}
				else
				{
					Console.WriteLine("Invalid arguments for '{0}'", command);
					GetActionUsage(command, action);
				}
			}
			catch (Exception e)
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", args.JoinWith(" "));
				Log.Write("utility", "{0}", e);

				if (e is NoSuchCommandException)
					Console.WriteLine(e.Message);
				else
				{
					Console.WriteLine("Error: Utility application crashed. See utility.log for details");
					throw;
				}
			}
		}

		static void PrintUsage(InstalledMods mods, UtilityActions actions)
		{
			Console.WriteLine("Run `OpenRA.Utility.exe [MOD]` to see a list of available commands.");
			Console.WriteLine("The available mods are: " + string.Join(", ", mods.Keys));
			Console.WriteLine();

			if (actions == null)
				return;

			var keys = actions.Keys.OrderBy(x => x);

			foreach (var key in keys)
			{
				GetActionUsage(key, actions[key].Key);
			}
		}

		static void GetActionUsage(string key, Action<Utility, string[]> action)
		{
			var descParts = action.Method.GetCustomAttributes<DescAttribute>(true)
					.SelectMany(d => d.Lines).ToArray();

			if (descParts.Length == 0)
				return;

			var args = descParts.Take(descParts.Length - 1).JoinWith(" ");
			var desc = descParts[descParts.Length - 1];

			Console.WriteLine("  {0} {1}{3}  {2}{3}", key, args, desc, Environment.NewLine);
		}
	}
}
