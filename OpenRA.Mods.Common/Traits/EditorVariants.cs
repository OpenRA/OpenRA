#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("In the map editor the variants can be scrolled through after an actor has been placed.")]
	public class EditorVariantsInfo : TraitInfo<EditorVariants>, IEditorActorOptions
	{
		[FieldLoader.Require]
		[Desc("Variant actors that can be cycled through. For buildings these should have the same footprint to get optimal results.")]
		public readonly string[] Actors = null;

		[Desc("Display order for the building variant slider in the map editor UI.")]
		public readonly int EditorVariantDisplayOrder = 5;

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorSlider("Variant", EditorVariantDisplayOrder, 0, Actors.Length, 1,
				actor =>
				{
					var variants = Actors.Prepend(ai.Name);
					return variants.ToList().IndexOf(actor.Type);
				},
				(actor, value) =>
				{
					var variants = Actors.Prepend(ai.Name).ToArray();
					actor.ReplaceActor(variants[(int)value]);
				});
		}

		public override object Create(ActorInitializer init) { return new EditorVariants(); }
	}

	public class EditorVariants { }
}
