-- AI script
-- Current implementation is a proof-of-concept AI,
-- which will need much reworking by OpenRA contributors.

--
-- Global variable stuff
--
FACTION = nil
PLAYER = nil -- player object
PLAYER_NAME = nil -- internal name
TICKS = 0


--
-- Reserved constants
--

-- All power plants in Build orders must be written as ANYPOWER.
-- That's because we want AI to build the best power possible
-- after a lesser power had been destroyed.
-- In this implementation of build-order based build computation,
-- we count what the AI has and compare it against the build order
-- and build/rebuild when AI has not enough of a structure.
-- If we write powr, apwr in it, AI will constantly rebuild powr
-- even if it can build apwr.
ANYPOWER = "ANYPOWER"
HACKY_FALLBACK = "hacky_fallback"

UB_OPENER_STEP = 0 -- What ever the opener is, it needs some state. Save it here.
UB_OPENER_TICK = nil -- opener's tick function.
UB_ENABLED = false -- automatic unit building enabled? (false means opener in charge)



-- ALl Lua scripts are moddable to fit modder's needs.
-- However, things that definitely need modding are prefixed mod_.
-- What can be recycled for other mods aren't.
--
-- Given a list of things that AI has, count it.
--
UTIL_hist = function(stuff)
	local cnts = {}
	for _, v in ipairs(stuff) do
		if MOD_is_anypower(v) then
			v = ANYPOWER
		end

		if cnts[v] == nil then
			cnts[v] = 1
		else
			cnts[v] = cnts[v] + 1
		end
	end
	return cnts
end

-- Count unit types, owned by player
-- Not histogram like UTIL_hist.
-- Just count them and return integer.
UTIL_count_units = function(p, unit_types)
	local cnt = 0
	for _, name in ipairs(unit_types) do
		local units = p.GetActorsByType(name)
		if units ~= nil then
			cnt = cnt + #units
		end
	end
	return cnt
end

UTIL_is_enemy_with_AI = function(p)
	if p.InternalName == "Neutral" then
		return false
	end
	if p.InternalName == "Creeps" then
		return false
	end
	if p.InternalName == "Everyone" then
		return false
	end
	if p.InternalName == PLAYER.InternalName then
		return false
	end
	if p.IsAlliedWith(PLAYER) then
		return false
	end
	return true
end

UTIL_get_an_enemy_player = function()
	-- Currently we choose an enemy player by random.
	-- We may introduce policies in the future.

	local enemies = {}
	for _, p in ipairs(Player.GetPlayers(nil)) do
		if UTIL_is_enemy_with_AI(p) then
			table.insert(enemies, p)
		end
	end
	if #enemies == 0 then
		return nil
	else
		return Utils.Random(enemies)
	end
end

UTIL_get_enemy_units_by_type = function(name)
	local enemy = UTIL_get_an_enemy_player()
	if enemy == nil then
		return nil
	end
	--Media.DisplayMessage(enemy.Name .. " is my enemy", "lua_ai")

	local units = enemy.GetActorsByType(name)
	if units == nil then
		Media.DisplayMessage("no " .. name .. " found for enemy (1)")
		return nil
	elseif #units == 0 then
		Media.DisplayMessage("no " .. name .. " found for enemy (2)")
		return nil
	end
	return units
end

UTIL_get_least_protected = function(actors)
	local best_cnt = 999 -- big enough val
	local best_a = nil
	local stuff = nil
	for _, a in ipairs(actors) do
		stuff = Map.ActorsInCircle(a.CenterPosition, WDist.New(8 * 1024))
		-- I'n not caring if the object is mine or not
		-- Doesn't matter. If it is surrounded my units then it is as good as dead
		-- so it makes sense to seek another target.
		if #stuff < best_cnt then
			best_a = a
			best_cnt = #stuff
		end
	end
	--Media.DisplayMessage("least protected OK", "lua_ai")
	return best_a
end

UTIL_count_alive = function(actors)
	local alive = 0
	for _, a in ipairs(actors) do
		if not a.IsDead then
			alive = alive + 1
		end
	end
	return alive
end

