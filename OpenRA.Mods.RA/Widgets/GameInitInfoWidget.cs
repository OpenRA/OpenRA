#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;
using System;
using OpenRA.Mods.RA.Widgets.Delegates;
using System.IO;

namespace OpenRA.Mods.RA.Widgets
{
	class GameInitInfoWidget : Widget
	{
		public string TestFile = "";
		public string GameTitle = "";
		public string PackageURL = "";
		public string PackagePath = "";
		public string InstallMode = "";
		
		public string ResolvedPackagePath { get { return PackagePath.Replace("^", Game.SupportDir); } }

		public override void DrawInner() {}
	}
}