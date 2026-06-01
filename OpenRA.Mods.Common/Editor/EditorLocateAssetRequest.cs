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
	public enum EditorLocateAssetKind { Tile, Actor, Resource }

	public readonly struct EditorLocateAssetRequest
	{
		public readonly EditorLocateAssetKind Kind;
		public readonly ushort? TemplateId;
		public readonly ActorInfo Actor;
		public readonly string ResourceType;

		public static EditorLocateAssetRequest ForTile(ushort templateId) =>
			new(EditorLocateAssetKind.Tile, templateId, null, null);

		public static EditorLocateAssetRequest ForActor(ActorInfo actor) =>
			new(EditorLocateAssetKind.Actor, null, actor, null);

		public static EditorLocateAssetRequest ForResource(string resourceType) =>
			new(EditorLocateAssetKind.Resource, null, null, resourceType);

		EditorLocateAssetRequest(EditorLocateAssetKind kind, ushort? templateId, ActorInfo actor, string resourceType)
		{
			Kind = kind;
			TemplateId = templateId;
			Actor = actor;
			ResourceType = resourceType;
		}
	}
}
