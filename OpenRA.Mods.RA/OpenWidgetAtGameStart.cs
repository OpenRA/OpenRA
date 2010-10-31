#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class OpenWidgetAtGameStartInfo : ITraitInfo
	{
		public readonly string Widget = "INGAME_ROOT";
		public readonly string ObserverWidget = "";

		public object Create(ActorInitializer init) { return new OpenWidgetAtGameStart(this); }
	}

	public class OpenWidgetAtGameStart: IWorldLoaded
	{
		readonly OpenWidgetAtGameStartInfo Info;
		public OpenWidgetAtGameStart(OpenWidgetAtGameStartInfo Info)
		{
			this.Info = Info;
		}
		
		public void WorldLoaded(World world)
		{
			if (world.LocalPlayer != null)
				world.OpenWindow(Info.Widget);
			else if (Info.ObserverWidget != null)
				world.OpenWindow(Info.ObserverWidget);
		}
	}
}