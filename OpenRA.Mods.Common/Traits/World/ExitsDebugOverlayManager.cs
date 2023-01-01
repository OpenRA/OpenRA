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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	public class ExitsDebugOverlayManagerInfo : TraitInfo
	{
		[Desc("The font used to draw cell vectors. Should match the value as-is in the Fonts section of the mod manifest (do not convert to lowercase).")]
		public readonly string Font = "TinyBold";

		public override object Create(ActorInitializer init) { return new ExitsDebugOverlayManager(init.Self, this); }
	}

	public class ExitsDebugOverlayManager : IWorldLoaded, IChatCommand
	{
		const string CommandName = "exits-overlay";

		[TranslationReference]
		const string CommandDescription = "description-exits-overlay";

		public readonly SpriteFont Font;
		public readonly ExitsDebugOverlayManagerInfo Info;

		public bool Enabled;

		readonly Actor self;

		public ExitsDebugOverlayManager(Actor self, ExitsDebugOverlayManagerInfo info)
		{
			this.self = self;
			Info = info;

			if (!Game.Renderer.Fonts.TryGetValue(info.Font, out Font))
				throw new YamlException($"Could not find font '{info.Font}'");
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = self.TraitOrDefault<ChatCommands>();
			var help = self.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		void IChatCommand.InvokeCommand(string command, string arg)
		{
			if (command == CommandName)
				Enabled ^= true;
		}
	}
}
