#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion
using System;

namespace OpenRA.Launcher
{
	public class Mod
	{
		public string Title { get; private set; }
		public string Version { get; private set; }
		public string Author { get; private set; }
		public string Description { get; private set; }
		public string Requires { get; private set; }
		public bool Standalone { get; private set; }

		public Mod(string title, string version, string author, string description, string requires, bool standalone)
		{
			Title = title;
			Version = version;
			Author = author;
			Description = description;
			Requires = requires;
			Standalone = standalone;
		}
	}
}
