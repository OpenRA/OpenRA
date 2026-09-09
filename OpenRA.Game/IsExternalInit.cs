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

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Lets C# 9 records and init-only setters compile on the netstandard2.1
	/// (mono) target, which lacks this marker type.
	/// </summary>
	static class IsExternalInit { }
}
#endif
