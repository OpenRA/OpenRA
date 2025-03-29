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
using Microsoft.CodeAnalysis;

namespace OpenRA.Roslyn.SourceGenerators
{
	static class SourceGeneratorHelpers
	{
		public static string GetClassModifiers(INamedTypeSymbol classSymbol)
		{
			var modifiers = new List<string>
			{
				classSymbol.DeclaredAccessibility.ToString().ToLower()
			};

			if (classSymbol.IsAbstract)
				modifiers.Add("abstract");

			if (classSymbol.IsSealed)
				modifiers.Add("sealed");

			if (classSymbol.IsStatic)
				modifiers.Add("static");

			modifiers.Add("partial");

			return string.Join(" ", modifiers);
		}
	}
}
