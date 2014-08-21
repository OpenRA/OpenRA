#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Widgets
{
	public class PasswordFieldWidget : TextFieldWidget
	{
		public PasswordFieldWidget() { }
		protected PasswordFieldWidget(PasswordFieldWidget widget) : base(widget) { }

		protected override string GetApparentText() { return new string('*', Text.Length); }
		public override Widget Clone() { return new PasswordFieldWidget(this); }
	}
}