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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MockUpdateRule : UpdateRule, IBeforeUpdateActors, IBeforeUpdateWeapons, IBeforeUpdateSequences
	{
		public override string Name => "Mock Update Rule";
		public override string Description => "A mock update rule that allows to test YAML loading and can be used to correct YAML syntax.";

		public interface IBeforeUpdateActors
		{
			IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors) { yield break; }
		}

		public interface IBeforeUpdateWeapons
		{
			IEnumerable<string> BeforeUpdateWeapons(ModData modData, List<MiniYamlNodeBuilder> resolvedWeapons) { yield break; }
		}

		public interface IBeforeUpdateSequences
		{
			IEnumerable<string> BeforeUpdateSequences(ModData modData, List<MiniYamlNodeBuilder> resolvedImages) { yield break; }
		}
	}
}
