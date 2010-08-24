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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	class MoneyBinWidget : Widget
	{
		const int chromeButtonGap = 2;
		public bool SplitOreAndCash = false;

		/* legacy crap!!! */
		List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle, Action<MouseInput>>>();
		void AddButton(Rectangle r, Action<MouseInput> b) { buttons.Add(Pair.New(r, b)); }

		public MoneyBinWidget() : base() { }
		protected MoneyBinWidget(Widget other) : base(other) { }

		public override Widget Clone() { return new MoneyBinWidget(this); }

		public override void DrawInner(World world)
		{
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

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();
				if (action == null)
					return false;

				action(mi);
				return true;
			}
			return false;
		}
	}
}
