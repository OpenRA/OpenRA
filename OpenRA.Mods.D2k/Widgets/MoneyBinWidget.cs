#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets
{
	class MoneyBinWidget : Widget
	{
		readonly World world;
		readonly PlayerResources playerResources;

		[ObjectCreator.UseCtor]
		public MoneyBinWidget(World world)
		{
			this.world = world;
			playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
		}

		public override void Draw()
		{
			if (world.LocalPlayer == null)
				return;

			if (world.LocalPlayer.WinState != WinState.Undefined)
				return;

			var digitCollection = "digits-" + world.LocalPlayer.Country.Race;
			var chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;

			var spriteMoneyBin = ChromeProvider.GetImage(chromeCollection, "moneybin");

			if (spriteMoneyBin != null)
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(spriteMoneyBin, new float2(Bounds.Left, 0));

			// Cash
			var cashDigits = (playerResources.DisplayCash + playerResources.DisplayResources).ToString();
			var x = Bounds.Right - 65;

			foreach (var d in cashDigits.Reverse())
			{
				var spriteDigit = ChromeProvider.GetImage(digitCollection, (d - '0').ToString());
				
				if (spriteDigit != null)
					Game.Renderer.RgbaSpriteRenderer.DrawSprite(spriteDigit, new float2(x, 6));
				
				x -= 14;
			}
		}
	}
}
