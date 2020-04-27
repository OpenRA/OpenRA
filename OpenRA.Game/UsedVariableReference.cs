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
	public interface IWithUsedVariables { IEnumerable<string> GetUsedVariables(); }

	public struct UsedVariableReference : IWithUsedVariables
	{
		public readonly string Name;

		public UsedVariableReference(string name)
		{
			Name = name;
		}

		public UsedVariableReference(UsedVariableReference reference)
		{
			Name = reference.Name;
		}

		public bool Valid { get { return string.IsNullOrEmpty(Name); } }

		IEnumerable<string> IWithUsedVariables.GetUsedVariables()
		{
			if (!string.IsNullOrEmpty(Name))
				yield return Name;
		}

		public override string ToString() { return Name; }

		public static bool operator ==(UsedVariableReference me, UsedVariableReference other) { return me.Name == other.Name; }
		public static bool operator !=(UsedVariableReference me, UsedVariableReference other) { return !(me == other); }
		public static bool operator ==(UsedVariableReference me, string other) { return me.Name == other; }
		public static bool operator !=(UsedVariableReference me, string other) { return me.Name != other; }

		public override bool Equals(object obj)
		{
			return Name.Equals(obj.ToString());
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}

	public static class WithUsedVariablesExts
	{
		public static IEnumerable<string> GetUsedVariables(this IEnumerable<IWithUsedVariables> variableCollections)
		{
			foreach (var variables in variableCollections)
				foreach (var variable in variables.GetUsedVariables())
					if (!string.IsNullOrEmpty(variable))
						yield return variable;
		}
	}
}
