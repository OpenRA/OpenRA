--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EB: GDI owns two bases on the southern mainland. The original
-- mission has one GDI house; the port splits it into GDI (east) and GDIWest
-- so destroying one base does not eliminate the other. Both are allied and
-- send each other relief when a key building is lost. Nod starts on the
-- northern island and receives an MCV and a transport helicopter after the
-- opening build-up. The scripted airstrikes are stopped by destroying the
-- central HQ, the Ion Cannon by destroying the eastern EYE. All timers are
-- compressed compared to the original.
--
-- Design note: the broad base alarm (any repairable building hit) and the
-- broad harvester defense (all nearby ground units react) are deliberate.
-- They are the preferred behavior for the nod13a backport; see
-- HANDOFF-NOD13A.md, section "Aus nod13b übernehmen".

local difficulty
local land = {}
local bases = {}
local gdiPlayers = {}
local guarded = {}
local trackedHarvesters = {}
local retreatingOrcas = {}
local orcaBase = {}
local orcaHandled = {}
local defenseResponses = {}
local buildingInProgress = false
local airliftIndex = 1
local SouthRegion = 0
local RebuildHarvester
local Queue
local RequestRelief

local MaxProducedUnits = { easy = 5, normal = 7, hard = 9 }
local DefenderReserve = { easy = 2, normal = 3, hard = 4 }
local StartingCash = { easy = 7000, normal = 9000, hard = 11000 }
local ReliefSize = { easy = 2, normal = 3, hard = 4 }
local ReliefCooldown = { easy = 150, normal = 110, hard = 80 }
local BaseReliefBuildings = { fact = true, proc = true, weap = true, pyle = true,
	hq = true, eye = true, nuke = true, nuk2 = true, hpad = true, atwr = true, gtwr = true }
-- Original SCB13EB trigger times (tenths of a minute, multiplied by six):
-- attacks at 19.8-29.8 min, first airstrike at 19.8, Ion Cannon at 39.8.
local WaveStart = { easy = 1426, normal = 1188, hard = 1010 }
local FirstLanding = { easy = 706, normal = 588, hard = 500 }
local AirstrikeFirstDelay = { easy = 1426, normal = 1188, hard = 1010 }
local AirstrikeInterval = { easy = 600, normal = 480, hard = 420 }
local IonCannonFirstDelay = { easy = 2866, normal = 2388, hard = 2030 }
local IonCannonInterval = { easy = 900, normal = 720, hard = 600 }
local AirliftFirstDelay = { easy = 1786, normal = 1488, hard = 1265 }
local AirliftInterval = { easy = 480, normal = 360, hard = 300 }
local AutoWaveInterval = { easy = 240, normal = 180, hard = 120 }
local MaxOrcas = { easy = 1, normal = 1, hard = 2 }

local passable = { Clear = true, Rough = true, Road = true, Bridge = true,
	Tiberium = true, BlueTiberium = true, Beach = true }

local function IsLive(actor)
	return actor ~= nil and actor.Owner ~= nil and not actor.IsDead
end

local function IsGDIPlayer(player)
	for _, p in ipairs(gdiPlayers) do
		if p == player then return true end
	end
	return false
end

local function AnyGDIPlayer()
	for _, p in ipairs(gdiPlayers) do
		if p ~= nil and not HouseEliminated(p) then return p end
	end
	return nil
end

local function GDIOrcaCount()
	local count = 0
	for _, p in ipairs(gdiPlayers) do count = count + #p.GetActorsByType("orca") end
	return count
end

local function CellKey(x, y)
	return y * 64 + x
end

local function BuildLandRegions()
	local nextRegion = 0
	for y = 1, 60 do
		for x = 2, 61 do
			local key = CellKey(x, y)
			if land[key] == nil then
				if not passable[Map.TerrainType(CPos.New(x, y))] then
					land[key] = 0
				else
					nextRegion = nextRegion + 1
					land[key] = nextRegion
					local queue, index = { { x, y } }, 1
					while index <= #queue do
						local cell = queue[index]
						index = index + 1
						for _, d in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 },
							{ 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }) do
							local nx, ny = cell[1] + d[1], cell[2] + d[2]
							local nk = CellKey(nx, ny)
							if nx >= 2 and nx <= 61 and ny >= 1 and ny <= 60 and land[nk] == nil then
								if passable[Map.TerrainType(CPos.New(nx, ny))] then
									land[nk] = nextRegion
									table.insert(queue, { nx, ny })
								else
									land[nk] = 0
								end
							end
						end
					end
				end
			end
		end
	end
end

local function Region(cell)
	if cell.X < 2 or cell.X > 61 or cell.Y < 1 or cell.Y > 60 then return 0 end
	return land[CellKey(cell.X, cell.Y)] or 0
end

local function SameLand(a, b)
	local region = Region(a)
	return region ~= 0 and region == Region(b)
end

local function ActorRegion(actor)
	local center = CPos.New(math.floor(actor.CenterPosition.X / 1024), math.floor(actor.CenterPosition.Y / 1024))
	local region = Region(center)
	if region == 0 then region = Region(actor.Location) end
	return region
end

local function TargetOnLand(origin, target)
	if Region(target.Location) ~= 0 then return SameLand(origin, target.Location) end
	for dx = -1, 3 do
		for dy = -1, 3 do
			if SameLand(origin, CPos.New(target.Location.X + dx, target.Location.Y + dy)) then return true end
		end
	end
	return false
end

local function IsTerrainObstacle(actor)
	return actor.Type == "brik" or actor.Type == "cycl" or actor.Type:match("^rock%d") ~= nil
		or actor.Type:match("^t%d%d") ~= nil or actor.Type:match("^split") ~= nil
		or actor.Type:match("%.husk$") ~= nil
end

local function GDIFunds(player)
	return player.Cash + player.Resources
end

local function SpendGDIFunds(player, amount)
	local resources = math.min(player.Resources, amount)
	player.Resources = player.Resources - resources
	player.Cash = player.Cash - amount + resources
	return resources
end

