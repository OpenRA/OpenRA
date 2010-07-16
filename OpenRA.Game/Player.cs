#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical };

	public class Player
	{
		public Actor PlayerActor;
		public int Kills;
		public int Deaths;

		public readonly string Palette;
		public readonly Color Color;
		public readonly Color Color2;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly CountryInfo Country;
		public readonly int Index;
		public readonly bool NonCombatant = false;
		
		public ShroudRenderer Shroud;
		public World World { get; private set; }

		public Player( World world, PlayerReference pr, int index )
		{
			World = world;
			Shroud = new ShroudRenderer(this, world.Map);

			PlayerActor = world.CreateActor("Player", new int2(int.MaxValue, int.MaxValue), this);
			
			Index = index;
			Palette = "player"+index;
			Color = Util.ArrayToColor(new int[] {93,194,165});
			Color2 = Util.ArrayToColor(new int[] {0,32,32});
			
			PlayerName = InternalName = pr.Name;
			NonCombatant = pr.NonCombatant;
			Country = world.GetCountries()
				.FirstOrDefault(c => pr.Race == c.Race);
			
			RegisterPlayerColor(world, Palette);
		}
		
		public Player( World world, Session.Client client )
		{
			World = world;
			Shroud = new ShroudRenderer(this, world.Map);

			PlayerActor = world.CreateActor("Player", new int2(int.MaxValue, int.MaxValue), this);
			
			Index = client.Index;
			Palette = "player"+client.Index;
			Color = Util.ArrayToColor(new int[] {93,194,165});
			Color2 = Util.ArrayToColor(new int[] {0,32,32});
			PlayerName = client.Name;
			InternalName = "Multi{0}".F(client.Index);
			Country = world.GetCountries()
				.FirstOrDefault(c => client != null && client.Country == c.Race)
				?? world.GetCountries().Random(world.SharedRandom);
			
			RegisterPlayerColor(world, Palette);
		}
		
		public void RegisterPlayerColor(World world, string palette)
		{			
			var info = Rules.Info["world"].Traits.Get<PlayerColorPaletteInfo>();
			var newpal = new Palette(world.WorldRenderer.GetPalette(info.BasePalette),
			                 new PlayerColorRemap(Color, Color2, info.SplitRamp));
			world.WorldRenderer.AddPalette(palette, newpal);	
		}
		
		public void GiveAdvice(string advice)
		{
			// todo: store the condition or something.
			// repeat after World.Defaults.SpeakDelay, as long as the condition holds.
			Sound.PlayToPlayer(this, advice);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
	}
}
