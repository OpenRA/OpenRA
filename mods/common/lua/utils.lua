Utils = { }

Utils.Enumerate = function(netEnumerable)
	local enum = netEnumerable:GetEnumerator()
	return function()
		if enum:MoveNext() then
			return enum:get_Current()
		end
	end
end

Utils.EnumerableFirstOrNil = function(netEnumerable, func)
	for item in Utils.Enumerate(netEnumerable) do
		if func(item) then
			return item
		end
	end
	return nil
end

Utils.EnumerableWhere = function(netEnumerable, func)
	local ret = { }
	for item in Utils.Enumerate(netEnumerable) do
		if func(item) then
			table.insert(ret, item)
		end
	end
	return ret
end

Utils.Where = function(array, func)
	local ret = { }
	for i, item in ipairs(array) do
		if func(item) then
			table.insert(ret, item)
		end
	end
	return ret
end

Utils.All = function(array, func)
	for i, item in ipairs(array) do
		if not func(item) then
			return false
		end
	end
	return true
end

Utils.Any = function(array, func)
	for i, item in ipairs(array) do
		if func(item) then
			return true
		end
	end
	return false
end

Utils.Do = function(array, func)
	for i, item in ipairs(array) do
		func(item)
	end
end

Utils.Skip = function(array, n)
	local ret = { }
	for i, item in ipairs(array) do
		if i > n then
			table.insert(ret, item)
		end
	end
	return ret
end

Utils.TableToArray = function(luaTable)
	return Internal.TableToArray(luaTable)
end

Utils.Seconds = function(seconds)
	local TicksPerSecond = 25
	return seconds * TicksPerSecond
end

Utils.Minutes = function(minutes)
	return Utils.Seconds(minutes * 60)
end