local function Owned(player, kind)
	for _, actor in ipairs(player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.Owner == player and actor.IsInWorld then return actor end
	end
	return nil
end

local function IsLocal(base, actor)
	if not actor.HasProperty("Location") then return false end
	local dx, dy = actor.Location.X - base.yard[1], actor.Location.Y - base.yard[2]
	return dx * dx + dy * dy <= 32 * 32
end

local function BaseAt(cell)
	for _, base in ipairs(bases) do
		local dx, dy = cell.X - base.yard[1], cell.Y - base.yard[2]
		if dx * dx + dy * dy <= 24 * 24 then return base end
	end
	return nil
end

local function LocalBuilding(base, kind)
	local best, bestDistance
	for _, actor in ipairs(base.player.GetActorsByType(kind)) do
		if IsLive(actor) and actor.Owner == base.player and actor.IsInWorld and IsLocal(base, actor) then
			local dx, dy = actor.Location.X - base.yard[1], actor.Location.Y - base.yard[2]
			local distance = dx * dx + dy * dy
			if bestDistance == nil or distance < bestDistance then
				best, bestDistance = actor, distance
			end
		end
	end
	return best
end

local function LocalTech(base, kind)
	if kind == "proc" or kind == "pyle" then
		return LocalBuilding(base, "nuke") ~= nil or LocalBuilding(base, "nuk2") ~= nil
	elseif kind == "weap" or kind == "hq" or kind == "silo" or kind == "harv" then
		return LocalBuilding(base, "proc") ~= nil
	elseif kind == "gtwr" or kind == "e1" or kind == "e3" then
		return LocalBuilding(base, "pyle") ~= nil
	elseif kind == "atwr" or kind == "nuk2" or kind == "eye" or kind == "e2" or kind == "mtnk" or kind == "msam" then
		return LocalBuilding(base, "hq") ~= nil or LocalBuilding(base, "eye") ~= nil
	elseif kind == "htnk" then
		return LocalBuilding(base, "eye") ~= nil or LocalBuilding(base, "hq") ~= nil
	elseif kind == "hpad" then
		return LocalBuilding(base, "proc") ~= nil
	elseif kind == "orca" then
		return LocalBuilding(base, "hpad") ~= nil
			and (LocalBuilding(base, "hq") ~= nil or LocalBuilding(base, "eye") ~= nil)
	end
	return true
end

local function BaseForPlayer(player)
	for _, base in ipairs(bases) do
		if base.player == player then return base end
	end
	return nil
end

local function BaseForHarvester(harv)
	local best, bestDistance
	for _, base in ipairs(bases) do
		local proc = LocalBuilding(base, "proc")
		local anchor = proc or LocalBuilding(base, "fact")
		if anchor ~= nil then
			local dx = harv.Location.X - anchor.Location.X
			local dy = harv.Location.Y - anchor.Location.Y
			local distance = dx * dx + dy * dy
			if best == nil or distance < bestDistance then
				best, bestDistance = base, distance
			end
		end
	end
	return best or bases[1]
end

local function HasLiveHarvester(base)
	return IsLive(base.harvester) and base.harvester.IsInWorld and base.harvester.Owner == base.player
end

local function PruneReserve(base)
	for i = #base.reserve, 1, -1 do
		local unit = base.reserve[i]
		if not IsLive(unit) or unit.Owner ~= base.player then table.remove(base.reserve, i) end
	end
end

local function ProducedCounts(base)
	for i = #base.produced, 1, -1 do
		local unit = base.produced[i]
		if not IsLive(unit) or unit.Owner ~= base.player then table.remove(base.produced, i) end
	end

	local total, infantryCount, vehicleCount = 0, 0, 0
	for _, unit in ipairs(base.produced) do
		total = total + 1
		if unit.Type == "jeep" or unit.Type == "mtnk" or unit.Type == "msam" or unit.Type == "htnk" then
			vehicleCount = vehicleCount + 1
		else
			infantryCount = infantryCount + 1
		end
	end
	return total, infantryCount, vehicleCount
end

local function RemoveProduced(base, team)
	for _, unit in ipairs(team) do
		for i = #base.produced, 1, -1 do
			if base.produced[i] == unit then table.remove(base.produced, i) end
		end
	end
end

local function ClosestEnemy(unit, maxDistance, capture)
	local target, distance
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
			and actor.HasProperty("Health") and not actor.HasProperty("Land")
			and TargetOnLand(unit.Location, actor)
			and (not capture or unit.CanCapture(actor)) then
			local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
			local d = dx * dx + dy * dy
			if (maxDistance == nil or d <= maxDistance * maxDistance) and (distance == nil or d < distance) then
				target, distance = actor, d
			end
		end
	end
	return target
end

local function OrderAttack(unit)
	if unit.HasProperty("AttackMove") then
		local target = ClosestEnemy(unit)
		if target ~= nil then unit.AttackMove(target.Location, 2) end
	elseif unit.HasProperty("Capture") and unit.HasProperty("CanCapture") then
		local target = ClosestEnemy(unit, nil, true)
		if target ~= nil then
			unit.Capture(target)
		elseif unit.HasProperty("Move") then
			local enemy = ClosestEnemy(unit)
			if enemy ~= nil then unit.Move(enemy.Location) end
		end
	end
end

local function WatchAttackers(team, owner)
	if MissionFinished then return end
	local alive = false
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner then
			alive = true
			if unit.IsInWorld and unit.IsIdle then OrderAttack(unit) end
		end
	end
	if alive then Trigger.AfterDelay(DateTime.Seconds(10), function() WatchAttackers(team, owner) end) end
end

local function SendTeam(team, owner, route, attack)
	if MissionFinished then return end
	for _, unit in ipairs(team) do
		if IsLive(unit) and unit.Owner == owner and unit.IsInWorld then
			if unit.HasProperty("Stance") then unit.Stance = attack and "AttackAnything" or "Defend" end
			for _, waypoint in ipairs(route) do
				if SameLand(unit.Location, waypoint.Location) then
					if unit.HasProperty("AttackMove") then unit.AttackMove(waypoint.Location)
					elseif unit.HasProperty("Move") then unit.Move(waypoint.Location) end
				end
			end
			if attack and #route == 0 then OrderAttack(unit) end
		end
	end
	if attack and #team > 0 then WatchAttackers(team, owner) end
end

local function TakeForAttack(base, limit)
	local team = {}
	local taken = {}
	PruneReserve(base)
	for i = #base.reserve, 1, -1 do
		if #team >= limit then break end
		local unit = base.reserve[i]
		if IsLive(unit) and unit.Owner == base.player and unit.IsInWorld and not taken[unit] then
			table.remove(base.reserve, i)
			table.insert(team, unit)
			taken[unit] = true
		end
	end
	for _, unit in ipairs(base.produced) do
		if #team >= limit then break end
		if IsLive(unit) and unit.Owner == base.player and unit.IsInWorld and not taken[unit] then
			table.insert(team, unit)
			taken[unit] = true
		end
	end
	-- Offensive units no longer count against the production cap; the freed
	-- slot is used for the next garrison unit.
	RemoveProduced(base, team)
	return team
end

local function JoinGarrison(base, unit)
	if not IsLive(unit) or unit.Owner ~= base.player then return end
	if base.defending then
		RemoveProduced(base, { unit })
		SendTeam({ unit }, base.player, {}, true)
		return
	end
	PruneReserve(base)
	if #base.reserve < DefenderReserve[difficulty] then table.insert(base.reserve, unit) end
	if unit.HasProperty("Stance") then unit.Stance = "Defend" end
end

local function FindUnitSpot(anchor)
	for radius = 0, 12 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				if math.abs(dx) == radius or math.abs(dy) == radius then
					local cell = CPos.New(anchor.X + dx, anchor.Y + dy)
					if cell.X >= 2 and cell.X <= 61 and cell.Y >= 1 and cell.Y <= 60
						and SameLand(anchor, cell) then
						local terrain = Map.TerrainType(cell)
						if terrain == "Clear" or terrain == "Road" or terrain == "Beach" then
							local blockers = Map.ActorsInCircle(Map.CenterOfCell(cell), WDist.New(1024), function(actor)
								return IsLive(actor) and actor.HasProperty("Location")
									and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
							end)
							if #blockers == 0 then return cell end
						end
					end
				end
			end
		end
	end
	return nil
end

-- A base that loses a key building or its harvester asks the other base for
-- help. A scratch does not trigger this; relief also leaves a home guard.
RequestRelief = function(base)
	if MissionFinished or base == nil or HouseEliminated(base.player) then return end
	if DateTime.GameTime < (base.reliefCooldown or 0) then return end
	base.reliefCooldown = DateTime.GameTime + DateTime.Seconds(ReliefCooldown[difficulty])
	for _, helper in ipairs(bases) do
		if helper ~= base and not HouseEliminated(helper.player) then
			local available = #helper.reserve + #helper.produced
			local keep = math.min(DefenderReserve[difficulty], available)
			local takeLimit = math.min(ReliefSize[difficulty], math.max(0, available - keep))
			local team = TakeForAttack(helper, takeLimit)
			local taken = {}
			for _, unit in ipairs(team) do taken[unit] = true end
			local tracked = {}
			for _, unit in ipairs(helper.reserve) do tracked[unit] = true end
			for _, unit in ipairs(helper.produced) do tracked[unit] = true end
			-- Early in the mission most of a base's force is still placed on
			-- the map, so allow idle Defend units to join the relief force.
			-- Units still tracked by the base must stay in its production pool.
			for _, unit in ipairs(helper.player.GetGroundAttackers()) do
				if #team >= ReliefSize[difficulty] then break end
				if not taken[unit] and not tracked[unit] and IsLive(unit) and unit.IsInWorld
					and unit.HasProperty("Stance") and unit.Stance == "Defend" and IsLocal(helper, unit)
					and SameLand(unit.Location, CPos.New(base.yard[1], base.yard[2])) then
					table.insert(team, unit)
					taken[unit] = true
				end
			end
			if #team > 0 then
				local cell = FindUnitSpot(CPos.New(base.yard[1], base.yard[2]))
				if cell == nil then cell = CPos.New(base.yard[1], base.yard[2]) end
				SendTeam(team, helper.player, { { Location = cell } }, false)
			end
		end
	end
end

local function DefendAgainst(actor, attacker, damage)
	if MissionFinished or not IsLive(actor) or not actor.IsInWorld or not actor.HasProperty("Location")
		or damage == nil or damage <= 0 then return end
	if not IsLive(attacker) or attacker.Owner ~= Nod or not attacker.HasProperty("Location") then return end
	if actor.HasProperty("StartBuildingRepairs") and actor.Health < actor.MaxHealth * 0.75 then
		actor.StartBuildingRepairs()
	end

	local base = BaseForPlayer(actor.Owner)
	if base ~= nil and not IsLocal(base, actor) then base = nil end
	if base ~= nil then
		base.defending = true
		base.defenseUntil = math.max(base.defenseUntil, DateTime.GameTime + DateTime.Seconds(45))
	end

	-- A hit on a base building puts the reserved Orcas into the fight.
	if base ~= nil and actor.HasProperty("StartBuildingRepairs") then
		for _, player in ipairs(gdiPlayers) do
			for _, orca in ipairs(player.GetActorsByType("orca")) do
				if IsLive(orca) and orca.IsInWorld and orca.HasProperty("Stance")
					and orca.Stance ~= "AttackAnything" then
					orca.Stance = "AttackAnything"
					IdleHunt(orca)
				end
			end
		end
	end

	for i = #defenseResponses, 1, -1 do
		local response = defenseResponses[i]
		if DateTime.GameTime >= response.expires then
			table.remove(defenseResponses, i)
		elseif response.attacker == attacker then
			return
		end
	end
	table.insert(defenseResponses, { attacker = attacker, expires = DateTime.GameTime + DateTime.Seconds(2) })

	-- A hit on a base building commits part of that base's reserve.
	if base ~= nil and actor.HasProperty("StartBuildingRepairs")
		and DateTime.GameTime >= (base.reserveRelease or 0) then
		base.reserveRelease = DateTime.GameTime + DateTime.Seconds(30)
		local team = TakeForAttack(base, 2)
		if #team > 0 then SendTeam(team, base.player, {}, true) end
	end

	local radius = actor.HasProperty("StartBuildingRepairs") and 18 or 10
	for _, player in ipairs(gdiPlayers) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if IsLive(unit) and unit.IsInWorld and unit.HasProperty("AttackMove") and unit ~= actor
				and SameLand(unit.Location, attacker.Location) then
				local dx, dy = unit.Location.X - actor.Location.X, unit.Location.Y - actor.Location.Y
				if dx * dx + dy * dy <= radius * radius then
					if unit.HasProperty("Stop") then unit.Stop() end
					unit.AttackMove(attacker.Location, 2)
				end
			end
		end
	end
end

local function Guard(actor)
	if not IsLive(actor) then return end
	for _, other in ipairs(guarded) do
		if other == actor then return end
	end
	table.insert(guarded, actor)
	Trigger.OnDamaged(actor, DefendAgainst)
	if actor.HasProperty("StartBuildingRepairs") then
		Trigger.OnCapture(actor, function(_, _, _, newOwner)
			if IsGDIPlayer(newOwner) then Guard(actor) end
		end)
	end
end

-- Orcas are the mobile reserve and stay on their helipad until their base
-- comes under attack. DefendAgainst releases them when a base building is hit;
-- the same pattern is used in nod13a.
local function OrcaDamaged(base, orca, attacker, damage)
	if MissionFinished or base == nil or damage == nil or damage <= 0 or not IsLive(orca) then return end
	if retreatingOrcas[orca] then return end
	if orca.Health * 4 > orca.MaxHealth or LocalBuilding(base, "hpad") == nil then return end
	retreatingOrcas[orca] = true
	orca.Stop()
	orca.ReturnToBase()
end

local function GuardOrca(base, orca)
	if not IsLive(orca) or orca.Type ~= "orca" then return end
	Guard(orca)
	if base ~= nil then orcaBase[orca] = base end
	if not orcaHandled[orca] then
		orcaHandled[orca] = true
		Trigger.OnDamaged(orca, function(actor, attacker, damage)
			OrcaDamaged(orcaBase[actor] or BaseForPlayer(actor.Owner) or BaseAt(actor.Location), actor, attacker, damage)
		end)
	end
	if orca.HasProperty("Stance") then orca.Stance = "Defend" end
	if base ~= nil and base.defending then
		if orca.HasProperty("Stance") then orca.Stance = "AttackAnything" end
		IdleHunt(orca)
	end
end

local function OrcaWatch()
	if MissionFinished then return end
	for _, player in ipairs(gdiPlayers) do
		for _, orca in ipairs(player.GetActorsByType("orca")) do
			if IsLive(orca) and retreatingOrcas[orca] then
				local base = orcaBase[orca] or BaseForPlayer(orca.Owner) or BaseAt(orca.Location)
				if base == nil or LocalBuilding(base, "hpad") == nil or orca.Health * 10 >= orca.MaxHealth * 9 then
					retreatingOrcas[orca] = nil
					if base ~= nil and base.defending then
						if orca.HasProperty("Stance") then orca.Stance = "AttackAnything" end
						IdleHunt(orca)
					end
				end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(5), OrcaWatch)
