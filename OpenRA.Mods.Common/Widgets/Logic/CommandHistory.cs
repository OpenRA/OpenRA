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

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public sealed class CommandHistory
	{
		const int MaxHistorySize = 50;
		static CommandHistory instance;
		static readonly object LockObject = new();
		readonly List<string> history = [];
		int currentIndex = -1;

		CommandHistory() { }

		public static CommandHistory Instance
		{
			get
			{
				if (instance == null)
				{
					lock (LockObject)
					{
						instance ??= new CommandHistory();
					}
				}

				return instance;
			}
		}

		public void AddCommand(string command)
		{
			if (string.IsNullOrWhiteSpace(command) || !command.StartsWith('/'))
				return;

			var trimmedCommand = command.Trim();
			history.RemoveAll(c => c == trimmedCommand);
			history.Insert(0, trimmedCommand);
			if (history.Count > MaxHistorySize)
				history.RemoveAt(history.Count - 1);

			currentIndex = -1;
		}

		public string GetPrevious()
		{
			if (history.Count == 0)
				return null;

			if (currentIndex < history.Count - 1)
				currentIndex++;

			return history[currentIndex];
		}

		public string GetNext()
		{
			if (history.Count == 0)
				return null;

			if (currentIndex > 0)
			{
				currentIndex--;
				return history[currentIndex];
			}

			if (currentIndex == 0)
				currentIndex = -1;

			return null;
		}

		public void Reset()
		{
			currentIndex = -1;
		}
	}
}
