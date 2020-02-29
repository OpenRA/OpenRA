#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA
{
	public enum TextNotificationPool { System, Chat, Mission, Feedback }

	public class TextNotification : IEquatable<TextNotification>
	{
		public readonly TextNotificationPool Pool;
		public readonly string Prefix;
		public readonly string Text;
		public readonly Color PrefixColor;
		public readonly Color TextColor;

		public TextNotification(TextNotificationPool pool, string prefix, string text, Color prefixColor, Color textColor)
		{
			Pool = pool;
			Prefix = prefix;
			Text = text;
			PrefixColor = prefixColor;
			TextColor = textColor;
		}

		public bool CanIncrementOnDuplicate()
		{
			return Pool == TextNotificationPool.Feedback || Pool == TextNotificationPool.System;
		}

		public bool Equals(TextNotification other)
		{
			return other != null && other.GetHashCode() == GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is TextNotification && Equals((TextNotification)obj);
		}

		public override int GetHashCode()
		{
			return string.Format("{0}{1}{2}", Prefix, Text, Pool).GetHashCode();
		}
	}
}
