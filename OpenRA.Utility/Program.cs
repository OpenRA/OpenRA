#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

namespace OpenRA
{
	using UtilityActions = Dictionary<string, KeyValuePair<Action<Utility, string[]>, Func<string[], bool>>>;

	public class NoSuchCommandException : Exception
	{
		public readonly string Command;
		public NoSuchCommandException(string command)
			: base($"No such command '{command}'")
		{
			Command = command;
		}
	}

	sealed class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Run(args);
			}
			catch
			{
				// Flush logs before rethrowing, i.e. allowing the exception to go unhandled.
				// try-finally won't work - an unhandled exception kills our process without running the finally block!
				Log.Dispose();
				throw;
			}
			finally
			{
				Log.Dispose();
			}
		}

		static void Run(string[] args)
		{
			var engineDir = Environment.GetEnvironmentVariable("ENGINE_DIR");
			if (!string.IsNullOrEmpty(engineDir))
				Platform.OverrideEngineDir(engineDir);

			Log.AddChannel("perf", null);
			Log.AddChannel("debug", null);

			Game.InitializeSettings(Arguments.Empty);

			var envModSearchPaths = Environment.GetEnvironmentVariable("MOD_SEARCH_PATHS");
			var modSearchPaths = !string.IsNullOrWhiteSpace(envModSearchPaths) ?
				FieldLoader.GetValue<string[]>("MOD_SEARCH_PATHS", envModSearchPaths) :
				[Path.Combine(Platform.EngineDir, "mods")];

			if (args.Length == 0)
			{
				PrintUsage(new InstalledMods(modSearchPaths, []), null);
				return;
			}

			var modId = args[0];
			var explicitModPaths = Array.Empty<string>();
			if (File.Exists(modId) || Directory.Exists(modId))
			{
				explicitModPaths = [modId];
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
				if (!actions.TryGetValue(command, out var kvp))
					throw new NoSuchCommandException(command);

				var action = kvp.Key;
				var validateActionArgs = kvp.Value;

				if (validateActionArgs.Invoke(args))
				{
					action.Invoke(utility, args);
				}
				else
				{
					Console.WriteLine($"Invalid arguments for '{command}'");
					GetActionUsage(command, action);
					Environment.Exit(1);
				}
			}
			catch (Exception e)
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", $"Received args: {args.JoinWith(" ")}");
				Log.Write("utility", e);

				if (e is NoSuchCommandException)
				{
					Console.WriteLine(e.Message);
					Log.Dispose(); // Flush logs before we terminate the process.
					Environment.Exit(1);
				}
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

			var keys = actions.Keys.Order();

			foreach (var key in keys)
			{
				GetActionUsage(key, actions[key].Key);
			}
		}

		static void GetActionUsage(string key, Action<Utility, string[]> action)
		{
			var descParts = Utility.GetCustomAttributes<DescAttribute>(action.Method, true)
					.SelectMany(d => d.Lines).ToArray();

			if (descParts.Length == 0)
				return;

			var args = descParts.Take(descParts.Length - 1).JoinWith(" ");
			var desc = descParts[^1];

			Console.WriteLine($"  {key} {args}{Environment.NewLine}  {desc}{Environment.NewLine}");
		}
	}
}
