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

using System;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Lint
{
	public interface ILintPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData); }
	public interface ILintMapPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, IMap map); }
	public interface ILintRulesPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules); }
	public interface ILintSequencesPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules, SequenceSet sequences); }
}
