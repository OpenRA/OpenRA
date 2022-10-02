--Get players
HumanPlayers = {};
--get building
nukeLab = { "nukelab" }


function GetAllPlayers()
    return Player.GetPlayers(function(player)
        return player.IsNonCombatant == false;
    end);
end

function GetHumans()
    return Player.GetPlayers(function(player)
        return player.IsBot == false and player.IsNonCombatant == false;
    end);
end

Trigger.AfterDelay(DateTime.Seconds(22), function()
    for _, humanPlayer in ipairs(HumanPlayers) do
        for _, player in ipairs(AIPlayers) do
            Radar.Ping(humanPlayer, Map.CenterOfCell(player.HomeLocation), HSLColor.Red, 500)
            Beacon.New(humanPlayer, Map.CenterOfCell(player.HomeLocation), 500)
        end
    end
    Media.PlaySoundNotification(USSR, "Beacon");
end)

function WorldLoaded()
    Neutral = Player.GetPlayer("Neutral");
    Creeps = Player.GetPlayer("Creeps");

    -- Initialize AI & Human Players
    AllPlayersIncludingCreeps = GetAllPlayersIncludingCreeps();
    AllPlayers = GetAllPlayers();
    AIPlayers = GetBots();
    HumanPlayers = GetHumans();
end