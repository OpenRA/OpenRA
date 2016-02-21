#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenRA.Utility
{
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
			if (args.Length == 0)
			{
				PrintUsage(null);
				return;
			}

			Log.AddChannel("perf", null);
			Log.AddChannel("debug", null);

			var modName = args[0];
			if (!ModMetadata.AllMods.Keys.Contains(modName))
			{
				PrintUsage(null);
				return;
			}

			Game.InitializeSettings(Arguments.Empty);
			var modData = new ModData(modName);
			args = args.Skip(1).ToArray();
			var actions = new Dictionary<string, KeyValuePair<Action<ModData, string[]>, Func<string[], bool>>>();
			foreach (var commandType in modData.ObjectCreator.GetTypesImplementing<IUtilityCommand>())
			{
				var command = (IUtilityCommand)Activator.CreateInstance(commandType);
				var kvp = new KeyValuePair<Action<ModData, string[]>, Func<string[], bool>>(command.Run, command.ValidateArguments);
				actions.Add(command.Name, kvp);
			}

			if (args.Length == 0)
			{
				PrintUsage(actions);
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
					action.Invoke(modData, args);
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

		static void PrintUsage(IDictionary<string, KeyValuePair<Action<ModData, string[]>, Func<string[], bool>>> actions)
		{
			Console.WriteLine("Run `OpenRA.Utility.exe [MOD]` to see a list of available commands.");
			Console.WriteLine("The available mods are: " + string.Join(", ", ModMetadata.AllMods.Keys));
			Console.WriteLine();

			if (actions == null)
				return;

			var keys = actions.Keys.OrderBy(x => x);

			foreach (var key in keys)
			{
				GetActionUsage(key, actions[key].Key);
			}
		}

		static void GetActionUsage(string key, Action<ModData, string[]> action)
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
