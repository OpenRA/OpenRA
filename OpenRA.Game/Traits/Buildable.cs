#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Traits
{
	public class ValuedInfo : ITraitInfo
	{
		public readonly int Cost = 0;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string[] Owner = { };

		public virtual object Create(ActorInitializer init) { return new Valued(); }
	}

	public class BuildableInfo : ValuedInfo
	{
		[ActorReference]public readonly string[] Prerequisites = { };
		[ActorReference] public readonly string[] BuiltAt = { };
		
		public readonly string Icon = null;
		public readonly string[] AlternateName = { };
		public readonly int BuildPaletteOrder = 9999;
        public readonly string Hotkey = null;

		public override object Create(ActorInitializer init) { return new Buildable(); }
	}

	class Valued { }		/* halfway to buildable */
	class Buildable { }
}