end

local function TrackHarvester(base, harv)
	if harv == nil or base == nil or trackedHarvesters[harv] then return end
	trackedHarvesters[harv] = true
	base.harvester = harv
	Trigger.OnKilled(harv, function()
		if base.harvester == harv then base.harvester = nil end
		Trigger.AfterDelay(1, function()
			RebuildHarvester(base)
			RequestRelief(base)
		end)
	end)
end

local footprint = { fact = { 3, 3 }, proc = { 3, 4 }, weap = { 3, 3 },
	pyle = { 2, 3 }, nuk2 = { 2, 3 }, atwr = { 1, 1 }, gtwr = { 1, 1 },
	silo = { 2, 1 }, hq = { 2, 3 }, eye = { 2, 3 }, hpad = { 2, 2 }, fix = { 3, 3 } }
local emptyFootprintCells = { proc = { [0] = true, [2] = true }, hq = { [1] = true }, eye = { [1] = true } }

local function OccupiesFootprint(kind, x, y)
	local empty = emptyFootprintCells[kind]
	return empty == nil or not empty[y * 4 + x]
end

local function BuildingAt(entry)
	for _, actor in ipairs(Map.ActorsInWorld) do
		if IsLive(actor) and actor.Type == entry[1] and actor.HasProperty("Location")
			and actor.Location.X == entry[2] and actor.Location.Y == entry[3] then return actor end
	end
	return nil
