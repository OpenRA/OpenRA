
BankBalance = function(Player)
	return Player.Resources + Player.Cash
end

BankDeduct = function(Player, cost)
	if cost > BankBalance(Player) then
		Media.Debug(tostring(Player) .. ' cannot afford $' .. cost)
	end
	local spendRes = math.min(cost, Player.Resources)
	Player.Resources = Player.Resources - spendRes
	local spendCash = math.max(0, cost - spendRes)
	Player.Cash = Player.Cash - spendCash
end