UTIL_wait_load = function(transport, passengers, next_task, params_next_task)
	if transport.IsDead then
		TASK_unoccupy(passengers)
	end

	if transport.PassengerCount >= UTIL_count_alive(passengers) then
		next_task(params_next_task)
	else
		Trigger.AfterDelay(30, function()
			-- invoke self again to implement "wait load".
			UTIL_wait_load(transport, passengers, next_task, params_next_task)
		end)
	end
end

UTIL_load_passengers = function(transport, passengers, next_task, params_next_task)
	-- Order just once, or else it will bug.
	for _, a in ipairs(passengers) do
		a.Wait(45) -- wait until transport is out of the factory.
		-- If we don't wait then infantries will move around the factory.
		a.EnterTransport(transport)
	end
	UTIL_wait_load(transport, passengers, next_task, params_next_task)
end

MOD_is_anypower = function(name)
	if name == "powr" then
		return true
	elseif name == "apwr" then
		return true
	end
	return false
end

--
-- Given faction, choose a build order.
-- Currently, random.
--
MOD_choose_build_order = function(faction)
	local build_order = nil

	if MOD_is_soviet(faction) then
		build_order = BO_SOVIET_BOS
	elseif MOD_is_allies(faction) then
		build_order = BO_ALLIES_BOS
	else
		-- some mods may be under development.
		-- Let modders to fall back to hacky behavior.
		BUILD_ORDER = nil
		return
	end

	BUILD_ORDER = Utils.Random(build_order)
	--BUILD_ORDER = BO_SOVIET_FLAME_OPEN -- lets watch

	-- select opener so it may be ticked.
	UB_OPENER_TICK = Utils.Random(BUILD_ORDER.openers)
end

MOD_is_soviet = function(faction_name)
	if faction_name == "soviet" or faction_name == "ukraine" or faction_name == "russia" then
		return true
	else
		return false
	end
end

MOD_is_allies = function(faction_name)
	if faction_name == "allies" or faction_name == "england" or faction_name == "germany" or faction_name == "france" then
		return true
	else
		return false
	end
end

TASKFORCES =
{
	-- common stuff
	["mcv"] = {"mcv"},
	["harv"] = {"harv"},
	["e6"] = {"e6"},

	-- Allied taskforces.
	-- These are basic units of production. They may be merged into a big team.
	["6_heli"] = {"heli", "heli", "heli", "heli", "heli", "heli"},
	["2_hind"] = {"hind", "hind"},
	["10_e1"] = {"e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1"},
	["5_e1"] = {"e1","e1","e1","e1","e1"},
	["5_e3"] = {"e3", "e3", "e3", "e3", "e3"},
	["2_jeep"] = {"jeep", "jeep"},
	["4_1tnk"] = {"1tnk", "1tnk", "1tnk", "1tnk"},
	["4_2tnk"] = {"2tnk", "2tnk", "2tnk", "2tnk"},
	["arty"] = {"arty", "arty", "2tnk", "2tnk"},

	-- Soviet taskforces
	["2_e2"] = {"e2", "e2"},
	["4_dog"] = {"dog", "dog", "dog", "dog"},
	["2_e4"] = {"e4", "e4"},
	["2_shok"] = {"shok", "shok"},
	["2_ftrk"] = {"ftrk", "ftrk"},
	["4_3tnk"] = {"3tnk", "3tnk", "3tnk", "3tnk"},
	["2_4tnk"] = {"4tnk", "4tnk"},
	["ttnk"] = {"ttnk"},
	["v2rl"] = {"v2rl", "v2rl", "3tnk", "3tnk"},
	["4_mig"] = {"mig", "mig", "mig", "mig"},
	["2_yak"] = {"yak", "yak"},

	-- Taskforces that need special actions
	["tany1"] = {"e7", "jeep"}, -- ride and bomb
	["tany2"] = {"e7", "stnk"}, -- STEALTH yeah
	["apc_hijacker"] = {"apc", "hijacker"},
	["flame_opener"] = {"apc", "e4", "e4"}
}
task_flame_rush_part2 = function(params)
	local apc = params[1]
	local actors = params[2]
	local target = params[3]

	for _, a in ipairs(actors) do
		Trigger.OnAddedToWorld(a, function()
			--Media.DisplayMessage("Hello World!", "lua_ai")
			a.Stance = "AttackAnything"
			if not target.IsDead then
				a.Attack(target)
			end
			a.AttackMove(target.Location)
			a.Hunt()
		end)
	end

	apc.Stance = "HoldFire" -- quick move to target until unload
	apc.Move(target.Location, 3) -- move to a target
	apc.UnloadPassengers() -- unloada
	Trigger.OnIdle(apc, function() -- upon arrival and unloading,
		--Media.DisplayMessage("end of the line!", "lua_ai")
		apc.Stance = "AttackAnything"
	end)
