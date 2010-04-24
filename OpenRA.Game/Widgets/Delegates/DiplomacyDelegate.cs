using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Widgets.Delegates
{
	public class DiplomacyDelegate : IWidgetDelegate
	{
		static List<Widget> controls = new List<Widget>();

		public DiplomacyDelegate()
		{
			var diplomacyBG = Chrome.rootWidget.GetWidget("DIPLOMACY_BG");

			Chrome.rootWidget.GetWidget("INGAME_DIPLOMACY_BUTTON").OnMouseUp = mi =>
			{
				diplomacyBG.Visible = !diplomacyBG.Visible;
				if (diplomacyBG.Visible)
					LayoutDialog(diplomacyBG);
				return true;
			};
		}

		void LayoutDialog(Widget bg)
		{
			bg.Children.RemoveAll(w => controls.Contains(w));
			controls.Clear();

			var unwantedPlayers = new[] { Game.world.NeutralPlayer, Game.world.LocalPlayer };
			var y = 50;
			var margin = 20;
			var labelWidth = (bg.Bounds.Width - 3 * margin) / 3;

			var ts = new LabelWidget
			{
				Bold = true,
				Bounds = new Rectangle(margin + labelWidth + 10, y, labelWidth, 25),
				Text = "Their Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ts);
			controls.Add(ts);

			var ms = new LabelWidget
			{
				Bold = true,
				Bounds = new Rectangle(margin + 2 * labelWidth + 20, y, labelWidth, 25),
				Text = "My Stance",
				Align = LabelWidget.TextAlign.Left,
			};

			bg.AddChild(ms);
			controls.Add(ms);

			y += 35;

			foreach (var p in Game.world.players.Values.Except(unwantedPlayers))
			{
				var pp = p;
				var label = new LabelWidget
				{
					Bounds = new Rectangle(margin, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,
					Bold = true,
				};

				bg.AddChild(label);
				controls.Add(label);

				var theirStance = new LabelWidget
				{
					Bounds = new Rectangle( margin + labelWidth + 10, y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_THEIR_{0}".F(p.Index),
					Text = p.PlayerName,
					Align = LabelWidget.TextAlign.Left,
					Bold = false,

					GetText = () => pp.Stances[ Game.world.LocalPlayer ].ToString(),
				};

				bg.AddChild(theirStance);
				controls.Add(theirStance);

				var myStance = new ButtonWidget
				{
					Bounds = new Rectangle( margin + 2 * labelWidth + 20,  y, labelWidth, 25),
					Id = "DIPLOMACY_PLAYER_LABEL_MY_{0}".F(p.Index),
					Text = Game.world.LocalPlayer.Stances[ pp ].ToString(),
				};

				myStance.OnMouseUp = mi => { CycleStance(pp, myStance); return true; };

				bg.AddChild(myStance);
				controls.Add(myStance);
				
				y += 35;
			}
		}

		Stance GetNextStance(Stance s)
		{
			switch (s)
			{
				case Stance.Ally: return Stance.Enemy;
				case Stance.Enemy: return Stance.Neutral;
				case Stance.Neutral: return Stance.Ally;
				default:
					throw new ArgumentException();
			}
		}

		void CycleStance(Player p, ButtonWidget bw)
		{
			var nextStance = GetNextStance((Stance)Enum.Parse(typeof(Stance), bw.Text));

			Game.IssueOrder(new Order("SetStance", Game.world.LocalPlayer.PlayerActor,
				new int2(p.Index, (int)nextStance)));

			bw.Text = nextStance.ToString();
		}
	}
}
