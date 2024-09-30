#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Widgets
{
	public abstract class InputWidget : Widget
	{
		public bool Disabled = false;
		public Func<bool> IsDisabled = () => false;

		protected InputWidget()
		{
			IsDisabled = () => Disabled;
		}

		protected InputWidget(InputWidget other)
			: base(other)
		{
			IsDisabled = () => other.Disabled;
		}
	}
}
