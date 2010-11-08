#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Server.Traits
{	
	// Returns true if order is handled 
	public interface IInterpretCommand { bool InterpretCommand(Connection conn, string cmd); }

	public class DebugServerTrait : IInterpretCommand
	{		
		public bool InterpretCommand(Connection conn, string cmd)
		{
			Game.Debug("Server received command from player {1}: {0}".F(cmd, conn.PlayerIndex));
			return false;
		}
	}
}
