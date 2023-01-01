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

namespace OpenRA.UtilityCommands
{
	class RegisterModCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--register-mod";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3 && new string[] { "system", "user", "both" }.Contains(args[2]);
		}

		[Desc("LAUNCHPATH (system|user|both)", "Generates a mod metadata entry for the in-game mod switcher.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			ModRegistration type = 0;
			if (args[2] == "system" || args[2] == "both")
				type |= ModRegistration.System;

			if (args[2] == "user" || args[2] == "both")
				type |= ModRegistration.User;

			new ExternalMods().Register(utility.ModData.Manifest, args[1], Enumerable.Empty<string>(), type);
		}
	}
}
