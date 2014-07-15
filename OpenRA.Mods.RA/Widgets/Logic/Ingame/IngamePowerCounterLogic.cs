#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngamePowerCounterLogic
	{
		[ObjectCreator.UseCtor]
		public IngamePowerCounterLogic(Widget widget, OrderManager orderManager, World world)
		{
			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var power = widget.Get<LabelWidget>("POWER");

			power.GetText = () => powerManager.PowerProvided == 1000000 ? "inf" : powerManager.ExcessPower.ToString("+#;-#;0");

			// Blink red when low power
			power.GetColor = () => powerManager.ExcessPower < 0 && orderManager.LocalFrameNumber / 9 % 2 == 0 ? Color.Red : power.TextColor;
		}
	}
}