end

task_flame_rush = function(actors)
	--Media.DisplayMessage("built flamer team", "lua_ai")

	TASK_occupy(actors) -- set occupied tag so that hacky part of the AI can't control.

	-- Now, lets find this APC to ride.
	local apc = nil
	local passengers = {}
	for _, a in ipairs(actors) do
		if a.Type == "apc" then
			apc = a
		else
			table.insert(passengers, a)
		end
	end

	-- Now lets find a suitable target.
	local enemy_stuff = UTIL_get_enemy_units_by_type("proc")
	if enemy_stuff == nil then
		Media.DisplayMessage("no proc", "lua_ai")
		enemy_stuff = UTIL_get_enemy_units_by_type("weap")
	end
	if enemy_stuff == nil then
		Media.DisplayMessage("no weap", "lua_ai")
		enemy_stuff = UTIL_get_enemy_units_by_type("powr")
	end
	if enemy_stuff == nil then -- nothing to target.
		-- Then let these hang around the base and used as an ordinary attack force.
		Media.DisplayMessage("nothing power even. no target", "lua_ai")
		TASK_unoccupy(actors) -- set occupied tag so that hacky part of the AI can't control.
		return
	end

	local target = UTIL_get_least_protected(enemy_stuff)
	--Media.DisplayMessage("target set", "lua_ai")
	local params = {apc, actors, target}
	UTIL_load_passengers(apc, passengers, task_flame_rush_part2, params)
end

TASK_occupy = function(actors)
	for _, a in ipairs(actors) do
		-- occupied by the trigger :)
		a.HackyAIOccupied = true
	end
end

TASK_unoccupy = function(actors)
	for _, a in ipairs(actors) do
		a.HackyAIOccupied = false
	end
end

team_mcv =
{
	tf = "mcv",
	trigger = nil
}

team_harv =
{
	tf = "harv",
	trigger = nil
}

team_e6 =
{
	tf = "e6",
	trigger = nil
}

team_6_heli =
{
	tf = "6_heli",
	trigger = nil
}

team_2_hind =
{
	tf = "2_hind",
	trigger = nil
}

team_10_e1 = 
{
	tf = "10_e1",
	trigger = nil
}

team_5_e1 =
{
	tf = "5_e1",
	trigger = nil
}

team_5_e3 =
{
	tf = "5_e3",
	trigger = nil
}

team_tany1 =
{
	tf = "tany1",
	trigger = nil
}

team_tany2 =
{
	tf = "tany2",
	trigger = nil
}

team_2_jeep =
{
	 tf = "2_jeep",
	trigger = nil
}

team_4_1tnk =
{
	tf = "4_1tnk",
	trigger = nil
}

team_4_2tnk =
{
	tf = "4_2tnk",
	trigger = nil
}

team_arty =
{
	tf = "arty",
	trigger = nil
}

team_2_e2 =
{
	tf = "2_e2",
	trigger = nil
}

team_4_dog =
{
	tf = "4_dog",
	trigger = nil
}

team_2_e4 =
{
	tf = "2_e4",
	trigger = nil
}

team_2_shok =
{
	tf = "2_shok",
	trigger = nil
}

team_apc_hijacker =
{
	tf = "apc_hijacker",
	trigger = nil
}

team_2_ftrk =
{
	tf = "2_ftrk",
	trigger = nil
}

team_4_3tnk =
{
	tf = "4_3tnk",
	trigger = nil
}

