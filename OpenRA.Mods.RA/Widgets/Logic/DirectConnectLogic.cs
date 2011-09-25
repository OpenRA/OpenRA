#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DirectConnectLogic
	{
		[ObjectCreator.UseCtor]
		public DirectConnectLogic( [ObjectCreator.Param] Widget widget )
		{
			var dc = widget.GetWidget("DIRECTCONNECT_BG");

			dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text = Game.Settings.Player.LastServer;

            dc.GetWidget<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
            {
                var address = dc.GetWidget<TextFieldWidget>("SERVER_ADDRESS").Text;
                var cpts = address.Split(':').ToArray();
                if (cpts.Length < 1 || cpts.Length > 2)
                    return;

                int port;
                if (cpts.Length != 2 || !int.TryParse(cpts[1], out port))
                    port = 1234;

                Game.Settings.Player.LastServer = address;
                Game.Settings.Save();

                Widget.CloseWindow();
                Game.JoinServer(cpts[0], port);
            };

			dc.GetWidget<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Widget.CloseWindow();
				Widget.OpenWindow("MAINMENU_BG");
			};
		}
	}
}

