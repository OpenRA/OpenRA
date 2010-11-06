#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	class ActorTemplate
	{
		public Bitmap Bitmap;
		public ActorInfo Info;
		public EditorAppearanceInfo Appearance;
	}

	class BrushTemplate
	{
		public Bitmap Bitmap;
		public ushort N;
	}

	class ResourceTemplate
	{
		public Bitmap Bitmap;
		public ResourceTypeInfo Info;
		public int Value;
	}

	class WaypointTemplate
	{
	}
}
