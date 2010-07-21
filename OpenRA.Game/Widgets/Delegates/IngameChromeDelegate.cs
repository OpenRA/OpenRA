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
using System.Linq;
namespace OpenRA.Widgets.Delegates
{
	public class IngameChromeDelegate : IWidgetDelegate
	{
		public IngameChromeDelegate()
		{
			var r = Widget.RootWidget;
			var gameRoot = r.GetWidget("INGAME_ROOT");
			var optionsBG = gameRoot.GetWidget("INGAME_OPTIONS_BG");
			
			Game.OnGameStart += () => r.OpenWindow("INGAME_ROOT");

			r.GetWidget("INGAME_OPTIONS_BUTTON").OnMouseUp = mi => {
				optionsBG.Visible = !optionsBG.Visible;
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_DISCONNECT").OnMouseUp = mi => {
				optionsBG.Visible = false;
				Game.Disconnect();
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_SETTINGS").OnMouseUp = mi => {
				r.OpenWindow("SETTINGS_MENU");
				return true;
			};

			optionsBG.GetWidget("BUTTON_RESUME").OnMouseUp = mi =>
			{
				optionsBG.Visible = false;
				return true;
			};

			optionsBG.GetWidget("BUTTON_SURRENDER").OnMouseUp = mi =>
			{
				Game.IssueOrder(new Order("Surrender", Game.world.LocalPlayer.PlayerActor));
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_QUIT").OnMouseUp = mi => {
				Game.Exit();
				return true;
			};
			
			Game.AddChatLine += gameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine;
			
			
			var postgameBG = gameRoot.GetWidget("POSTGAME_BG");
			var postgameText = postgameBG.GetWidget<LabelWidget>("TEXT");
			postgameBG.IsVisible = () =>
			{
				var conds = Game.world.Queries.WithTrait<IVictoryConditions>()
						.Where(c => !c.Actor.Owner.NonCombatant);
				
				if (Game.world.LocalPlayer != null && conds.Count() > 1)
				{
					if (conds.Any(c => c.Actor.Owner == Game.world.LocalPlayer && c.Trait.HasLost)
						|| conds.All(c => AreMutualAllies(c.Actor.Owner, Game.world.LocalPlayer) || c.Trait.HasLost))	
						return true;
				}
				
				return false;
			};
			
			postgameText.GetText = () =>
			{
				var lost = Game.world.Queries.WithTrait<IVictoryConditions>()
						.Where(c => c.Actor.Owner == Game.world.LocalPlayer)
						.Any(c => c.Trait.HasLost);
				
				return (lost) ? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS";
			};
		}
		bool AreMutualAllies(Player a, Player b) { return a.Stances[b] == Stance.Ally && b.Stances[a] == Stance.Ally; }

	}
}
