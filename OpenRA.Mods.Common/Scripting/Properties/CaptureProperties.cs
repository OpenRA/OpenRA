#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Ability")]
	public class CaptureProperties : ScriptActorProperties
	{
		readonly CapturesInfo normalInfo;
		readonly ExternalCapturesInfo externalInfo;

		public CaptureProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			normalInfo = Self.Info.TraitInfoOrDefault<CapturesInfo>();
			externalInfo = Self.Info.TraitInfoOrDefault<ExternalCapturesInfo>();
		}

		[Desc("Captures the target actor.")]
		public void Capture(Actor target)
		{
			var normalCapturable = target.Info.TraitInfoOrDefault<CapturableInfo>();
			var externalCapturable = target.Info.TraitInfoOrDefault<ExternalCapturableInfo>();

			if (normalInfo != null && normalCapturable != null && normalInfo.CaptureTypes.Contains(normalCapturable.Type))
				Self.QueueActivity(new CaptureActor(Self, target));
			else if (externalInfo != null && externalCapturable != null && externalInfo.CaptureTypes.Contains(externalCapturable.Type))
				Self.QueueActivity(new ExternalCaptureActor(Self, Target.FromActor(target)));
			else
				throw new LuaException("Actor '{0}' cannot capture actor '{1}'!".F(Self, target));
		}
	}
}