end

local function SiteFree(entry, ignore, owner)
	local size = footprint[entry[1]]
	if size == nil then return false end
	for x = entry[2], entry[2] + size[1] - 1 do
		for y = entry[3], entry[3] + size[2] - 1 do
			local terrain = Map.TerrainType(CPos.New(x, y))
			if OccupiesFootprint(entry[1], x - entry[2], y - entry[3])
				and terrain ~= "Clear" and terrain ~= "Road" then return false end
		end
	end
	local topLeft = WPos.New((entry[2] - 4) * 1024, (entry[3] - 4) * 1024, 0)
	local bottomRight = WPos.New((entry[2] + size[1] + 4) * 1024, (entry[3] + size[2] + 4) * 1024, 0)
	local blockers = Map.ActorsInBox(topLeft, bottomRight, function(actor)
		return IsLive(actor) and actor ~= ignore and actor.HasProperty("Location")
			and (actor.HasProperty("Health") or IsTerrainObstacle(actor))
	end)
	for _, actor in ipairs(blockers) do
		if not actor.Type:match("%.husk$") then
			local otherSize = footprint[actor.Type] or (actor.Type:match("^rock%d") and { 4, 2 })
				or (actor.Type:match("^t%d%d") and { 2, 2 })
				or (actor.HasProperty("StartBuildingRepairs") and { 3, 3 } or { 1, 1 })
			local x, y = actor.Location.X, actor.Location.Y
			for cx = math.max(x, entry[2]), math.min(x + otherSize[1], entry[2] + size[1]) - 1 do
				for cy = math.max(y, entry[3]), math.min(y + otherSize[2], entry[3] + size[2]) - 1 do
					if OccupiesFootprint(entry[1], cx - entry[2], cy - entry[3])
						and OccupiesFootprint(actor.Type, cx - x, cy - y) then
						if actor.Owner == owner and actor.HasProperty("Move") and not actor.HasProperty("Land") then
							actor.Move(CPos.New(entry[2], entry[3] + size[2] + 1))
						end
						return false
					end
				end
			end
		end
	end
	return true
end

local function FindNearbySite(base, kind, anchor, ignore)
	local size = footprint[kind]
	if size == nil then return nil end
	for radius = 0, 8 do
		for dx = -radius, radius do
			for dy = -radius, radius do
				local x, y = base.yard[1] + dx, base.yard[2] + dy
				if (math.abs(dx) == radius or math.abs(dy) == radius)
					and x >= 2 and x + size[1] - 1 <= 61 and y >= 2 and y + size[2] - 1 <= 61 then
					local candidate = { kind, x, y }
					if SameLand(anchor, CPos.New(x + 1, y + 1)) and SiteFree(candidate, ignore, nil) then
						return candidate
					end
				end
			end
		end
	end
	return nil
end

local function WatchMCV(base, mcv, tries)
	if MissionFinished then return end
	if base.mcv ~= mcv then return end
	if not IsLive(mcv) or mcv.Owner ~= base.player then base.mcv = nil return end
	if LocalBuilding(base, "fact") ~= nil then base.mcv = nil return end
	if mcv.IsInWorld then
		local entry = base.mcvSite
		if entry == nil or not SiteFree(entry, mcv, nil) then
			entry = FindNearbySite(base, "fact", mcv.Location, mcv)
			base.mcvSite = entry
		end
		if entry ~= nil then
			local x, y = entry[2] + 1, entry[3] + 1
			if mcv.Location.X == x and mcv.Location.Y == y then
				if mcv.HasProperty("Deploy") then mcv.Deploy() end
			elseif tries % 5 == 0 and mcv.HasProperty("Move") then
				mcv.Move(CPos.New(x, y))
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), function() WatchMCV(base, mcv, tries + 1) end)
end

-- Original [Base] build plan. WEAP and ATWR were shifted by the TD importer.
local nodes = {
	{ "fix", 12, 27 }, { "pyle", 22, 26 }, { "nuk2", 3, 29 }, { "nuk2", 5, 29 },
	{ "nuk2", 3, 32 }, { "nuk2", 5, 32 }, { "silo", 3, 35 }, { "silo", 5, 35 },
	{ "silo", 7, 35 }, { "proc", 15, 30 }, { "fact", 21, 34 }, { "hq", 4, 26 },
	{ "atwr", 19, 27 }, { "atwr", 34, 48 }, { "atwr", 15, 27 }, { "gtwr", 14, 38 },
	{ "gtwr", 19, 24 }, { "gtwr", 18, 38 }, { "gtwr", 14, 24 }, { "weap", 21, 30 },
	{ "silo", 28, 46 }, { "silo", 36, 49 }, { "silo", 34, 49 }, { "silo", 30, 46 },
	{ "proc", 29, 48 }, { "fact", 58, 48 }, { "eye", 59, 51 }, { "pyle", 49, 49 },
	{ "nuk2", 26, 47 }, { "nuk2", 49, 52 }, { "nuk2", 47, 52 }, { "nuk2", 53, 45 },
	{ "atwr", 45, 47 }, { "atwr", 60, 46 }, { "gtwr", 44, 44 }, { "gtwr", 48, 43 },
	{ "gtwr", 43, 51 }, { "gtwr", 43, 49 }, { "gtwr", 27, 50 }, { "gtwr", 26, 46 },
	{ "weap", 50, 45 },
}

local prerequisites = {
	proc = { "anypower" }, weap = { "proc" }, pyle = { "anypower" },
	gtwr = { "barracks" }, atwr = { "anyhq", "~techlevel.medium" },
	nuk2 = { "anyhq", "~techlevel.medium" }, hq = { "proc", "~techlevel.medium" },
	eye = { "anyhq", "~techlevel.high" }, silo = { "proc" },
	hpad = { "proc" }, orca = { "hpad", "anyhq", "~techlevel.medium" },
	fix = { "proc" }, fact = { "proc" },
}

local function StartBuilding(entry, yard)
	if buildingInProgress then return false end
	local player = yard.Owner
	local kind = entry[1]
	local cost = Actor.Cost(kind)
	local queue = (kind == "gtwr" or kind == "atwr" or kind == "silo") and "Support.GDI" or "Building.GDI"
	local buildTime = Actor.BuildTime(kind, queue)
	if player.PowerState == "Low" or player.PowerState == "Critical" then
		buildTime = math.ceil(buildTime * 1.5)
	end
	buildingInProgress = true
	local spentResources = SpendGDIFunds(player, cost)
	Trigger.AfterDelay(buildTime, function()
		buildingInProgress = false
		if MissionFinished then return end
		if not IsLive(yard) or yard.Owner ~= player or not yard.IsInWorld
			or BuildingAt(entry) ~= nil or not SiteFree(entry, nil, player) then
			player.Resources = player.Resources + spentResources
			player.Cash = player.Cash + cost - spentResources
			return
		end
		local building = Actor.Create(kind, true, { Owner = player, Location = CPos.New(entry[2], entry[3]) })
		Guard(building)
	end)
	return true