team_2_4tnk =
{
	tf = "2_4tnk",
	trigger = nil
}

team_ttnk =
{
	tf = "ttnk",
	trigger = nil
}

team_v2rl =
{
	tf = "v2rl",
	trigger = nil
}

team_4_mig =
{
	tf = "4_mig",
	trigger = nil
}

team_2_yak =
{
	tf = "2_yak",
	trigger = nil
}

team_flame_opener =
{
	tf = "flame_opener",
	trigger = task_flame_rush
}

-- Teams that may be use for auto production
TEAMS =
{
	-- Allied teams.
	-- These are basic units of production. They may be merged into a big team.
	["6_heli"] = team_6_heli,
	["2_hind"] = team_2_hind,
	["10_e1"] = team_10_e1,
	["5_e1"] = team_5_e1,
	["5_e3"] = team_5_e3,
	["2_jeep"] = team_2_jeep,
	["4_1tnk"] = team_4_1tnk,
	["4_2tnk"] = team_4_2tnk,
	["arty"] = team_arty,

	-- Soviet teams
	["2_e2"] = team_2_e2,
	["4_dog"] = team_4_dog,
	["2_e4"] = team_2_e4,
	["2_shok"] = team_2_shok,
	["2_ftrk"] = team_2_ftrk,
	["4_3tnk"] = team_4_3tnk,
	["2_4tnk"] = team_2_4tnk,
	["ttnk"] = team_ttnk,
	["v2rl"] = team_v2rl,
	["4_mig"] = team_4_mig,
	["2_yak"] = team_2_yak
}

SPECIAL_TEAMS =
{
	["flame_opener"] = team_flame_opener,
	["mcv"] = team_mcv,
	["harv"] = team_harv,
	["e6"] = team_e6,
	["apc_hijacker"] = team_apc_hijacker,
	["tany1"] = team_tany1,
	["tany2"] = team_tany2
}

TEAM_KEYS = {}

-- key isn't exactly a pair. WTF were Lua devs thinking?
-- Lua sucks
-- Pre-computation for random selection.
for key in pairs(TEAMS) do
	table.insert(TEAM_KEYS, key)
end


--
-- get one of the teams. Just randomly.
--
TEAM_get_random_team = function()
	local key = Utils.Random(TEAM_KEYS)
	return TEAMS[key]
end

TEAM_get_random_combat_team = function()
	local key = Utils.Random(TEAM_KEYS)
	return TEAMS[key]
end

TEAM_build_team = function(team)
	return PLAYER.Build(TASKFORCES[team.tf], team.trigger)
end
-- opener should come before build order.
--
-- Openers are a tick functions that controls the unit production
-- until automatic unit production is enabled.
--

--
-- If your mod is incomplete, use this opener.
--
null_opener = function()
	UB_ENABLED = true
end

op_single_proc_opener = function()
	-- When bot builds barracks or tent, queue 5 e1s to defend.
	if UB_OPENER_STEP == 0 then
		if PLAYER.HasPrerequisites({"kenn"}) then
			-- doggy opener
			if TEAM_build_team(TEAMS["4_dog"]) then
				UB_OPENER_STEP = 1
			end
		elseif PLAYER.HasPrerequisites({"barr"}) or PLAYER.HasPrerequisites({"tent"}) then
			if TEAM_build_team(TEAMS["5_e1"]) then
				UB_OPENER_STEP = 1
			end
		end
		-- else, stay in this state.
	elseif UB_OPENER_STEP == 1 then
		-- Build 5 harvesters.
		local cnt = 3
		if PLAYER.HasPrerequisites({"proc", "weap"}) then
			local harvs = PLAYER.GetActorsByType("harv")
			if #harvs >= cnt then
				UB_OPENER_STEP = 2
			elseif #harvs < cnt and not PLAYER.IsProducing("harv") then
				TEAM_build_team(SPECIAL_TEAMS["harv"])
			end
		end
	else
		UB_ENABLED = true
	end
end

