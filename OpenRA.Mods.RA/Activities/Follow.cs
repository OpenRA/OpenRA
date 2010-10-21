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
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Follow : CancelableActivity
	{
		Target Target;
		int Range;

		public Follow(Target target, int range)
		{
			Target = target;
			Range = range;
		}

		public override IActivity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;
			if (!Target.IsValid) return NextActivity;

			var inRange = ( Util.CellContaining( Target.CenterLocation ) - self.Location ).LengthSquared < Range * Range;

			if( inRange ) return this;

			var mobile = self.Trait<Mobile>();
			return Util.SequenceActivities( mobile.MoveTo( Target, Range ), this );
		}
	}
}
