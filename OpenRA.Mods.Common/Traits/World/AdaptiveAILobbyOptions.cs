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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Lobby options for Adaptive AI bots.")]
	public sealed class AdaptiveAILobbyOptionsInfo : TraitInfo, ILobbyOptions
	{
		public override object Create(ActorInitializer init) { return new AdaptiveAILobbyOptions(this); }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return Option(map, "adaptive-difficulty", 50,
				"dropdown-adaptive-difficulty.label", "dropdown-adaptive-difficulty.description",
				new Dictionary<string, string>
				{
					{ "easy", "options-adaptive-difficulty.easy" },
					{ "medium", "options-adaptive-difficulty.medium" },
					{ "hard", "options-adaptive-difficulty.hard" },
					{ "brutal", "options-adaptive-difficulty.brutal" },
				}, "medium");

			yield return Option(map, "adaptive-scouting", 51,
				"dropdown-adaptive-scouting.label", "dropdown-adaptive-scouting.description",
				new Dictionary<string, string>
				{
					{ "off", "options-adaptive-scouting.off" },
					{ "minimal", "options-adaptive-scouting.minimal" },
					{ "normal", "options-adaptive-scouting.normal" },
					{ "aggressive", "options-adaptive-scouting.aggressive" },
				}, "normal");

			yield return Option(map, "adaptive-counterbuild", 52,
				"dropdown-adaptive-counterbuild.label", "dropdown-adaptive-counterbuild.description",
				new Dictionary<string, string>
				{
					{ "off", "options-adaptive-counterbuild.off" },
					{ "balanced", "options-adaptive-counterbuild.balanced" },
					{ "aggressive", "options-adaptive-counterbuild.aggressive" },
				}, "balanced");

			yield return Option(map, "adaptive-expansion", 53,
				"dropdown-adaptive-expansion.label", "dropdown-adaptive-expansion.description",
				new Dictionary<string, string>
				{
					{ "defensive", "options-adaptive-expansion.defensive" },
					{ "balanced", "options-adaptive-expansion.balanced" },
					{ "greedy", "options-adaptive-expansion.greedy" },
				}, "balanced");

			yield return Option(map, "adaptive-aggression", 54,
				"dropdown-adaptive-aggression.label", "dropdown-adaptive-aggression.description",
				new Dictionary<string, string>
				{
					{ "passive", "options-adaptive-aggression.passive" },
					{ "balanced", "options-adaptive-aggression.balanced" },
					{ "rush", "options-adaptive-aggression.rush" },
				}, "balanced");

			yield return Option(map, "adaptive-supportpowers", 55,
				"dropdown-adaptive-supportpowers.label", "dropdown-adaptive-supportpowers.description",
				new Dictionary<string, string>
				{
					{ "never", "options-adaptive-supportpowers.never" },
					{ "conservative", "options-adaptive-supportpowers.conservative" },
					{ "normal", "options-adaptive-supportpowers.normal" },
					{ "aggressive", "options-adaptive-supportpowers.aggressive" },
				}, "normal");
		}

		static LobbyOption Option(MapPreview map, string id, int displayOrder, string label, string description,
			Dictionary<string, string> values, string defaultValue)
		{
			return new LobbyOption(map, id, label, description, true, displayOrder, values, defaultValue, false);
		}
	}

	public sealed class AdaptiveAILobbyOptions
	{
		public AdaptiveAILobbyOptions(AdaptiveAILobbyOptionsInfo info) { }
	}
}