--
-- Defensive, eco oriented opening.
--
op_multiproc_opener = function()
	-- When bot builds barracks or tent, queue 5 e1s to defend.
	if UB_OPENER_STEP == 0 then
		if PLAYER.HasPrerequisites({"kenn"}) then
			-- doggy opener
			if TEAM_build_team(TEAMS["4_dog"]) then
				UB_OPENER_STEP = 1
			end
		elseif PLAYER.HasPrerequisites({"barr"}) or PLAYER.HasPrerequisites({"tent"}) then
			if TEAM_build_team(TEAMS["10_e1"]) then
				UB_OPENER_STEP = 1
			end
		end
		-- else, stay in this state.
	elseif UB_OPENER_STEP == 1 then
		-- when multi proc it is ok to build freely.
		if #PLAYER.GetActorsByType("proc") >= 2 then
			UB_OPENER_STEP = 2
		end
	else
		UB_ENABLED = true
	end
end

--
-- Soviet opener with APC+flame thrower
--
op_flame_opener = function()
	-- When bot builds barracks or tent, queue 5 e1s to defend.
	if UB_OPENER_STEP == 0 then
		if PLAYER.HasPrerequisites({"barr"}) then
			if TEAM_build_team(TEAMS["5_e1"]) then
				UB_OPENER_STEP = 1
			end
		end
		-- else, stay in this state.
	elseif UB_OPENER_STEP == 1 then
		if PLAYER.HasPrerequisites({"barr", "ftur", "weap"}) then
			--Media.DisplayMessage("Building flamer team", "lua_ai")
			if TEAM_build_team(SPECIAL_TEAMS["flame_opener"]) then -- returns false when you cant build
				UB_OPENER_STEP = 2
			end
		end
	elseif UB_OPENER_STEP == 2 then
		-- Build 3 harvesters.
		local cnt = 3
		if PLAYER.HasPrerequisites({"proc", "weap"}) then
			local harvs = PLAYER.GetActorsByType("harv")
			if #harvs >= cnt then
				UB_OPENER_STEP = 3
			elseif #harvs < cnt and not PLAYER.IsProducing("harv") then
				TEAM_build_team(SPECIAL_TEAMS["harv"])
			end
		end
	else
		UB_ENABLED = true
	end
end
--
-- Allies build order definitions
--

-- omit powers, except for the first one.
-- which is required for tech.
BO_ALLIES_NORMAL = {
	name = "allies_normal",
	openers = {op_multiproc_opener},
	bo = {
		ANYPOWER,
		"tent",
		"proc",
		"proc",
		ANYPOWER,
		"weap",
		"fix",
		"dome",
		ANYPOWER,
		"proc",
		"hpad",
		"hpad",
		"atek",
		ANYPOWER
	}
}

BO_ALLIES_FAST_WEAP = {
	name = "allies_weap",
	openers = {op_single_proc_opener},
	bo = {
		ANYPOWER,
		"tent",
		"proc",
		"weap",
		ANYPOWER,
		"proc",
		"fix",
		"proc",
		"dome",
		ANYPOWER,
		"atek",
		ANYPOWER
	}
}

BO_ALLIES_FAST_AIR = {
	name = "allies_air",
	openers = {op_single_proc_opener},
	bo = {
		ANYPOWER,
		"tent",
		"proc",
		"proc",
		"dome",
		"hpad",
		ANYPOWER,
		"proc",
		"weap",
		"atek",
		ANYPOWER,
		"hpad",
		"hpad",
		"hpad",
		"proc",
		"fix"
	}
}

BO_ALLIES_ECO = {
	name = "allies_eco",
	openers = {op_multiproc_opener},
	bo = {
		ANYPOWER,
		"tent",
		"proc",
		"proc",
		ANYPOWER,
		"proc",
		"weap",
		"fix",
		ANYPOWER,
		"dome",
		ANYPOWER,
		"atek"
	}
}

BO_ALLIES_BOS = {BO_ALLIES_NORMAL, BO_ALLIES_ECO, BO_ALLIES_FAST_WEAP, BO_ALLIES_FAST_AIR}

--
-- Soviet build order definitions
--

