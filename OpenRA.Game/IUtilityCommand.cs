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

using OpenRA.Traits;

namespace OpenRA
{
	public class Utility
	{
		public readonly ModData ModData;
		public readonly InstalledMods Mods;

		public Utility(ModData modData, InstalledMods mods)
		{
			ModData = modData;
			Mods = mods;
		}
	}

	[RequireExplicitImplementation]
	public interface IUtilityCommand
	{
		/// <summary>
		/// The string used to invoke the command.
		/// </summary>
		string Name { get; }

		bool ValidateArguments(string[] args);

		void Run(Utility utility, string[] args);
	}
}
