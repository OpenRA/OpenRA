#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits.Activities
{
	public abstract class CancelableActivity : IActivity
	{
		protected IActivity NextActivity { get; private set; }
		protected bool IsCanceled { get; private set; }

		public abstract IActivity Tick( Actor self );
		protected virtual bool OnCancel( Actor self ) { return true; }

		public void Cancel( Actor self )
		{
			IsCanceled = OnCancel( self );
			if( IsCanceled )
				NextActivity = null;
			else if (NextActivity != null)
				NextActivity.Cancel( self );
		}

		public void Queue( IActivity activity )
		{
			if( NextActivity != null )
				NextActivity.Queue( activity );
			else
				NextActivity = activity;
		}

		public virtual IEnumerable<float2> GetCurrentPath()
		{
			yield break;
		}
	}
}
