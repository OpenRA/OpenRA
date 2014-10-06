#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Transform")]
	public class TransformProperties : ScriptActorProperties, Requires<TransformsInfo>
	{
		readonly Transforms transforms;

		public TransformProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			transforms = self.Trait<Transforms>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue a new transformation.")]
		public void Deploy()
		{
			transforms.DeployTransform(true);
		}
	}
}