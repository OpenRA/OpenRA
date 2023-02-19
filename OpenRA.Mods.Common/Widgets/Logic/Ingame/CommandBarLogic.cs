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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary> Contains all functions that are unit-specific. </summary>
	public class CommandBarLogic : ChromeLogic
	{
		readonly World world;

		int selectionHash;
		Actor[] selectedActors = Array.Empty<Actor>();
		bool attackMoveDisabled = true;
		bool forceMoveDisabled = true;
		bool forceAttackDisabled = true;
		bool guardDisabled = true;
		bool scatterDisabled = true;
		bool stopDisabled = true;
		bool waypointModeDisabled = true;

		int deployHighlighted;
		int scatterHighlighted;
		int stopHighlighted;

		TraitPair<IIssueDeployOrder>[] selectedDeploys = Array.Empty<TraitPair<IIssueDeployOrder>>();

		[ObjectCreator.UseCtor]
		public CommandBarLogic(Widget widget, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;

			var highlightOnButtonPress = false;
			if (logicArgs.ContainsKey("HighlightOnButtonPress"))
				highlightOnButtonPress = FieldLoader.GetValue<bool>("HighlightOnButtonPress", logicArgs["HighlightOnButtonPress"].Value);

			var attackMoveButton = widget.GetOrNull<ButtonWidget>("ATTACK_MOVE");
			if (attackMoveButton != null)
			{
				WidgetUtils.BindButtonIcon(attackMoveButton);

				attackMoveButton.IsDisabled = () => { UpdateStateIfNecessary(); return attackMoveDisabled; };
				attackMoveButton.IsHighlighted = () => world.OrderGenerator is AttackMoveOrderGenerator;

				void Toggle(bool allowCancel)
				{
					if (attackMoveButton.IsHighlighted())
					{
						if (allowCancel)
							world.CancelInputMode();
					}
					else
						world.OrderGenerator = new AttackMoveOrderGenerator(selectedActors, Game.Settings.Game.MouseButtonPreference.Action);
				}

				attackMoveButton.OnClick = () => Toggle(true);
				attackMoveButton.OnKeyPress = _ => Toggle(false);
			}

			var forceMoveButton = widget.GetOrNull<ButtonWidget>("FORCE_MOVE");
			if (forceMoveButton != null)
			{
				WidgetUtils.BindButtonIcon(forceMoveButton);

				forceMoveButton.IsDisabled = () => { UpdateStateIfNecessary(); return forceMoveDisabled; };
				forceMoveButton.IsHighlighted = () => !forceMoveButton.IsDisabled() && IsForceModifiersActive(Modifiers.Alt);
				forceMoveButton.OnClick = () =>
				{
					if (forceMoveButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(Modifiers.Alt, true);
				};
			}

			var forceAttackButton = widget.GetOrNull<ButtonWidget>("FORCE_ATTACK");
			if (forceAttackButton != null)
			{
				WidgetUtils.BindButtonIcon(forceAttackButton);

				forceAttackButton.IsDisabled = () => { UpdateStateIfNecessary(); return forceAttackDisabled; };
				forceAttackButton.IsHighlighted = () => !forceAttackButton.IsDisabled() && IsForceModifiersActive(Modifiers.Ctrl)
					&& !(world.OrderGenerator is AttackMoveOrderGenerator);

				forceAttackButton.OnClick = () =>
				{
					if (forceAttackButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(Modifiers.Ctrl, true);
				};
			}

			var guardButton = widget.GetOrNull<ButtonWidget>("GUARD");
			if (guardButton != null)
			{
				WidgetUtils.BindButtonIcon(guardButton);

				guardButton.IsDisabled = () => { UpdateStateIfNecessary(); return guardDisabled; };
				guardButton.IsHighlighted = () => world.OrderGenerator is GuardOrderGenerator;

				void Toggle(bool allowCancel)
				{
					if (guardButton.IsHighlighted())
					{
						if (allowCancel)
							world.CancelInputMode();
					}
					else
						world.OrderGenerator = new GuardOrderGenerator(selectedActors,
							"Guard", "guard", Game.Settings.Game.MouseButtonPreference.Action);
				}

				guardButton.OnClick = () => Toggle(true);
				guardButton.OnKeyPress = _ => Toggle(false);
			}

			var scatterButton = widget.GetOrNull<ButtonWidget>("SCATTER");
			if (scatterButton != null)
			{
				WidgetUtils.BindButtonIcon(scatterButton);

				scatterButton.IsDisabled = () => { UpdateStateIfNecessary(); return scatterDisabled; };
				scatterButton.IsHighlighted = () => scatterHighlighted > 0;
				scatterButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						scatterHighlighted = 2;

					PerformKeyboardOrderOnSelection(a => new Order("Scatter", a, false));
				};

				scatterButton.OnKeyPress = ki => { scatterHighlighted = 2; scatterButton.OnClick(); };
			}

			var deployButton = widget.GetOrNull<ButtonWidget>("DEPLOY");
			if (deployButton != null)
			{
				WidgetUtils.BindButtonIcon(deployButton);

				deployButton.IsDisabled = () =>
				{
					UpdateStateIfNecessary();

					var queued = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
					return !selectedDeploys.Any(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued));
				};

				deployButton.IsHighlighted = () => deployHighlighted > 0;
				deployButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						deployHighlighted = 2;

					var queued = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
					PerformDeployOrderOnSelection(queued);
				};

				deployButton.OnKeyPress = ki => { deployHighlighted = 2; deployButton.OnClick(); };
			}

			var stopButton = widget.GetOrNull<ButtonWidget>("STOP");
			if (stopButton != null)
			{
				WidgetUtils.BindButtonIcon(stopButton);

				stopButton.IsDisabled = () => { UpdateStateIfNecessary(); return stopDisabled; };
				stopButton.IsHighlighted = () => stopHighlighted > 0;
				stopButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						stopHighlighted = 2;

					PerformKeyboardOrderOnSelection(a => new Order("Stop", a, false));
				};

				stopButton.OnKeyPress = ki => { stopHighlighted = 2; stopButton.OnClick(); };
			}

			var queueOrdersButton = widget.GetOrNull<ButtonWidget>("QUEUE_ORDERS");
			if (queueOrdersButton != null)
			{
				WidgetUtils.BindButtonIcon(queueOrdersButton);

				queueOrdersButton.IsDisabled = () => { UpdateStateIfNecessary(); return waypointModeDisabled; };
				queueOrdersButton.IsHighlighted = () => !queueOrdersButton.IsDisabled() && IsForceModifiersActive(Modifiers.Shift);
				queueOrdersButton.OnClick = () =>
				{
					if (queueOrdersButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(Modifiers.Shift, false);
				};
			}

			var keyOverrides = widget.GetOrNull<LogicKeyListenerWidget>("MODIFIER_OVERRIDES");
			if (keyOverrides != null)
			{
				var noShiftButtons = new[] { guardButton, deployButton, attackMoveButton };
				var keyUpButtons = new[] { guardButton, attackMoveButton };
				keyOverrides.AddHandler(e =>
				{
					// HACK: allow command buttons to be triggered if the shift (queue order modifier) key is held
					if (e.Modifiers.HasModifier(Modifiers.Shift))
					{
						var eNoShift = e;
						eNoShift.Modifiers &= ~Modifiers.Shift;

						foreach (var b in noShiftButtons)
						{
							// Button is not used by this mod
							if (b == null)
								continue;

							// Button is not valid for this event
							if (b.IsDisabled() || !b.Key.IsActivatedBy(eNoShift))
								continue;

							// Event is not valid for this button
							if (!(b.DisableKeyRepeat ^ e.IsRepeat) || (e.Event == KeyInputEvent.Up && !keyUpButtons.Contains(b)))
								continue;

							b.OnKeyPress(e);
							return true;
						}
					}

					// HACK: Attack move can be triggered if the ctrl (assault move modifier)
					// or shift (queue order modifier) keys are pressed, on both key down and key up
					var eNoMods = e;
					eNoMods.Modifiers &= ~(Modifiers.Ctrl | Modifiers.Shift);

					if (attackMoveButton != null && !attackMoveDisabled && attackMoveButton.Key.IsActivatedBy(eNoMods))
					{
						attackMoveButton.OnKeyPress(e);
						return true;
					}

					return false;
				});
			}
		}

		public override void Tick()
		{
			if (deployHighlighted > 0)
				deployHighlighted--;

			if (scatterHighlighted > 0)
				scatterHighlighted--;

			if (stopHighlighted > 0)
				stopHighlighted--;

			base.Tick();
		}

		bool IsForceModifiersActive(Modifiers modifiers)
		{
			if (world.OrderGenerator is ForceModifiersOrderGenerator fmog && fmog.Modifiers.HasFlag(modifiers))
				return true;

			return world.OrderGenerator is UnitOrderGenerator && Game.GetModifierKeys().HasFlag(modifiers);
		}

		void UpdateStateIfNecessary()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			selectedActors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead)
				.ToArray();

			attackMoveDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<AttackMoveInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			guardDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<GuardInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			forceMoveDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<MobileInfo>() || a.Info.HasTraitInfo<AircraftInfo>());
			forceAttackDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<AttackBaseInfo>());
			scatterDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<IMoveInfo>());

			selectedDeploys = selectedActors
				.SelectMany(a => a.TraitsImplementing<IIssueDeployOrder>()
					.Select(d => new TraitPair<IIssueDeployOrder>(a, d)))
				.ToArray();

			var cbbInfos = selectedActors.Select(a => a.Info.TraitInfoOrDefault<CommandBarBlacklistInfo>()).ToArray();
			stopDisabled = !cbbInfos.Any(i => i == null || !i.DisableStop);
			waypointModeDisabled = !cbbInfos.Any(i => i == null || !i.DisableWaypointMode);

			selectionHash = world.Selection.Hash;
		}

		void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
		{
			UpdateStateIfNecessary();

			var orders = selectedActors
				.Select(f)
				.ToArray();

			foreach (var o in orders)
				world.IssueOrder(o);

			orders.PlayVoiceForOrders();
		}

		void PerformDeployOrderOnSelection(bool queued)
		{
			UpdateStateIfNecessary();

			var orders = selectedDeploys
				.Where(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued))
				.Select(d => d.Trait.IssueDeployOrder(d.Actor, queued))
				.Where(d => d != null)
				.ToArray();

			foreach (var o in orders)
				world.IssueOrder(o);

			orders.PlayVoiceForOrders();
		}
	}
}
