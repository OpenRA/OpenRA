#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Irc
{
	public enum NumericCommand
	{
		Undefined = 0,
		RPL_WELCOME = 001,
		RPL_NOTOPIC = 331,
		RPL_TOPIC = 332,
		RPL_TOPICWHOTIME = 333,
		RPL_NAMREPLY = 353,
		RPL_ENDOFNAMES = 366,
		ERR_ERRONEUSNICKNAME = 432,
		ERR_NICKNAMEINUSE = 433
	}
}
