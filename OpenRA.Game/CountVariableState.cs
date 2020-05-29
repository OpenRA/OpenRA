#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA
{
	public class CountVariableState : IntVariableStateBase, IAcceptsTokens<int>
	{
		/// <summary>Unique integers identifying granted instances of the condition.</summary>
		protected readonly HashSet<int> tokens = new HashSet<int>();

		public CountVariableState(string variable)
			: base(variable) { }

		public override int Init() { return tokens.Count; }

		public void AddOrUpdateToken(Actor self, int token, int tokenValue)
		{
			if (tokenValue != 0)
				tokens.Add(token);
			else
				tokens.Remove(token);

			// Count only non-zero tokens
			SetValue(self, tokens.Count);
		}

		public void AddOrUpdateToken(Actor self, int token)
		{
			tokens.Add(token);
			SetValue(self, tokens.Count);
		}

		public void RemoveToken(Actor self, int token)
		{
			// Tokens with zero value can be added, but are neither counted nor tracked here.
			if (tokens.Remove(token))
				SetValue(self, GetValue(self) - 1);
		}
	}
}
