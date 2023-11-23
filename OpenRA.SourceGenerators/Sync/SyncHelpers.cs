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

using System.Linq;
using Microsoft.CodeAnalysis;

namespace OpenRA.SourceGenerators.Sync
{
	static class SyncHelpers
	{
		public const string GenerateSyncCodeAttributeName = "GenerateSyncCodeAttribute";
		public const string SyncMemberAttributeName = "SyncMemberAttribute";
		public const string SyncInterfaceName = "OpenRA.ISync";
		public const string SyncHashMethodName = "GetSyncHash";

		public static bool ImplementsISync(this INamedTypeSymbol type) => type.AllInterfaces.Any(y => y.ToDisplayString() == SyncInterfaceName);

		public static bool ManuallyImplementsISync(this INamedTypeSymbol type)
			=> type.ImplementsISync() && type.MemberNames.Any(x => x == SyncHashMethodName);

		public static bool HasOrInheritsGenerateSyncCodeAttribute(this INamedTypeSymbol symbol)
		{
			var symbolType = symbol;
			while (symbolType != null)
			{
				foreach (var attribute in symbolType.GetAttributes())
					if (attribute.AttributeClass?.Name == GenerateSyncCodeAttributeName)
						return true;

				symbolType = symbolType.BaseType;
			}

			return false;
		}

		public static bool HasSyncMemberAttribute(this ISymbol symbol) => symbol.GetAttributes().Any(x => x.AttributeClass.Name == SyncMemberAttributeName);
	}
}
