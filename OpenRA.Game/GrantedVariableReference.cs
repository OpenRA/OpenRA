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

using System;
using System.Collections.Generic;

namespace OpenRA
{
	public interface IWithGrantedVariables { IEnumerable<KeyValuePair<string, Type>> GetGrantedVariables(); }

	public interface IWithGrantedVariables<TokenType> : IWithGrantedVariables { }

	public struct GrantedVariableReference<TokenType> : IWithGrantedVariables<TokenType>
	{
		public readonly string Name;

		public GrantedVariableReference(string name = null)
		{
			Name = name;
		}

		public GrantedVariableReference(GrantedVariableReference<TokenType> reference)
		{
			Name = reference.Name;
		}

		public bool Valid { get { return !string.IsNullOrEmpty(Name); } }

		public override string ToString() { return Name; }

		public static bool operator ==(GrantedVariableReference<TokenType> me, GrantedVariableReference<TokenType> other) { return me.Name == other.Name; }
		public static bool operator !=(GrantedVariableReference<TokenType> me, GrantedVariableReference<TokenType> other) { return !(me == other); }
		public static bool operator ==(GrantedVariableReference<TokenType> me, string other) { return me.Name == other; }
		public static bool operator !=(GrantedVariableReference<TokenType> me, string other) { return me.Name != other; }

		public override bool Equals(object obj)
		{
			return Name.Equals(obj.ToString());
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		IEnumerable<KeyValuePair<string, Type>> IWithGrantedVariables.GetGrantedVariables()
		{
			if (!string.IsNullOrEmpty(Name))
				yield return new KeyValuePair<string, Type>(Name, typeof(TokenType));
		}
	}

	public static class WithGrantedVariablesExts
	{
		public static IEnumerable<KeyValuePair<string, Type>> GetGrantedVariables(this IEnumerable<IWithGrantedVariables> variableCollections)
		{
			foreach (var variables in variableCollections)
				foreach (var variable in variables.GetGrantedVariables())
					if (!string.IsNullOrEmpty(variable.Key))
						yield return variable;
		}
	}
}
