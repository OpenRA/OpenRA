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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class PasswordFieldWidget : TextFieldWidget
	{
		public PasswordFieldWidget() { }
		protected PasswordFieldWidget(PasswordFieldWidget widget)
			: base(widget) { }

		protected override string GetApparentText() { return new string('*', Text.Length); }
		public override Widget Clone() { return new PasswordFieldWidget(this); }
	}
}
