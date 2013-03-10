#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public class FogInfo : TraitInfo<Fog>
	{
		/*
		 * This tag trait will enable fog of war in ShroudRenderer.
		 * Don't forget about HiddenUnderFog and FrozenUnderFog.
		 */
	}
	
	public class Fog { }
}