end

-- A base flattened down to its construction yard cannot satisfy any
-- prerequisite chain, so rebuild the power plant, refinery and barracks
-- without their tech gates first.
local function Bootstrap(base)
	if buildingInProgress then return false end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return false end

	local function TryNode(kind)
		for _, entry in ipairs(base.nodes) do
			if entry[1] == kind and BuildingAt(entry) == nil
				and GDIFunds(base.player) >= Actor.Cost(kind) and SiteFree(entry, nil, base.player) then
				return StartBuilding(entry, yard)
			end
		end
		return false
	end

	if Owned(base.player, "nuke") == nil and Owned(base.player, "nuk2") == nil then return TryNode("nuk2") end
	if Owned(base.player, "proc") == nil then return TryNode("proc") end
	if Owned(base.player, "pyle") == nil then return TryNode("pyle") end
	return false
end

local function Rebuild(base)
	if buildingInProgress then return end
	local yard = LocalBuilding(base, "fact")
	if yard == nil then return end
	if Bootstrap(base) then return end
	if not HasLiveHarvester(base) then return end
	for _ = 1, #base.nodes do
		base.nodeIndex = (base.nodeIndex or 1) % #base.nodes + 1
		local entry = base.nodes[base.nodeIndex]
		-- The EYE is not rebuilt: destroying it permanently stops the Ion Cannon.
		if entry[1] ~= "eye" and BuildingAt(entry) == nil then
			local cost = Actor.Cost(entry[1])
			local reserve = (entry[1] == "proc" or entry[1] == "weap" or entry[1] == "pyle") and 1200 or 2200
			local required = prerequisites[entry[1]] or {}
			if GDIFunds(base.player) >= cost + reserve and base.player.HasPrerequisites(required)
				and SiteFree(entry, nil, base.player) then
				if StartBuilding(entry, yard) then return end
			end
		end
	end
end

function RebuildHarvester(base)
	if MissionFinished or base == nil or base.harvesterBuilding then return end
	if HasLiveHarvester(base) or DateTime.GameTime < (base.harvesterCooldown or 0) then return end
	local factory = LocalBuilding(base, "weap")
	if factory ~= nil and LocalTech(base, "harv") and base.player.HasPrerequisites({ "proc" })
		and GDIFunds(base.player) >= Actor.Cost("harv") then
		base.harvesterBuilding = true
		if Queue(factory, "harv", base.player) then
			Trigger.AfterDelay(DateTime.Seconds(120), function() base.harvesterBuilding = false end)
			return
		end
		base.harvesterBuilding = false
	end
	local proc = LocalBuilding(base, "proc")
	if proc ~= nil and GDIFunds(base.player) >= Actor.Cost("harv") then
		local cell = FindUnitSpot(proc.Location)
		if cell ~= nil then
			SpendGDIFunds(base.player, Actor.Cost("harv"))
			local harv = Actor.Create("harv", true, { Owner = base.player, Location = cell })
			if IsLive(harv) then
				base.harvester = harv
				Guard(harv)
				TrackHarvester(base, harv)
				if harv.HasProperty("FindResources") then harv.FindResources() end
			end
			base.harvesterCooldown = DateTime.GameTime + DateTime.Seconds(120)
		end
	end
end

function Queue(factory, kind, player)
	if not IsLive(factory) or factory.Owner ~= player or not factory.IsInWorld or not factory.HasProperty("Build")
		or not factory.HasProperty("IsProducing") or factory.IsProducing(kind) or GDIFunds(player) < Actor.Cost(kind) then
		return false
	end
	return factory.Build({ kind })
end

local infantry = { "e3", "e1", "e1", "e1" }
local eastInfantry = { "e1", "e2", "e3", "e1" }
local westVehicles = { "mtnk", "jeep", "jeep" }
local eastVehicles = { "htnk", "mtnk", "msam", "jeep" }
local unitPrerequisites = {
	e1 = { "barracks" }, e3 = { "barracks" }, e2 = { "anyhq", "~techlevel.medium" },
	jeep = { "weap" }, mtnk = { "anyhq", "~techlevel.medium" },
	msam = { "anyhq", "~techlevel.medium" }, htnk = { "eye", "~techlevel.high" },
}

local function CombatOrderPending(base)
	local pending = base.combatPending
	if pending == nil then return false end
	local factory = pending.factory
	if not IsLive(factory) or factory.Owner ~= base.player or not factory.IsInWorld
		or not factory.HasProperty("IsProducing") then
		base.combatPending = nil
		return false
	end
	if factory.IsProducing(pending.kind) then
		pending.emptySince = nil
	elseif pending.emptySince == nil then
		pending.emptySince = DateTime.GameTime
	elseif DateTime.GameTime > pending.emptySince then
		base.combatPending = nil
		return false
	end
	return true
end

local function Produce(base)
	local factory = LocalBuilding(base, "weap")
	if LocalBuilding(base, "fact") == nil then
		-- A lost construction yard is recovered by deploying a new MCV.
		if base.mcv == nil then
			local mcv = LocalBuilding(base, "mcv")
			if mcv ~= nil then
				base.mcv = mcv
				WatchMCV(base, mcv, 0)
			elseif factory ~= nil then
				Queue(factory, "mcv", base.player)
			end
		end
		return
	end
	if not HasLiveHarvester(base) then
		if factory ~= nil and LocalTech(base, "harv") and base.player.HasPrerequisites({ "proc" }) then
			Queue(factory, "harv", base.player)
		end
		return
	end
	if CombatOrderPending(base) then return end
	local pyle = LocalBuilding(base, "pyle")
	if (pyle ~= nil and pyle.HasProperty("IsProducing") and pyle.IsProducing("e1"))
		or (factory ~= nil and factory.HasProperty("IsProducing") and factory.IsProducing("jeep")) then return end

	local total, infantryCount, vehicleCount = ProducedCounts(base)
	if total >= MaxProducedUnits[difficulty] then return end
	local categoryLimit = math.max(4, math.floor(MaxProducedUnits[difficulty] / 2))
	local vehicles = base == bases[1] and westVehicles or eastVehicles
	local infantryTypes = base == bases[1] and infantry or eastInfantry
	local specs = base.nextProduction == "vehicle"
		and { { "weap", vehicles, vehicleCount }, { "pyle", infantryTypes, infantryCount } }
		or { { "pyle", infantryTypes, infantryCount }, { "weap", vehicles, vehicleCount } }
	for _, spec in ipairs(specs) do
		local producer = LocalBuilding(base, spec[1])
		if producer ~= nil and spec[3] < categoryLimit then
			for _ = 1, #spec[2] do
				local kind = table.remove(spec[2], 1)
				table.insert(spec[2], kind)
				if LocalTech(base, kind) and base.player.HasPrerequisites(unitPrerequisites[kind])
					and GDIFunds(base.player) >= Actor.Cost(kind) + 500
					and Queue(producer, kind, base.player) and producer.IsProducing(kind) then
					base.combatPending = { factory = producer, kind = kind }
					base.nextProduction = spec[1] == "pyle" and "vehicle" or "infantry"
					return
				end
			end
		end
	end