-- omit powers, except for the first one.
-- which is required for tech.
BO_SOVIET_NORMAL = {
	name = "soviet_normal",
	openers = {op_multiproc_opener},
	bo = {
		ANYPOWER,
		"barr",
		"kenn",
		"proc",
		"proc",
		ANYPOWER,
		"weap",
		"fix",
		"dome",
		ANYPOWER,
		"proc",
		"afld",
		"afld",
		"stek",
		ANYPOWER
	}
}

BO_SOVIET_FAST_WEAP = {
	name = "soviet_weap",
	openers = {op_single_proc_opener},
	bo = {
		ANYPOWER,
		"kenn",
		"proc",
		"weap",
		ANYPOWER,
		"barr",
		"proc",
		"fix",
		"proc",
		"dome",
		ANYPOWER,
		"stek",
		ANYPOWER
	}
}

BO_SOVIET_FLAME_OPEN = {
	name = "soviet_flame",
	openers = {op_flame_opener},
	bo = {
		ANYPOWER,
		"proc",
		"barr", -- proc first for slightly more income
		"weap",
		ANYPOWER,
		"proc",
		"fix",
		"proc",
		"kenn",
		"dome",
		ANYPOWER,
		"stek",
		ANYPOWER
	}
}

BO_SOVIET_FAST_AIR = {
	name = "soviet_air",
	openers = {op_single_proc_opener},
	bo = {
		ANYPOWER,
		"barr",
		"proc",
		"proc",
		"dome",
		"afld",
		ANYPOWER,
		"proc",
		"weap",
		"stek",
		ANYPOWER,
		"afld",
		"afld",
		"afld",
		"fix"
	}
}

BO_SOVIET_ECO = {
	name = "soviet_eco",
	openers = {op_multiproc_opener},
	bo = {
		ANYPOWER,
		"kenn",
		"barr",
		"proc",
		"proc",
		ANYPOWER,
		"proc",
		"weap",
		"fix",
		ANYPOWER,
		"dome",
		ANYPOWER,
		"stek"
	}
}

BO_SOVIET_BOS = {
	BO_SOVIET_NORMAL, BO_SOVIET_ECO, BO_SOVIET_FAST_WEAP,
	BO_SOVIET_FAST_AIR, BO_SOVIET_FLAME_OPEN
}

BUILD_ORDER = nil -- current build order the AI is on.

-- Examine what we have and the build order to see if anything is missing.
-- If multiple things are missing, return the one that
-- occurs early in build_order.
-- Returns nil when everything in BO is built.
BB_get_unbuilt = function(build_order, building_count)
	local cnts = {}
	for _, name in ipairs(build_order) do
		if building_count[name] == nil then
			return name
		else
			-- count upto this point.
			-- now we know how many buildings we should have
			-- when BO went well.
			if cnts[name] == nil then
				cnts[name] = 1
			else
				cnts[name] = cnts[name] + 1
			end

			-- if we have less buildings than BO orders us to have, then build one.
			if building_count[name] < cnts[name] then
				return name
			end
		end
	end
	return nil
end

-- Called by ChooseBuildingToBuild in BaseBuilder.cs
-- Thinking process exported to Lua.
-- Input: tab: table containing all the parameters from BaseBuilder.cs
-- Returns: a rules name of structures to build.
--		Modders may return nil on purpose to choose not to build anything.
BB_choose_building_to_build = function(tab)
	if BUILD_ORDER == nil then
		Media.DisplayMessage("No build order for " .. FACTION, "BB Warning")
		return HACKY_FALLBACK
	end

	tab = tab[1] -- Sandbox quirks
	local building_count = UTIL_hist(tab["player_buildings"])

	-- Base defenses aren't current focus now.
	-- Fall back to hacky behavior.
	if tab["queue_type"] == "defense" then
		return HACKY_FALLBACK
	end

	-- if tab["excess_power"] < 0 then -- just check for low power.
	-- Build orders are fine but getting into low power is too bad.
	-- Since hacky behavior is building defenses, we do need excess power, as of now.
	if tab["excess_power"] < tab["minimum_excess_power"] then
		-- Build power, as we don't have enough excess power.
		if tab["power_gen"] > 0 then
			--Media.DisplayMessage("Low power. Building power.", "BB")
			return tab["power"]
		end
	end

	local unbuilt = BB_get_unbuilt(BUILD_ORDER.bo, building_count)

	if unbuilt == nil then
		-- If everything is built as BO, fall back to hacky AI builder.
		return HACKY_FALLBACK
	elseif unbuilt == ANYPOWER then
		return tab["power"]
	end
	return unbuilt -- Build, as in bulid order.
