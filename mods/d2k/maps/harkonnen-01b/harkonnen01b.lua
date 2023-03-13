--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AtreidesReinforcements =
{
	easy =
	{
		{ "light_inf", "light_inf" }
	},

	normal =
	{
		{ "light_inf", "light_inf" },
		{ "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "trike" }
	},

	hard =
	{
		{ "light_inf", "light_inf" },
		{ "trike", "trike" },
		{ "light_inf", "light_inf", "light_inf" },
		{ "light_inf", "trike" },
		{ "trike", "trike" }
	}
}

AtreidesEntryWaypoints = { AtreidesWaypoint1.Location, AtreidesWaypoint2.Location, AtreidesWaypoint3.Location, AtreidesWaypoint4.Location }
AtreidesAttackDelay = DateTime.Seconds(30)

AtreidesAttackWaves =
{
	easy = 1,
	normal = 5,
	hard = 12
}

ToHarvest =
{
	easy = 2500,
	normal = 3000,
	hard = 3500
}

HarkonnenReinforcements = { "light_inf", "light_inf", "light_inf", "trike" }
HarkonnenEntryPath = { HarkonnenWaypoint.Location, HarkonnenRally.Location }

Messages =
{
	UserInterface.Translate("build-concrete"),
	UserInterface.Translate("build-windtrap"),
	UserInterface.Translate("build-refinery"),
	UserInterface.Translate("build-silo")
}

CachedResources = -1
Tick = function()
	if AtreidesArrived and Atreides.HasNoRequiredUnits() then
		Harkonnen.MarkCompletedObjective(KillAtreides)
	end

	if Harkonnen.Resources > SpiceToHarvest - 1 then
		Harkonnen.MarkCompletedObjective(GatherSpice)
	end

	-- player has no Wind Trap
	if (Harkonnen.PowerProvided <= 20 or Harkonnen.PowerState ~= "Normal") and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		HasPower = false
		Media.DisplayMessage(Messages[2], Mentat)
	else
		HasPower = true
	end

	-- player has no Refinery and no Silos
	if HasPower and Harkonnen.ResourceCapacity == 0 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[3], Mentat)
	end

	if HasPower and Harkonnen.Resources > Harkonnen.ResourceCapacity * 0.8 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[4], Mentat)
	end

	if Harkonnen.Resources ~= CachedResources then
		local parameters = { ["harvested"] = Harkonnen.Resources, ["goal"] = SpiceToHarvest }
		local harvestedResources = UserInterface.Translate("harvested-resources", parameters)
		UserInterface.SetMissionText(harvestedResources)
		CachedResources = Harkonnen.Resources
	end
end

WorldLoaded = function()
	Harkonnen = Player.GetPlayer("Harkonnen")
	Atreides = Player.GetPlayer("Atreides")

	SpiceToHarvest = ToHarvest[Difficulty]

	InitObjectives(Harkonnen)
	KillHarkonnen = AddPrimaryObjective(Atreides, "")
	local harvestSpice = UserInterface.Translate("harvest-spice", { ["spice"] = SpiceToHarvest })
	GatherSpice = AddPrimaryObjective(Harkonnen, harvestSpice)
	KillAtreides = AddSecondaryObjective(Harkonnen, "eliminate-atreides-units-reinforcements")

	local checkResourceCapacity = function()
		Trigger.AfterDelay(0, function()
			if Harkonnen.ResourceCapacity < SpiceToHarvest then
				Media.DisplayMessage(UserInterface.Translate("not-enough-silos"), Mentat)
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Harkonnen.MarkCompletedObjective(KillAtreides)
				end)

				return true
			end
		end)
	end

	Trigger.OnRemovedFromWorld(HarkonnenConyard, function()

		-- Mission already failed, no need to check the other conditions as well
		if checkResourceCapacity() then
			return
		end

		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" and actor.Owner == Harkonnen end)

		if #refs == 0 then
			Atreides.MarkCompletedObjective(KillHarkonnen)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				Atreides.MarkCompletedObjective(KillHarkonnen)
			end)

			local silos = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "silo" and actor.Owner == Harkonnen end)
			Utils.Do(refs, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
			Utils.Do(silos, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
		end
	end)

	Media.DisplayMessage(Messages[1], Mentat)

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Media.PlaySpeechNotification(Harkonnen, "Reinforce")
		Reinforcements.Reinforce(Harkonnen, HarkonnenReinforcements, HarkonnenEntryPath)
	end)

	WavesLeft = AtreidesAttackWaves[Difficulty]
	SendReinforcements()
end

SendReinforcements = function()
	local units = AtreidesReinforcements[Difficulty]
	local delay = Utils.RandomInteger(AtreidesAttackDelay - DateTime.Seconds(2), AtreidesAttackDelay)
	AtreidesAttackDelay = AtreidesAttackDelay - (#units * 3 - 3 - WavesLeft) * DateTime.Seconds(1)
	if AtreidesAttackDelay < 0 then AtreidesAttackDelay = 0 end

	Trigger.AfterDelay(delay, function()
		Reinforcements.Reinforce(Atreides, Utils.Random(units), { Utils.Random(AtreidesEntryWaypoints) }, 10, IdleHunt)

		WavesLeft = WavesLeft - 1
		if WavesLeft == 0 then
			Trigger.AfterDelay(DateTime.Seconds(1), function() AtreidesArrived = true end)
		else
			SendReinforcements()
		end
	end)
end