end

local function OnProduced(factory, unit, productionType)
	if MissionFinished or not IsLive(factory) or not IsLive(unit)
		or not IsGDIPlayer(factory.Owner) or unit.Owner ~= factory.Owner then return end
	local base = BaseForPlayer(factory.Owner) or BaseAt(factory.Location)
	if base == nil then return end
	if base.combatPending ~= nil and base.combatPending.factory == factory
		and base.combatPending.kind == unit.Type then base.combatPending = nil end
	if unit.Type == "mcv" then
		Guard(unit)
		base.mcv = unit
		base.mcvSite = nil
		WatchMCV(base, unit, 0)
	elseif unit.Type == "harv" then
		base.harvesterBuilding = false
		base.harvester = unit
		Guard(unit)
		TrackHarvester(base, unit)
		if unit.HasProperty("FindResources") then unit.FindResources() end
	elseif unit.Type == "orca" then
		GuardOrca(base, unit)
	elseif unit.HasProperty("AttackMove") then
		Guard(unit)
		table.insert(base.produced, unit)
		JoinGarrison(base, unit)
	end
end

local function EnsureHelipad(base)
	if MissionFinished or LocalBuilding(base, "hpad") ~= nil then return end
	local yard = LocalBuilding(base, "fact")
	if yard == nil or GDIFunds(base.player) < Actor.Cost("hpad") + 1500 then return end
	if not LocalTech(base, "hpad") or not base.player.HasPrerequisites(prerequisites.hpad) then return end
	local entry = FindNearbySite(base, "hpad", yard.Location, nil)
	if entry ~= nil then StartBuilding(entry, yard) end
end

local function ProduceOrca(base)
	if MissionFinished or base.orcaBusy ~= nil then return end
	local pad = LocalBuilding(base, "hpad")
	if pad == nil or GDIOrcaCount() >= MaxOrcas[difficulty] then return end
	if not LocalTech(base, "orca") or not base.player.HasPrerequisites(prerequisites.orca)
		or GDIFunds(base.player) < Actor.Cost("orca") + 800 then return end

	local stamp = DateTime.GameTime
	base.orcaBusy = stamp
	if pad.Build({ "orca" }, function(units)
		if base.orcaBusy ~= stamp then return end
		base.orcaBusy = nil
		for _, orca in ipairs(units) do
			if IsLive(orca) and orca.Type == "orca" then
				GuardOrca(base, orca)
			end
		end
	end) then
		Trigger.AfterDelay(DateTime.Seconds(240), function()
			if base.orcaBusy == stamp then base.orcaBusy = nil end
		end)
	else
		base.orcaBusy = nil
	end
end

local function CheckGDIEconomy()
	if MissionFinished then return end
	for _, base in ipairs(bases) do
		if base.defending and DateTime.GameTime >= base.defenseUntil then base.defending = false end
		Produce(base)
		EnsureHelipad(base)
		ProduceOrca(base)
		RebuildHarvester(base)
		Rebuild(base)
	end
	Trigger.AfterDelay(DateTime.Seconds(10), CheckGDIEconomy)
end

-- Idle defenders engage nearby enemies instead of waiting for the first hit.
-- Units on the attack have Stance AttackAnything and are excluded here.
local function SouthDefense()
	if MissionFinished then return end
	for _, player in ipairs(gdiPlayers) do
		for _, unit in ipairs(player.GetGroundAttackers()) do
			if IsLive(unit) and unit.IsInWorld and unit.IsIdle and unit.HasProperty("AttackMove")
				and unit.HasProperty("Stance") and unit.Stance == "Defend" then
				local target = ClosestEnemy(unit, 16)
				if target ~= nil then unit.AttackMove(target.Location, 2) end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(3), SouthDefense)
end

-- atk1..atk7 from the original mission.
local waveUnits = {
	{ "e1", "e1", "e2", "e2" },
	{ "e2", "e2", "e3", "e3" },
	{ "e3", "e3", "e6", "e6" },
	{ "jeep", "jeep" },
	{ "mtnk", "msam" },
	{ "mtnk", "mtnk" },
	{ "htnk" },
}
local waveOffsets = { 0, 120, 240, 60, 300, 480, 600 }

local function SpawnGroupNear(waypoint, kinds, owner)
	owner = owner or AnyGDIPlayer()
	if owner == nil then return {} end
	local team = {}
	for _, kind in ipairs(kinds) do
		local cell = FindUnitSpot(waypoint.Location)
		if cell == nil then cell = waypoint.Location end
		local unit = Actor.Create(kind, true, { Owner = owner, Location = cell })
		if IsLive(unit) then
			Guard(unit)
			if unit.HasProperty("Stance") then unit.Stance = "AttackAnything" end
			table.insert(team, unit)
		end
	end
	return team
end

local function ScriptedWave(index)
	if MissionFinished then return end
	-- Original [TeamTypes]: atk1-atk5 and atk7 enter at WP2 on the southern
	-- shore and advance to WP4 before attacking. Only atk6 starts on the
	-- northern island (WP1). Spawning at WP4 put the waves on top of the
	-- player's second MCV.
	local spawn = index == 6 and waypoint1 or waypoint2
	local owner = AnyGDIPlayer()
	if owner == nil then return end
	local team = SpawnGroupNear(spawn, waveUnits[index], owner)
	SendTeam(team, owner, index == 6 and {} or { waypoint4 }, true)
end

local function AutoWave()
	if MissionFinished then return end
	if NodSouthPresence() then
		local sent = false
		for _, base in ipairs(bases) do
			local team = TakeForAttack(base, 3)
			if #team > 0 then
				SendTeam(team, base.player, {}, true)
				sent = true
			end
		end
		if not sent then
			-- Autocreate used the southern map edge, not the eastern
			-- mainland waypoint where the second MCV arrives.
			local owner = AnyGDIPlayer()
			if owner ~= nil then
				local team = SpawnGroupNear(waypoint2, { "jeep", "e1", "e1" }, owner)
				if #team > 0 then SendTeam(team, owner, {}, true) end
			end
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(AutoWaveInterval[difficulty]), AutoWave)
end

local function SendChinook(cargo, landing, paid, owner)
	if MissionFinished then return false end
	owner = owner or AnyGDIPlayer()
	if owner == nil then return false end
	local cost, paidResources = 0, 0
	if paid then
		cost = Actor.Cost("tran")
		for _, kind in ipairs(cargo or {}) do cost = cost + Actor.Cost(kind) end
		if GDIFunds(owner) < cost + 800 then return false end
		paidResources = SpendGDIFunds(owner, cost)
	end
	local entry = GDIAirEntry.Location
	local transport = Reinforcements.ReinforceWithTransport(owner, "tran.in", cargo,
		{ entry, landing.Location }, { entry })
	if transport == nil then
		if paid then
			owner.Resources = owner.Resources + paidResources
			owner.Cash = owner.Cash + cost - paidResources
		end
		return false
	end
	TrackGDIReinforcements(transport[2])
	local transportActor = transport[1]
	if IsLive(transportActor) then
		Trigger.OnPassengerExited(transportActor, function(_, passenger)
			Trigger.AfterDelay(1, function()
				if IsLive(passenger) and passenger.Owner == owner and passenger.IsInWorld then
					Guard(passenger)
					if passenger.HasProperty("Stance") then passenger.Stance = "AttackAnything" end
					SendTeam({ passenger }, owner, {}, true)
				end
			end)
		end)
	end
	return true
