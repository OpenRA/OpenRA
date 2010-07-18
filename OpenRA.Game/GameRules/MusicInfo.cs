#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class MusicInfo
	{
		public readonly MusicPool Pool;
		public readonly string[] Music = { };

		public MusicInfo( MiniYaml y )
		{
			FieldLoader.Load(this, y);
			Pool = new MusicPool(Music);
		}
	}
	
	public class MusicPool
	{
		readonly string[] clips;
		int playing = 0;

		public MusicPool(params string[] clips)
		{
			this.clips = clips;
		}

		public string GetNext() 
		{ 
			playing = (playing + 1) % clips.Length;
			return clips[playing]; 
		}
		public string GetPrev() 
		{ 
			playing = (playing + clips.Length - 1) % clips.Length;
			return clips[playing]; 
		}
		public string GetCurrent(){ return clips[playing];}
		
	}
}
