--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AttackGroupSize = {8}
AttackDelay = { DateTime.Seconds(2), DateTime.Seconds(4) }

IdlingUnits =
{
	Atreides = { },
	Harkonnen = { },
	Ordos = { },
	Corrino = { }
}

HoldProduction =
{
	Atreides = false,
	Harkonnen = false,
	Ordos = false,
	Corrino = false
}

IsAttacking =
{
	Atreides = false,
	Harkonnen = false,
	Ordos = false,
	Corrino = false
}

AtreidesInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper", "grenadier", "grenadier" }
AtreidesVehicleTypes = { "trike", "trike", "quad" }
AtreidesTankTypes = { "combat_tank_a", "combat_tank_a", "combat_tank_a", "siege_tank" }
AtreidesStarportTypes = { "trike.starport", "quad.starport", "siege_tank.starport", "missile_tank.starport", "combat_tank_a.starport" }

HarkonnenInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper", "mpsardaukar" }
HarkonnenVehicleTypes = { "trike", "quad", "quad" }
HarkonnenTankTypes = { "combat_tank_h", "combat_tank_h", "combat_tank_h", "siege_tank" }
HarkonnenStarportTypes = { "trike.starport", "quad.starport", "siege_tank.starport", "missile_tank.starport", "combat_tank_h.starport" }

OrdosInfantryTypes = { "light_inf", "light_inf", "light_inf", "trooper", "trooper" }
OrdosVehicleTypes = { "raider", "raider", "quad", "stealth_raider" }
OrdosTankTypes = { "combat_tank_o", "combat_tank_o", "combat_tank_o", "siege_tank" }
OrdosStarportTypes = { "trike.starport", "quad.starport", "siege_tank.starport", "missile_tank.starport", "combat_tank_o.starport" }

CorrinoInfantryTypes = { "light_inf", "trooper", "sardaukar", "sardaukar", "sardaukar", "sardaukar" }
CorrinoVehicleTypes = { "trike", "quad", "quad" }
CorrinoTankTypes = { "combat_tank_h", "combat_tank_h", "combat_tank_h", "siege_tank" }
CorrinoStarportTypes = { "trike.starport", "quad.starport", "siege_tank.starport", "missile_tank.starport", "combat_tank_h.starport" }

Upgrades = { "upgrade.barracks", "upgrade.light", "upgrade.conyard", "upgrade.heavy", "upgrade.hightech" }

Harvester = { "harvester" }

AtrCarryHarvWaypoints = { atr_harvcarry_2.Location, atr_harvcarry_1.Location }
HarCarryHarvWaypoints = { har_harvcarry_2.Location, har_harvcarry_1.Location }
OrdCarryHarvWaypoints = { ord_harvcarry_2.Location, ord_harvcarry_1.Location }
CorCarryHarvWaypoints = { cor_harvcarry_2.Location, cor_harvcarry_1.Location }
SmgCarryHarvWaypoints = { smg_harvcarry_2.Location, smg_harvcarry_1.Location }

Produce = function(house, units)
    if HoldProduction[house.Name] then
        Trigger.AfterDelay(DateTime.Minutes(1), function() Produce(house, units) end)
        return
    end

    local delay = Utils.RandomInteger(AttackDelay[1], AttackDelay[2])
    local toBuild = { Utils.Random(units) }
    house.Build(toBuild, function(unit)
		local unitCount = 1
		if IdlingUnits[house.Name] then
			unitCount = 1 + #IdlingUnits[house.Name]
		end
		IdlingUnits[house.Name][unitCount] = unit[1]
        Trigger.AfterDelay(delay, function() Produce(house, units) end)

        if unitCount >= (AttackGroupSize[1] * 2) then
            SendAttack(house)
        end
    end)
end

SetupAttackGroup = function(house)
	local units = { }

	for i = 0, AttackGroupSize[1], 1 do
		if #IdlingUnits[house.Name] == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits[house.Name])

		if IdlingUnits[house.Name][number] and not IdlingUnits[house.Name][number].IsDead then
			units[i] = IdlingUnits[house.Name][number]
			table.remove(IdlingUnits[house.Name], number)
		end
	end

	return units
end

SendAttack = function(house)
	if IsAttacking[house.Name] then
		return
	end
	IsAttacking[house.Name] = true
	HoldProduction[house.Name] = true

	local units = SetupAttackGroup(house)
	Utils.Do(units, function(unit)
		IdleHunt(unit)
	end)

	Trigger.OnAllRemovedFromWorld(units, function()
		IsAttacking[house.Name] = false
		HoldProduction[house.Name] = false
	end)
end

SendNewHarv = function(house, waypoint, count)
	local harvs = house.GetActorsByType("harvester")
	if #harvs < count then
		local harvesters = Reinforcements.ReinforceWithTransport(house, "carryall.reinforce", Harvester, waypoint, { waypoint[1] })[2]
		Utils.Do(harvesters, function(harvester)
			Trigger.OnAddedToWorld(harvester, function()
				InitializeHarvester(harvester)
				SendNewHarv(house, waypoint, count)
			end)
		end)
	end
end

InitializeHarvester = function(harvester)
	harvester.FindResources()
end

Ticks = 0
Speed = 5

Tick = function()
	Ticks = Ticks + 1

	if Ticks > 1 or not Map.IsPausedShellmap then
		local t = (Ticks + 45) % (360 * Speed) * (math.pi / 180) / Speed;
		Camera.Position = ViewportOrigin + WVec.New(19200 * math.sin(t), 28800 * math.cos(t), 0)
	end
end

WorldLoaded = function()
	Atreides = Player.GetPlayer("Atreides")
	Harkonnen = Player.GetPlayer("Harkonnen")
	Ordos = Player.GetPlayer("Ordos")
	Corrino = Player.GetPlayer("Corrino")
	Smugglers = Player.GetPlayer("Smugglers")

	ViewportOrigin = Camera.Position

	Utils.Do(Utils.Take(4, Upgrades), function(upgrade)
		atr_cyard.Produce(upgrade)
		har_cyard.Produce(upgrade)
		ord_cyard.Produce(upgrade)
		cor_cyard.Produce(upgrade)
	end)
	atr_cyard.Produce(Upgrades[5])

	Trigger.AfterDelay(DateTime.Seconds(45), function()
		SendNewHarv(Atreides, AtrCarryHarvWaypoints, 3)
		SendNewHarv(Harkonnen, HarCarryHarvWaypoints, 3)
		SendNewHarv(Ordos, OrdCarryHarvWaypoints, 3)
		SendNewHarv(Corrino, CorCarryHarvWaypoints, 3)
		SendNewHarv(Smugglers, SmgCarryHarvWaypoints, 1)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Produce(Atreides, AtreidesInfantryTypes)
		Produce(Atreides, AtreidesVehicleTypes)
		Produce(Atreides, AtreidesTankTypes)
		Produce(Atreides, AtreidesStarportTypes)

		Produce(Harkonnen, HarkonnenInfantryTypes)
		Produce(Harkonnen, HarkonnenVehicleTypes)
		Produce(Harkonnen, HarkonnenTankTypes)
		Produce(Harkonnen, HarkonnenStarportTypes)

		Produce(Ordos, OrdosInfantryTypes)
		Produce(Ordos, OrdosVehicleTypes)
		Produce(Ordos, OrdosTankTypes)
		Produce(Ordos, OrdosStarportTypes)

		Produce(Corrino, CorrinoInfantryTypes)
		Produce(Corrino, CorrinoVehicleTypes)
		Produce(Corrino, CorrinoTankTypes)
		Produce(Corrino, CorrinoStarportTypes)
	end)
end
