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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA
{
	public static class VoiceExts
	{
		public static void PlayVoice(this Actor self, string phrase)
		{
			if (phrase == null)
				return;

			foreach (var voiced in self.TraitsImplementing<IVoiced>())
			{
				if (string.IsNullOrEmpty(voiced.VoiceSet))
					return;

				voiced.PlayVoice(self, phrase, self.Owner.Faction.InternalName);
			}
		}

		public static void PlayVoiceLocal(this Actor self, string phrase, float volume)
		{
			if (phrase == null)
				return;

			foreach (var voiced in self.TraitsImplementing<IVoiced>())
			{
				if (string.IsNullOrEmpty(voiced.VoiceSet))
					return;

				voiced.PlayVoiceLocal(self, phrase, self.Owner.Faction.InternalName, volume);
			}
		}

		public static bool HasVoice(this Actor self, string voice)
		{
			return self.TraitsImplementing<IVoiced>().Any(x => x.HasVoice(self, voice));
		}

		public static void PlayVoiceForOrders(this Order[] orders)
		{
			// Find the first actor with a phrase to say
			foreach (var o in orders)
			{
				if (o == null)
					continue;

				if (o.GroupedActors != null)
				{
					foreach (var subject in o.GroupedActors)
						if (PlayVoiceForOrder(Order.FromGroupedOrder(o, subject)))
							return;
				}
				else if (PlayVoiceForOrder(o))
					return;
			}
		}

		static bool PlayVoiceForOrder(Order o)
		{
			var orderSubject = o.Subject;
			if (orderSubject == null || orderSubject.Disposed)
				return false;

			foreach (var voice in orderSubject.TraitsImplementing<IVoiced>())
			{
				foreach (var v in orderSubject.TraitsImplementing<IOrderVoice>())
				{
					if (voice.PlayVoice(orderSubject, v.VoicePhraseForOrder(orderSubject, o),
						orderSubject.Owner.Faction.InternalName))
						return true;
				}
			}

			return false;
		}
	}
}
