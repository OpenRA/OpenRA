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
	class ClearInvalidModRegistrationsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--clear-invalid-mod-registrations";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2 && new string[] { "system", "user", "both" }.Contains(args[1]);
		}

		[Desc("(system|user|both)", "Removes invalid metadata entries for the in-game mod switcher.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			ModRegistration type = 0;
			if (args[1] == "system" || args[1] == "both")
				type |= ModRegistration.System;

			if (args[1] == "user" || args[1] == "both")
				type |= ModRegistration.User;

			new ExternalMods().ClearInvalidRegistrations(type);
		}
	}
}
