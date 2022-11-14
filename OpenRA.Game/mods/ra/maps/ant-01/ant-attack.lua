--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
timeTracker = 0
amount = 1
SendAnts = true

AttackAngles = {
	{ waypoint4.Location, waypoint18.Location, waypoint5.Location, waypoint15.Location },
	{ waypoint20.Location, waypoint2.Location },
	{ waypoint21.Location, waypoint10.Location, waypoint2.Location },
	{ waypoint7.Location, waypoint17.Location, waypoint1.Location },
	{ waypoint8.Location, waypoint9.Location, waypoint19.Location }
}

AttackInterval = {
	easy = DateTime.Seconds(40),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(25)
}

AntTypes = {
	"scoutant",
	"fireant"
}

MaxAnts = {
	easy = 3,
	normal = 5,
	hard = 6
}

MaxFireAnts = {
	easy = 2,
	normal = 3,
	hard = 4
}

StartAntAttack = function()
	local path = Utils.Random(AttackAngles)
	local antType = "scoutant"
	local index = 0
	local amount = 1
	local timeTracker = GetTicks()

	if timeTracker > DateTime.Minutes(6) then
		antType = Utils.Random(AntTypes)
	end

	if antType == "warriorant" and Difficulty == "easy" then
		antType = "scoutant"
	end

	if Difficulty == "normal" and timeTracker < DateTime.Minutes(6) and antType == "scoutant" then
		antType = "warriorant"
	elseif Difficulty == "hard" and timeTracker < DateTime.Minutes(8) and antType == "scoutant" then
		antType = "warriorant"
	end

	local max = MaxAnts[Difficulty] - math.ceil(timeTracker / DateTime.Minutes(6))
	if timeTracker > DateTime.Minutes(3) and antType == "fireant" then
		amount = Utils.RandomInteger(1, MaxFireAnts[Difficulty])
	elseif timeTracker > 15 and antType == "fireant" then
		antType = "scoutant"
	else
		amount = Utils.RandomInteger(1, max)
	end

	for i = 0,amount,1 do
		Reinforcements.Reinforce(AntMan, { antType }, path, DateTime.Seconds(5), function(actor)
			actor.AttackMove(CPos.New(65, 65))
			Trigger.OnIdle(actor, function()
				actor.Hunt()
			end)
		end)
	end

	-- Setup next wave
	if SendAnts then
		Trigger.AfterDelay(AttackInterval[Difficulty], function()
			StartAntAttack()
		end)
	end
end

EndAntAttack = function()
	SendAnts = false
end
