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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class TabCompletionLogic : ChromeLogic
	{
		IList<string> candidates = new List<string>();
		int currentCandidateIndex = 0;
		string lastCompleted;
		string prefix;
		string suffix;

		public IEnumerable<string> Commands { get; set; }

		public IList<string> Names { get; set; }

		public string Complete(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			if (lastCompleted == text && candidates.Count > 0)
			{
				lastCompleted = prefix + candidates[++currentCandidateIndex % candidates.Count] + suffix;
				return lastCompleted;
			}

			var toComplete = "";
			if (text.StartsWith("/") && Commands != null)
			{
				prefix = "/";
				suffix = "";
				toComplete = text.Substring(1);
				candidates = Commands.Where(x => x.StartsWith(toComplete, StringComparison.InvariantCultureIgnoreCase)).ToList();
			}
			else if (Names != null)
			{
				var oneWord = text.Contains(' ');
				if (oneWord)
				{
					prefix = text.Substring(0, text.LastIndexOf(' ') + 1);
					suffix = "";
					toComplete = text.Substring(prefix.Length);
				}
				else
				{
					prefix = "";
					suffix = ": ";
					toComplete = text;
				}

				candidates = Names.Where(x => x.StartsWith(toComplete, StringComparison.InvariantCultureIgnoreCase)).ToList();
			}
			else
				return text;

			currentCandidateIndex = 0;

			if (candidates.Count == 0)
				return text;

			lastCompleted = prefix + candidates[currentCandidateIndex] + suffix;
			return lastCompleted;
		}
	}
}
