--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

HarkonnenReinforcements =
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

HarkonnenEntryWaypoints = { HarkonnenWaypoint1.Location, HarkonnenWaypoint2.Location, HarkonnenWaypoint3.Location, HarkonnenWaypoint4.Location }
HarkonnenAttackDelay = DateTime.Seconds(30)

HarkonnenAttackWaves =
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

AtreidesReinforcements = { "light_inf", "light_inf", "light_inf" }
AtreidesEntryPath = { AtreidesWaypoint.Location, AtreidesRally.Location }

Messages =
{
	UserInterface.Translate("build-concrete"),
	UserInterface.Translate("build-windtrap"),
	UserInterface.Translate("build-refinery"),
	UserInterface.Translate("build-silo")
}

CachedResources = -1
Tick = function()
	if HarkonnenArrived and Harkonnen.HasNoRequiredUnits() then
		Atreides.MarkCompletedObjective(KillHarkonnen)
	end

	if Atreides.Resources > SpiceToHarvest - 1 then
		Atreides.MarkCompletedObjective(GatherSpice)
	end

	-- player has no Wind Trap
	if (Atreides.PowerProvided <= 20 or Atreides.PowerState ~= "Normal") and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		HasPower = false
		Media.DisplayMessage(Messages[2], Mentat)
	else
		HasPower = true
	end

	-- player has no Refinery and no Silos
	if HasPower and Atreides.ResourceCapacity == 0 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[3], Mentat)
	end

	if HasPower and Atreides.Resources > Atreides.ResourceCapacity * 0.8 and DateTime.GameTime % DateTime.Seconds(32) == 0 then
		Media.DisplayMessage(Messages[4], Mentat)
	end

	if Atreides.Resources ~= CachedResources then
		local parameters = { ["harvested"] = Atreides.Resources, ["goal"] = SpiceToHarvest }
		local harvestedResources = UserInterface.Translate("harvested-resources", parameters)
		UserInterface.SetMissionText(harvestedResources)
		CachedResources = Atreides.Resources
	end
end

WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")

	SpiceToHarvest = ToHarvest[Difficulty]

	InitObjectives(Atreides)
	KillAtreides = AddPrimaryObjective(Harkonnen, "")
	local harvestSpice = UserInterface.Translate("harvest-spice", { ["spice"] = SpiceToHarvest })
	GatherSpice = AddPrimaryObjective(Atreides, harvestSpice)
	KillHarkonnen = AddSecondaryObjective(Atreides, "eliminate-harkonnen-units-reinforcements")

	local checkResourceCapacity = function()
		Trigger.AfterDelay(0, function()
			if Atreides.ResourceCapacity < SpiceToHarvest then
				Media.DisplayMessage(UserInterface.Translate("not-enough-silos"), Mentat)
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Harkonnen.MarkCompletedObjective(KillAtreides)
				end)

				return true
			end
		end)
	end

	Trigger.OnRemovedFromWorld(AtreidesConyard, function()

		-- Mission already failed, no need to check the other conditions as well
		if checkResourceCapacity() then
			return
		end

		local refs = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "refinery" and actor.Owner == Atreides end)
		if #refs == 0 then
			Harkonnen.MarkCompletedObjective(KillAtreides)
		else
			Trigger.OnAllRemovedFromWorld(refs, function()
				Harkonnen.MarkCompletedObjective(KillAtreides)
			end)

			local silos = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "silo" and actor.Owner == Atreides end)
			Utils.Do(refs, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
			Utils.Do(silos, function(actor) Trigger.OnRemovedFromWorld(actor, checkResourceCapacity) end)
		end
	end)

	Media.DisplayMessage(Messages[1], Mentat)

	Trigger.AfterDelay(DateTime.Seconds(25), function()
		Media.PlaySpeechNotification(Atreides, "Reinforce")
		Reinforcements.Reinforce(Atreides, AtreidesReinforcements, AtreidesEntryPath)
	end)

	WavesLeft = HarkonnenAttackWaves[Difficulty]
	SendReinforcements()
end

SendReinforcements = function()
	local units = HarkonnenReinforcements[Difficulty]
	local delay = Utils.RandomInteger(HarkonnenAttackDelay - DateTime.Seconds(2), HarkonnenAttackDelay)
	HarkonnenAttackDelay = HarkonnenAttackDelay - (#units * 3 - 3 - WavesLeft) * DateTime.Seconds(1)
	if HarkonnenAttackDelay < 0 then HarkonnenAttackDelay = 0 end

	Trigger.AfterDelay(delay, function()
		Reinforcements.Reinforce(Harkonnen, Utils.Random(units), { Utils.Random(HarkonnenEntryWaypoints) }, 10, IdleHunt)

		WavesLeft = WavesLeft - 1
		if WavesLeft == 0 then
			Trigger.AfterDelay(DateTime.Seconds(1), function() HarkonnenArrived = true end)
		else
			SendReinforcements()
		end
	end)
end