end

local function PaidAirlifts()
	if MissionFinished then return end
	local cargo = airliftIndex % 2 == 0 and { "e1", "e1", "e3", "e3" } or { "e2", "e2", "e6", "e6" }
	local landing = airliftIndex % 2 == 0 and waypoint0 or waypoint1
	local base = bases[airliftIndex % #bases + 1]
	airliftIndex = airliftIndex + 1
	if base ~= nil and not HouseEliminated(base.player) and LocalBuilding(base, "fact") ~= nil then
		SendChinook(cargo, landing, true, base.player)
	end
	Trigger.AfterDelay(DateTime.Seconds(AirliftInterval[difficulty]), PaidAirlifts)
end

-- bot1: an LST landing from the southern sea. The passengers disembark on the
-- shore north of the craft, move to the rally point and then attack.
local function SendLandingCraft(kinds, start, unload, rally)
	if MissionFinished then return false end
	local owner = AnyGDIPlayer()
	if owner == nil then return false end
	local transport = Actor.Create("lst", true, { Owner = owner, Facing = Angle.North, Location = start })
	if not IsLive(transport) then return false end

	local subcell = #kinds == 1 and 0 or 1
	local passengers = {}
	for _, kind in ipairs(kinds) do
		local passenger = Actor.Create(kind, false, { Owner = owner, Facing = transport.Facing,
			Location = unload, SubCell = subcell })
		if IsLive(passenger) then
			transport.LoadPassenger(passenger)
			table.insert(passengers, passenger)
			subcell = subcell + 1
		end
	end

	transport.ScriptedMove(unload)
	transport.CallFunc(function()
		local team = {}
		for _ = 1, #passengers do
			local passenger = transport.UnloadPassenger()
			if IsLive(passenger) then
				passenger.IsInWorld = true
				passenger.MoveIntoWorld(transport.Location - CVec.New(0, 1))
				if rally ~= nil then passenger.Move(rally) end
				Guard(passenger)
				table.insert(team, passenger)
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			SendTeam(team, owner, {}, true)
		end)
	end)
	transport.Wait(5)
	transport.ScriptedMove(start)
	transport.Destroy()
	return true
end

local function RandomNodTarget(minDistanceFromEye)
	local eye = nil
	if minDistanceFromEye ~= nil then
		if not IsLive(GDIEYE) then return nil end
		eye = GDIEYE
	end

	local targets = {}
	for _, actor in ipairs(Nod.GetGroundAttackers()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location") then
			local valid = true
			if eye ~= nil then
				local dx = actor.Location.X - eye.Location.X
				local dy = actor.Location.Y - eye.Location.Y
				valid = dx * dx + dy * dy > minDistanceFromEye * minDistanceFromEye
			end
			if valid then table.insert(targets, actor) end
		end
	end
	if #targets == 0 and minDistanceFromEye == nil then
		for _, actor in ipairs(Nod.GetActors()) do
			if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Health") and actor.HasProperty("Location")
				and not actor.HasProperty("Land") then
				table.insert(targets, actor)
			end
		end
	end
	if #targets > 0 then return Utils.Random(targets) end
	return nil
end

local function Airstrike()
	if MissionFinished or AirstrikeStopped then return end
	if not IsLive(GDIHQ) or not IsGDIPlayer(GDIHQ.Owner) or not GDIHQ.HasProperty("TargetAirstrike") then
		Trigger.AfterDelay(DateTime.Seconds(60), Airstrike)
		return
	end
	local target = RandomNodTarget(nil)
	if target ~= nil then GDIHQ.TargetAirstrike(target.CenterPosition) end
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeInterval[difficulty]), Airstrike)
end

local function IonCannonStrike()
	if MissionFinished or IonCannonStopped then return end
	if not IsLive(GDIEYE) or not IsGDIPlayer(GDIEYE.Owner) or not GDIEYE.HasProperty("ActivateIonCannon") then
		Trigger.AfterDelay(DateTime.Seconds(30), IonCannonStrike)
		return
	end
	local power = GDIEYE.Owner.PowerState
	if power == "Low" or power == "Critical" then
		Trigger.AfterDelay(DateTime.Seconds(60), IonCannonStrike)
		return
	end
	local target = RandomNodTarget(10)
	if target == nil then
		Trigger.AfterDelay(DateTime.Seconds(15), IonCannonStrike)
		return
	end
	GDIEYE.ActivateIonCannon(target.Location)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonInterval[difficulty]), IonCannonStrike)
end

-- chn3: Nod entering the north-western corner of its starting island
-- triggers another armed landing.
local NorthWestCells = {
	CPos.New(20, 4), CPos.New(21, 4), CPos.New(22, 4),
	CPos.New(20, 5), CPos.New(21, 5), CPos.New(22, 5),
	CPos.New(20, 6), CPos.New(21, 6), CPos.New(22, 6),
	CPos.New(20, 7), CPos.New(21, 7), CPos.New(22, 7),
}

-- bot1: Nod reaches the eastern shore.
local Bot1Cells = {
	CPos.New(55, 22), CPos.New(56, 22), CPos.New(57, 22),
	CPos.New(58, 22), CPos.New(59, 22), CPos.New(60, 22),
	CPos.New(55, 23), CPos.New(56, 23), CPos.New(57, 23),
	CPos.New(58, 23), CPos.New(59, 23), CPos.New(60, 23),
}

-- bot2: Nod reaches the central GDI outpost.
local Bot2Cells = {
	CPos.New(41, 28), CPos.New(42, 28), CPos.New(43, 28), CPos.New(44, 28), CPos.New(45, 28),
	CPos.New(41, 29), CPos.New(42, 29), CPos.New(43, 29), CPos.New(44, 29), CPos.New(45, 29),
}

NodSouthPresence = function()
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location") then
			if ActorRegion(actor) == SouthRegion then return true end
		end
	end
	return false
end

-- The reinforcement MCV is only replaced when the player has neither a
-- southern construction yard nor a southern MCV. A single infantry squad
-- south of the river must not consume the second MCV.
NodHasSouthernBase = function()
	for _, kind in ipairs({ "mcv", "fact" }) do
		for _, actor in ipairs(Nod.GetActorsByType(kind)) do
			if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location")
				and ActorRegion(actor) == SouthRegion then return true end
		end
	end
	return false
end

-- map.yaml already assigns the western actors to GDIWest. This split is kept
-- as a safety net (and for the Lua harness, whose actor list still starts as
-- GDI): x <= 24 belongs to the western base, everything east stays GDI. The
-- two starting harvesters follow the refinery they are actually working from,
-- so each base keeps one.
local function SplitGDI()
	local actors = GDI.GetActors()
	local procs = GDI.GetActorsByType("proc")
	for _, actor in ipairs(actors) do
		if IsLive(actor) and actor.IsInWorld and actor.HasProperty("Location") then
			local west = actor.Location.X <= 24
			if actor.Type == "harv" then
				local bestProc, bestDistance
				for _, proc in ipairs(procs) do
					local dx = actor.Location.X - proc.Location.X
					local dy = actor.Location.Y - proc.Location.Y
					local distance = dx * dx + dy * dy
					if bestProc == nil or distance < bestDistance then
						bestProc, bestDistance = proc, distance
					end
				end
				west = bestProc ~= nil and bestProc.Location.X <= 24
			end
			if west then actor.Owner = GDIWest end
		end
	end
