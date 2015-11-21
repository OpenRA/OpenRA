#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
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

				var faction = self.Owner.Faction.InternalName;

				if (!string.IsNullOrEmpty(voiced.Faction))
					faction = voiced.Faction.ToLower();

				voiced.PlayVoice(self, phrase, faction);
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

				var faction = self.Owner.Faction.InternalName;

				if (!string.IsNullOrEmpty(voiced.Faction))
					faction = voiced.Faction.ToLower();

					voiced.PlayVoiceLocal(self, phrase, faction, volume);
			}
		}

		public static bool HasVoice(this Actor self, string voice)
		{
			return self.TraitsImplementing<IVoiced>().Any(x => x.HasVoice(self, voice));
		}

		public static void PlayVoiceForOrders(this World w, Order[] orders)
		{
			// Find an actor with a phrase to say
			foreach (var o in orders)
			{
				if (o == null)
					continue;

				var orderSubject = o.Subject;
				if (orderSubject.Disposed)
					continue;

				foreach (var voice in orderSubject.TraitsImplementing<IVoiced>())
					foreach (var v in orderSubject.TraitsImplementing<IOrderVoice>())
					{
						var faction = orderSubject.Owner.Faction.InternalName;

						if (!string.IsNullOrEmpty(voice.Faction))
							faction = voice.Faction.ToLower();

						if (voice.PlayVoice(orderSubject, v.VoicePhraseForOrder(orderSubject, o), faction))
							return;
					}
			}
		}
	}
}
