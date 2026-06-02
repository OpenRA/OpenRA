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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public enum EditorLocateAssetKind { Tile, Actor, Resource, RestoreAllCategories }

	public readonly struct EditorLocateAssetRequest
	{
		public readonly EditorLocateAssetKind Kind;
		public readonly ushort? TemplateId;
		public readonly ActorInfo Actor;
		public readonly string ResourceType;
		public readonly bool ScrollToAsset;

		public static EditorLocateAssetRequest ForTile(ushort templateId, bool scrollToAsset = true) =>
			new(EditorLocateAssetKind.Tile, templateId, null, null, scrollToAsset);

		public static EditorLocateAssetRequest ForActor(ActorInfo actor, bool scrollToAsset = true) =>
			new(EditorLocateAssetKind.Actor, null, actor, null, scrollToAsset);

		public static EditorLocateAssetRequest ForResource(string resourceType) =>
			new(EditorLocateAssetKind.Resource, null, null, resourceType, true);

		public static EditorLocateAssetRequest RestoreAllCategories() =>
			new(EditorLocateAssetKind.RestoreAllCategories, null, null, null, false);

		EditorLocateAssetRequest(EditorLocateAssetKind kind, ushort? templateId, ActorInfo actor, string resourceType, bool scrollToAsset)
		{
			Kind = kind;
			TemplateId = templateId;
			Actor = actor;
			ResourceType = resourceType;
			ScrollToAsset = scrollToAsset;
		}
	}
}