end

SetupAI = function()
	difficulty = Difficulty or "normal"
	GDIWest = GDIWest or Player.GetPlayer("GDIWest")
	gdiPlayers = { GDIWest, GDI }
	local cash = math.floor(StartingCash[difficulty] / 2)
	GDI.Cash = cash
	GDIWest.Cash = cash
	BuildLandRegions()
	SouthRegion = Region(CPos.New(21, 34))

	bases = {
		{ player = GDIWest, yard = { 21, 34 }, reserve = {}, produced = {}, nextProduction = "infantry" },
		{ player = GDI, yard = { 58, 48 }, reserve = {}, produced = {}, nextProduction = "infantry" },
	}
	for _, base in ipairs(bases) do
		base.defending = false
		base.defenseUntil = 0
		base.reserveRelease = 0
		base.combatPending = nil
		base.orcaBusy = nil
		base.harvester = nil
		base.harvesterBuilding = false
		base.harvesterCooldown = 0
		base.reliefCooldown = 0
		base.mcv = nil
		base.mcvSite = nil
		base.nodeIndex = 1
		base.nodes = {}
		for _, entry in ipairs(nodes) do
			if (entry[2] <= 24) == (base == bases[1]) then table.insert(base.nodes, entry) end
		end
	end
	SplitGDI()

	for _, player in ipairs(gdiPlayers) do
		for _, harv in ipairs(player.GetActorsByType("harv")) do
			Guard(harv)
			local base = BaseForHarvester(harv)
			if base ~= nil then TrackHarvester(base, harv) end
			if harv.HasProperty("FindResources") then harv.FindResources() end
		end
	end

	for _, player in ipairs(gdiPlayers) do
		for _, actor in ipairs(player.GetActors()) do
			if IsLive(actor) and actor.IsInWorld then
				if actor.HasProperty("StartBuildingRepairs") then
					Guard(actor)
				elseif actor.Type == "orca" then
					GuardOrca(BaseForPlayer(actor.Owner), actor)
				elseif actor.HasProperty("AttackMove") then
					Guard(actor)
					local localToBase = false
					for _, base in ipairs(bases) do
						if base.player == actor.Owner and IsLocal(base, actor)
							and SameLand(actor.Location, CPos.New(base.yard[1], base.yard[2])) then
							localToBase = true
							break
						end
					end
					if actor.HasProperty("Stance") then
						actor.Stance = localToBase and "Defend" or "AttackAnything"
					end
					if not localToBase then IdleHunt(actor) end
				end
			end
		end
	end

	-- Key building losses are serious enough to call the other base for help.
	for _, base in ipairs(bases) do
		for _, actor in ipairs(base.player.GetActors()) do
			if IsLive(actor) and actor.IsInWorld and BaseReliefBuildings[actor.Type] then
				Trigger.OnKilled(actor, function() RequestRelief(base) end)
				Trigger.OnCapture(actor, function(_, _, _, newOwner)
					if not IsGDIPlayer(newOwner) then RequestRelief(base) end
				end)
			end
		end
	end

	Trigger.OnAnyProduction(OnProduced)

	for i, offset in ipairs(waveOffsets) do
		Trigger.AfterDelay(DateTime.Seconds(WaveStart[difficulty] + offset), function() ScriptedWave(i) end)
	end

	-- chn2: one empty transport at 0.4 minutes. chn4/chn5: two armed
	-- landings at 9.8 and 9.9 minutes.
	Trigger.AfterDelay(DateTime.Seconds(24), function() SendChinook(nil, waypoint0) end)
	Trigger.AfterDelay(DateTime.Seconds(FirstLanding[difficulty]), function()
		SendChinook({ "e1", "e1", "e2", "e2" }, waypoint0)
	end)
	Trigger.AfterDelay(DateTime.Seconds(FirstLanding[difficulty] + 6), function()
		SendChinook({ "e2", "e2", "e3", "e3", "e3" }, waypoint1)
	end)
	Trigger.OnEnteredFootprint(NorthWestCells, function(actor, id)
		if actor.Owner ~= Nod then return end
		Trigger.RemoveFootprintTrigger(id)
		SendChinook({ "e2", "e2", "e3", "e3", "e3" }, waypoint1)
	end)

	-- trup: 12 GDI infantry patrol the southern mainland from 0.3 minutes.
	Trigger.AfterDelay(DateTime.Seconds(18), function()
		local team = SpawnGroupNear(waypoint18, { "e1", "e1", "e1", "e2", "e2", "e2",
			"e3", "e3", "e3", "e6", "e6", "e6" })
		for _, unit in ipairs(team) do
			if unit.HasProperty("Patrol") and SameLand(unit.Location, waypoint19.Location) then
				if unit.HasProperty("Stance") then unit.Stance = "Defend" end
				unit.Patrol({ waypoint19.Location, waypoint18.Location }, true, DateTime.Seconds(12))
			else
				SendTeam({ unit }, unit.Owner, {}, true)
			end
		end
	end)

	-- bot1: an LST with 3x E2 and 2x E3 lands on the southern shore.
	Trigger.OnEnteredFootprint(Bot1Cells, function(actor, id)
		if actor.Owner ~= Nod then return end
		Trigger.RemoveFootprintTrigger(id)
		SendLandingCraft({ "e2", "e2", "e2", "e3", "e3" },
			CPos.New(61, 57), CPos.New(54, 57), waypoint2.Location)
	end)

	-- bot2: the original uses an LST with a single MSAM; the craft is
	-- replaced by a ground reinforcement entering from the eastern edge.
	Trigger.OnEnteredFootprint(Bot2Cells, function(actor, id)
		if actor.Owner ~= Nod then return end
		Trigger.RemoveFootprintTrigger(id)
		local owner = AnyGDIPlayer()
		if owner == nil then return end
		Reinforcements.Reinforce(owner, { "msam" }, { CPos.New(61, 29), waypoint3.Location }, 1,
			function(unit)
				if IsLive(unit) then
					Guard(unit)
					SendTeam({ unit }, owner, {}, true)
				end
			end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(AirliftFirstDelay[difficulty]), PaidAirlifts)
	Trigger.AfterDelay(DateTime.Seconds(AirstrikeFirstDelay[difficulty]), Airstrike)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[difficulty] - 60), function()
		if IonCannonStopped or not IsLive(GDIEYE) or not IsGDIPlayer(GDIEYE.Owner) then return end
		Media.PlaySpeechNotification(Nod, "IonCannonCharging")
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("ion-cannon-online"))
	end)
	Trigger.AfterDelay(DateTime.Seconds(IonCannonFirstDelay[difficulty]), IonCannonStrike)
	Trigger.AfterDelay(DateTime.Seconds(WaveStart[difficulty] + 300), AutoWave)
	CheckGDIEconomy()
	SouthDefense()
	OrcaWatch()
end