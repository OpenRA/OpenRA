#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	class MoneyBinWidget : Widget
	{
		public bool SplitOreAndCash = false;

		public MoneyBinWidget() : base() { }

		public override void DrawInner(World world)
		{
			if( world.LocalPlayer == null ) return;

			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();

			var digitCollection = "digits-" + world.LocalPlayer.Country.Race;
			var chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(
				ChromeProvider.GetImage(chromeCollection, "moneybin"),
				new float2(Bounds.Left, 0));

			// Cash
			var cashDigits = (SplitOreAndCash ? playerResources.DisplayCash
				: (playerResources.DisplayCash + playerResources.DisplayOre)).ToString();
			var x = Bounds.Right - 65;

			foreach (var d in cashDigits.Reverse())
			{
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(
					ChromeProvider.GetImage(digitCollection, (d - '0').ToString()),
					new float2(x, 6));
				x -= 14;
			}

			if (SplitOreAndCash)
			{
				x -= 14;
				// Ore
				var oreDigits = playerResources.DisplayOre.ToString();

				foreach (var d in oreDigits.Reverse())
				{
					Game.Renderer.RgbaSpriteRenderer.DrawSprite(
						ChromeProvider.GetImage( digitCollection, (d - '0').ToString()),
						new float2(x, 6));
					x -= 14;
				}
			}
		}
	}
}
