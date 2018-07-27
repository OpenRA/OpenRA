DifficultySetting = Map.LobbyOption("difficulty")
timeTracker = 0
amount = 1
SendAnts = true

AttackAngles = {
	{ waypoint4.Location, waypoint18.Location, waypoint5.Location, waypoint15.Location },
	{ waypoint20.Location, waypoint10.Location, waypoint2.Location },
	{ waypoint17.Location, waypoint1.Location },
	{ waypoint8.Location, waypoint9.Location, waypoint19.Location }
}

AttackInterval = {
	easy = DateTime.Seconds(40),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(20)
}

AntTypes = {
	"scoutant",
	"fireant"
}

MaxAnts = {
	easy = 3,
	normal = 5,
	hard = 7
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

	if antType == "warriorant" and DifficultySetting == "easy" then
		antType = "scoutant"
	end

	if DifficultySetting == "normal" and timeTracker < DateTime.Minutes(6) and antType == "scoutant" then
		antType = "warriorant"
	elseif DifficultySetting == "hard" and timeTracker < DateTime.Minutes(12) and antType == "scoutant" then
		antType = "warriorant"
	end

	local max = MaxAnts[DifficultySetting] - math.ceil(timeTracker / DateTime.Minutes(6))
	if timeTracker > DateTime.Minutes(3) and antType == "fireant" then
		amount = Utils.RandomInteger(1, MaxFireAnts[DifficultySetting])
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
		Trigger.AfterDelay(AttackInterval[DifficultySetting], function()
			StartAntAttack()
		end)
	end
end

EndAntAttack = function()
	SendAnts = false
end

InitEnemyPlayers = function()
	AntMan = Player.GetPlayer("AntMan")
end
