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
using OpenRA.Primitives;

namespace OpenRA
{
	public enum TextNotificationPool { System, Chat, Mission, Feedback, Transients }

	public class TextNotification : IEquatable<TextNotification>
	{
		public readonly TextNotificationPool Pool;
		public readonly string Prefix;
		public readonly int ClientId;
		public readonly string Text;
		public readonly Color? PrefixColor;
		public readonly Color? TextColor;
		public readonly DateTime Time;

		public TextNotification(TextNotificationPool pool, int clientId, string prefix, string text, Color? prefixColor, Color? textColor)
		{
			Pool = pool;
			ClientId = clientId;
			Prefix = prefix;
			Text = text;
			PrefixColor = prefixColor;
			TextColor = textColor;
			Time = DateTime.Now;
		}

		public bool CanIncrementOnDuplicate()
		{
			return Pool == TextNotificationPool.Feedback || Pool == TextNotificationPool.System || Pool == TextNotificationPool.Transients;
		}

		public static bool operator ==(TextNotification me, TextNotification other) { return me.GetHashCode() == other.GetHashCode(); }

		public static bool operator !=(TextNotification me, TextNotification other) { return !(me == other); }

		public bool Equals(TextNotification other) { return other == this; }

		public override bool Equals(object obj) { return obj is TextNotification notification && Equals(notification); }

		public override int GetHashCode()
		{
			return HashCode.Combine(Prefix, Text, (int)Pool);
		}
	}
}
