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
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	public static class RandomNameGenerator
	{
		const string PseudonymListsFile = "pseudonym-lists.yaml";

		static string[] adjectives;
		static string[] corps;
		static string[] roles;
		static bool isInitialized;
		static readonly object InitLock = new();

		public static void Initialize(IReadOnlyFileSystem fileSystem)
		{
			lock (InitLock)
			{
				if (isInitialized)
					return;

				var adjectivesList = new List<string>();
				var corpsList = new List<string>();
				var rolesList = new List<string>();

				try
				{
					if (!fileSystem.Exists(PseudonymListsFile))
					{
						Log.Write("debug", $"RandomNameGenerator: {PseudonymListsFile} not found, using fallback lists");
						InitializeFallbackLists();
						isInitialized = true;
						return;
					}

					var yaml = MiniYaml.FromStream(fileSystem.Open(PseudonymListsFile), PseudonymListsFile);

					foreach (var node in yaml)
					{
						if (node.Key == "Adjectives" && node.Value.Nodes != null)
						{
							foreach (var child in node.Value.Nodes)
								if (!string.IsNullOrWhiteSpace(child.Key))
									adjectivesList.Add(child.Key.Trim());
						}
						else if (node.Key == "Corps" && node.Value.Nodes != null)
						{
							foreach (var child in node.Value.Nodes)
								if (!string.IsNullOrWhiteSpace(child.Key))
									corpsList.Add(child.Key.Trim());
						}
						else if (node.Key == "Roles" && node.Value.Nodes != null)
						{
							foreach (var child in node.Value.Nodes)
								if (!string.IsNullOrWhiteSpace(child.Key))
									rolesList.Add(child.Key.Trim());
						}
					}

					if (adjectivesList.Count == 0 || corpsList.Count == 0 || rolesList.Count == 0)
					{
						Log.Write("debug", "RandomNameGenerator: Empty lists in YAML, using fallback lists");
						InitializeFallbackLists();
					}
					else
					{
						adjectives = adjectivesList.ToArray();
						corps = corpsList.ToArray();
						roles = rolesList.ToArray();
						Log.Write("debug", $"RandomNameGenerator: Loaded {adjectives.Length} adjectives, {corps.Length} corps, and {roles.Length} roles");
					}
				}
				catch (Exception ex)
				{
					Log.Write("debug", $"RandomNameGenerator: Error loading {PseudonymListsFile}: {ex.Message}");
					InitializeFallbackLists();
				}

				isInitialized = true;
			}
		}

		static void InitializeFallbackLists()
		{
			adjectives =
			[
				"Bold", "Brave", "Calm", "Clever", "Daring", "Eager", "Fierce", "Gallant",
				"Iron", "Keen", "Loyal", "Mighty", "Proud", "Rugged", "Silent", "Swift",
				"Tough", "Valiant", "Wary", "Zealous", "Dark", "Fast", "Grim", "Lone",
				"Mad", "Old", "Wild", "Cold", "Sharp", "Steady", "Grumpy", "Lazy"
			];

			corps =
			[
				"Airborne", "Armored", "Infantry", "Artillery", "Assault", "Cavalry", "Marine",
				"Ranger", "Commando", "Recon", "Special", "Guard", "Signal", "Medical",
				"Logistics", "Transport", "Supply", "Cyber", "Covert", "Stealth", "Strike",
				"Garrison", "Training", "Reserve", "Defense", "Security", "Patrol", "Radar",
				"Tech", "Drone", "Naval", "Fleet"
			];

			roles =
			[
				"Ace", "Agent", "Cadet", "Chief", "Cook", "Driver", "Guard", "Gunner",
				"Medic", "Pilot", "Ranger", "Rookie", "Scout", "Sniper", "Trooper", "Veteran",
				"Spy", "Soldier", "Fighter", "Sapper", "Captain", "Sergeant", "Corporal", "Marshal",
				"Operator", "Tanker", "Crewman", "Loader", "Mechanic", "Engineer", "Analyst", "Hacker"
			];
		}

		static string GenerateRandomName(MersenneTwister random = null)
		{
			if (!isInitialized)
			{
				Log.Write("debug", "RandomNameGenerator: Not initialized, using fallback");
				InitializeFallbackLists();
				isInitialized = true;
			}

			random ??= new MersenneTwister();

			var firstWordPool = adjectives.Concat(corps).ToArray();
			var shortestFirstWordLength = firstWordPool.Min(w => w.Length);
			var role = roles[random.Next(roles.Length)];
			var maxFirstWordLength = Settings.MaxPlayerNameLength - 1 - role.Length;

			if (maxFirstWordLength < shortestFirstWordLength)
				return role;

			var validFirstWords = firstWordPool.Where(w => w.Length <= maxFirstWordLength).ToArray();

			var firstWord = validFirstWords[random.Next(validFirstWords.Length)];
			return $"{firstWord} {role}";
		}

		public static string GenerateRandomName(ModData modData)
		{
			Initialize(modData.DefaultFileSystem);
			return GenerateRandomName(new MersenneTwister());
		}
	}
}
