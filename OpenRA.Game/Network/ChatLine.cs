#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	public enum ChatPool { System, Chat, Mission, Feedback, Transcriptions }
	public class ChatLine : IEquatable<ChatLine>
	{
		public readonly Color NameColor;
		public readonly string Name;
		public readonly string Text;
		public readonly Color TextColor;
		public readonly ChatPool Pool;

		public ChatLine(string name, Color nameColor, string text, Color textColor, ChatPool chatPool)
		{
			NameColor = nameColor;
			Name = name;
			Text = text;
			TextColor = textColor;
			Pool = chatPool;
		}

		public bool IsRepeatable()
		{
			return Pool == ChatPool.Transcriptions || Pool == ChatPool.Feedback || Pool == ChatPool.System;
		}

		public bool Equals(ChatLine other)
		{
			return other != null && other.GetHashCode() == GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is ChatLine && Equals((ChatLine)obj);
		}

		public override int GetHashCode()
		{
			return string.Format("{0}{1}{2}", Name, Text, Pool).GetHashCode();
		}
	}
}
