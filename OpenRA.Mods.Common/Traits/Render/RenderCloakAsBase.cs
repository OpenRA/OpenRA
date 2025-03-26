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
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Traits.Render
{
	public abstract class RenderCloakAsBaseInfo : ConditionalTraitInfo
	{
		[Desc("Cloak types that should be rendered as invisible. If empty, all cloak types are rendered as invisible.")]
		public readonly HashSet<string> CloakTypes = new();
	}

	public abstract class RenderCloakAsBase<InfoType> : ConditionalTrait<InfoType>, IRenderCloaked where InfoType : RenderCloakAsBaseInfo
	{
		protected RenderCloakAsBase(InfoType info)
			: base(info) { }

		bool IRenderCloaked.IsValidCloakType(string cloakType)
		{
			return Info.CloakTypes.Count == 0 || Info.CloakTypes.Contains(cloakType);
		}

		protected abstract IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r);

		IEnumerable<IRenderable> IRenderCloaked.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;
			return ModifyRender(self, wr, r);
		}

		protected virtual void OnCloaked(Actor self, CloakInfo cloakInfo, bool isInitial) { }

		void IRenderCloaked.OnCloaked(Actor self, CloakInfo cloakInfo, bool isInitial)
		{
			OnCloaked(self, cloakInfo, isInitial);
		}

		protected virtual void OnUncloaked(Actor self, CloakInfo cloakInfo, bool isInitial) { }

		void IRenderCloaked.OnUncloaked(Actor self, CloakInfo cloakInfo, bool isInitial)
		{
			OnUncloaked(self, cloakInfo, isInitial);
		}
	}
}
