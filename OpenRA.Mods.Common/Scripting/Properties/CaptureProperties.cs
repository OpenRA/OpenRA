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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class CaptureProperties : ScriptActorProperties, Requires<CaptureManagerInfo>
	{
		readonly CaptureManager captureManager;

		public CaptureProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			captureManager = Self.Trait<CaptureManager>();
		}

		[Desc("Captures the target actor.")]
		public void Capture(Actor target)
		{
			if (!CanCapture(target))
				return;

			// NB: Scripted actions get no visible targetlines.
			Self.QueueActivity(new CaptureActor(Self, Target.FromActor(target), null));
		}

		[Desc("Checks if the target actor can be catured.")]
		public bool CanCapture(Actor target)
		{
			var targetManager = target.TraitOrDefault<CaptureManager>();
			return targetManager != null && targetManager.CanBeTargetedBy(target, Self, captureManager);
		}
	}
}
