#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical };
	public enum WinState { Won, Lost, Undefined }; 
	
	public class Player
	{
		public Actor PlayerActor;
		public int Kills;
		public int Deaths;
		public WinState WinState = WinState.Undefined;

		public readonly string Palette;
		public readonly Color Color;
		public readonly Color Color2;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly CountryInfo Country;
		public readonly int Index;
		public readonly bool NonCombatant = false;
		public readonly int ClientIndex;
		public readonly PlayerReference PlayerRef;
		
		public ShroudRenderer Shroud;
		public World World { get; private set; }

		public Player( World world, PlayerReference pr, int index )
		{
			World = world;
			Shroud = new ShroudRenderer(this, world.Map);

			PlayerActor = world.CreateActor("Player", new TypeDictionary{ new OwnerInit( this ) });
			
			Index = index;
			Palette = "player"+index;
			Color = pr.Color;
			Color2 = pr.Color2;
			ClientIndex = 0;		/* it's a map player, "owned" by host */
			
			PlayerName = InternalName = pr.Name;
			NonCombatant = pr.NonCombatant;
			Country = world.GetCountries()
				.FirstOrDefault(c => pr.Race == c.Race)
				?? world.GetCountries().Random(world.SharedRandom);

			PlayerRef = pr;
			
			RegisterPlayerColor(world, Palette);
		}
		
		public Player( World world, Session.Client client, PlayerReference pr, int index )
		{
			World = world;
			Shroud = new ShroudRenderer(this, world.Map);

			PlayerActor = world.CreateActor("Player", new TypeDictionary{ new OwnerInit( this ) });
			
			Index = index;
			Palette = "player"+index;
			Color = client.Color1;
			Color2 = client.Color2;
			PlayerName = client.Name;
			InternalName = pr.Name;
			Country = world.GetCountries()
				.FirstOrDefault(c => client != null && client.Country == c.Race)
				?? world.GetCountries().Random(world.SharedRandom);

			ClientIndex = client.Index;
			PlayerRef = pr;
			
			RegisterPlayerColor(world, Palette);
		}
		
		public void RegisterPlayerColor(World world, string palette)
		{			
			var info = Rules.Info["world"].Traits.Get<PlayerColorPaletteInfo>();
			var newpal = new Palette(world.WorldRenderer.GetPalette(info.BasePalette),
			                 new PlayerColorRemap(Color, Color2, info.PaletteFormat));
			world.WorldRenderer.AddPalette(palette, newpal);
		}
		
		public void GiveAdvice(string advice)
		{
			Sound.PlayToPlayer(this, advice);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
	}
}