end
--
-- Tick unit builder logic
--
UB_tick = function()
	if not UB_ENABLED then
		-- UB_OPENER_TICK is selected by MOD_choose_build_order() at ActivateAI phase.
		if UB_OPENER_TICK ~= nil then
			UB_OPENER_TICK()
		else
			Media.DisplayMessage("warning: no auto production nor opener", "lua_ai")
		end
		return
	end

	--Media.DisplayMessage("ub_tick()", "lua_ai")

	-- MCV rebuilding logic
	local facts = PLAYER.GetActorsByType("fact")
	if #facts == 0 then
		local weaps = PLAYER.GetActorsByType("weap")
		local fixes = PLAYER.GetActorsByType("fix")
		local mcvs = PLAYER.GetActorsByType("mcv")
		if #weaps > 0 and #fixes > 0 and #mcvs == 0 then
			TEAM_build_team(SPECIAL_TEAMS["mcv"])
			return
		end
	end

	-- Count harvesters and make one if needed.
	local harvs = PLAYER.GetActorsByType("harv")
	local ateks = PLAYER.GetActorsByType("atek")
	local steks = PLAYER.GetActorsByType("stek")
	if #steks + #ateks > 0 and #harvs < 7 then
		-- Only build by 0.25 chance.
		-- Why? It is not an urgent problem and the AI may build other units
		if Utils.RandomInteger(0, 4) == 0 then
			TEAM_build_team(SPECIAL_TEAMS["harv"])
			return
		end
	elseif #harvs < 5 then
		TEAM_build_team(SPECIAL_TEAMS["harv"])
		return
	end
	
	-- Count enemy defenses and make siege units.
	local enemy = UTIL_get_an_enemy_player()
	local defense_cnt = UTIL_count_units(enemy, {"pbox", "hbox", "gun", "ftur", "tsla"})
	if defense_cnt >= 5 then
		if MOD_is_allies(PLAYER_NAME) and #PLAYER.GetActorsByType("arty") < 4 then
			if TEAM_build_team(TEAMS["arty"]) then
				-- return when only on true.
				-- arty still needs prerequisites such as DOME.
				return
			end
		elseif MOD_is_soviet(PLAYER_NAME) and #PLAYER.GetActorsByType("v2rl") < 4 then
			if TEAM_build_team(TEAMS["v2rl"]) then
				return
			end
		end
	end

	-- Just create a random team.
	local team = TEAM_get_random_combat_team()
	if team ~= nil then -- can't be nil if there are teams.
		-- don't build 1tnks when FIX is built.
		if PLAYER.HasPrerequisites({"fix"}) and team.tf == "4_1tnk" then
			return
		end

		--Media.DisplayMessage("TF BUILD START", "lua_ai")
		TEAM_build_team(team)
		-- returns false when something is in production.
		-- Don't worry about queue getting stacked too much.
	end
end


--
-- Important functions, which are called from the Engine.
--

--
-- Initialize stuff.
-- Called by Activate() in HackyAI.
--
ActivateAI = function(params)
	-- First, I have to know, what I'm playing.
	--Trigger.AfterDelay(DateTime.Seconds(1), Periodic)
	--test(faction)
	FACTION = string.lower(params[1]) -- faction of the bot player
	PLAYER_NAME = params[2] -- internal name of the bot player. dont lower case this.
	PLAYER = Player.GetPlayer(PLAYER_NAME)
	MOD_choose_build_order(FACTION) -- and its attached opener too.
end

--
-- Tick the AI thinking. Called by Tick() in Scripted AI.
--
Tick = function()
	TICKS = TICKS + 1

	-- once in a second or so.
	if TICKS % 32 == 0 then
		UB_tick() -- unit builder tick
	end
end
