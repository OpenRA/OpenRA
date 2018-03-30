#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules
{
	public abstract class UpdateRule
	{
		public abstract string Name { get; }
		public abstract string Description { get; }

		/// <summary>Defines a transformation that is run on each top-level node in a yaml file set.</summary>
		/// <returns>An enumerable of manual steps to be run by the user</returns>
		public delegate IEnumerable<string> TopLevelNodeTransform(ModData modData, MiniYamlNode node);

		/// <summary>Defines a transformation that is run on each widget node in a chrome yaml file set.</summary>
		/// <returns>An enumerable of manual steps to be run by the user</returns>
		public delegate IEnumerable<string> ChromeNodeTransform(ModData modData, MiniYamlNode widgetNode);

		public virtual IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode) { yield break; }
		public virtual IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode) { yield break; }
		public virtual IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNode chromeNode) { yield break; }
		public virtual IEnumerable<string> UpdateTilesetNode(ModData modData, MiniYamlNode tilesetNode) { yield break; }

		public virtual IEnumerable<string> BeforeUpdate(ModData modData) { yield break; }
		public virtual IEnumerable<string> AfterUpdate(ModData modData) { yield break; }
	}
}
