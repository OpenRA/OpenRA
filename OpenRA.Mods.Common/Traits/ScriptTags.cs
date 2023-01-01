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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows this actor to be 'tagged' with arbitrary strings. Tags must be unique or they will be rejected.")]
	public class ScriptTagsInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ScriptTags(init, this); }
	}

	public class ScriptTags
	{
		readonly HashSet<string> tags = new HashSet<string>();

		public ScriptTags(ActorInitializer init, ScriptTagsInfo info)
		{
			var scriptTagsInit = init.GetOrDefault<ScriptTagsInit>(info);
			if (scriptTagsInit != null)
				foreach (var tag in scriptTagsInit.Value)
					tags.Add(tag);
		}

		public bool AddTag(string tag)
		{
			return tags.Add(tag);
		}

		public bool RemoveTag(string tag)
		{
			return tags.Remove(tag);
		}

		public bool HasTag(string tag)
		{
			return tags.Contains(tag);
		}
	}

	/// <summary>Allows mappers to 'tag' actors with arbitrary strings that may have meaning in their scripts.</summary>
	public class ScriptTagsInit : ValueActorInit<string[]>
	{
		public ScriptTagsInit(string[] value)
			: base(value) { }
	}
}
