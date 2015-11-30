#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class TabCompletionLogic : ChromeLogic
	{
		IList<string> candidates = new List<string>();
		int currentCandidateIndex = 0;
		string lastCompleted;
		public readonly string PrefixOnStart;
		public readonly string SuffixOnStart;
		string prefix;
		string suffix;

		int longestElementLength;
		IList<string> completions;
		public IList<string> Completions
		{
			get
			{
				return completions;
			}

			set
			{
				completions = value;
				longestElementLength = completions.Max(element => element.Length);
			}
		}

		public TabCompletionLogic(string prefixOnStart = "", string suffixOnStart = ": ")
		{
			this.PrefixOnStart = prefixOnStart;
			this.SuffixOnStart = suffixOnStart;
		}

		public string Complete(string text)
		{
			if (string.IsNullOrEmpty(text) || completions == null)
				return text;

			// Check if tab is cycled without text modification
			if (lastCompleted == text)
			{
				if (!candidates.Any())
					return text;

				lastCompleted = prefix + candidates[++currentCandidateIndex % candidates.Count] + suffix;
				return lastCompleted;
			}

			GenerateCompletionCandidates(text);
			if (candidates.Count == 0)
				lastCompleted = text;
			else
				lastCompleted = prefix + candidates[currentCandidateIndex] + suffix;  // Alternatively, we could fill the shortest common substring of candidates
			return lastCompleted;
		}

		void GenerateCompletionCandidates(string text)
		{
			suffix = "";
			candidates = new List<string>();
			var relevantSubstrings = Enumerable.Range(1, Math.Min(text.Length, longestElementLength)).Select(i => text.Substring(text.Length - i));

			prefix = "";
			var possibleCandidates = new List<Tuple<string, int>>();
			foreach (string x in completions)
			{
				var firstValid = relevantSubstrings.Take(x.Length).Reverse().FirstOrDefault(e => x.StartsWith(e, StringComparison.InvariantCultureIgnoreCase));

				if (!string.IsNullOrEmpty(firstValid))
				{
					possibleCandidates.Add(Tuple.Create(x, firstValid.Length));
				}
			}

			if (possibleCandidates.Any())
			{
				var maxMatch = possibleCandidates.Max(e => e.Item2);  // Longest common substring length
				candidates = possibleCandidates.Where(e => e.Item2 == maxMatch).Select(e => e.Item1).ToList();
				prefix = text.Substring(0, text.Length - maxMatch);
			}

			// Add prefix/suffix if first word
			if (prefix == "")
			{
				prefix = PrefixOnStart;
				suffix = SuffixOnStart;
			}

			currentCandidateIndex = 0;
		}
	}
}
