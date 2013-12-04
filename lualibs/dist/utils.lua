-- System functions

module ("dist.utils", package.seeall)

local sys = require "dist.sys"

-- Returns a deep copy of 'table' with reference to the same metadata table.
-- Source: http://lua-users.org/wiki/CopyTable
function deepcopy(object)
    local lookup_table = {}
    local function _copy(object)
        if type(object) ~= "table" then
            return object
        elseif lookup_table[object] then
            return lookup_table[object]
        end
        local new_table = {}
        lookup_table[object] = new_table
        for index, value in pairs(object) do
            new_table[_copy(index)] = _copy(value)
        end
        return setmetatable(new_table, getmetatable(object))
    end
    return _copy(object)
end

-- Return deep copy of table 'array', containing only items for which 'predicate_fn' returns true.
function filter(array, predicate_fn)
    assert(type(array) == "table", "utils.filter: Argument 'array' is not a table.")
    assert(type(predicate_fn) == "function", "utils.filter: Argument 'predicate_fn' is not a function.")
    local filtered = {}
    for _,v in pairs(array) do
        if predicate_fn(v) == true then table.insert(filtered, deepcopy(v)) end
    end
    return filtered
end

-- Return deep copy of table 'array', sorted according to the 'compare_fn' function.
function sort(array, compare_fn)
    assert(type(array) == "table", "utils.sort: Argument 'array' is not a table.")
    assert(type(compare_fn) == "function", "utils.sort: Argument 'compare_fn' is not a function.")
    local sorted = deepcopy(array)
    table.sort(sorted, compare_fn)
    return sorted
end

-- Return whether the 'value' is in the table 'tbl'.
function contains(tbl, value)
    assert(type(tbl) == "table", "utils.contains: Argument 'tbl' is not a table.")
    for _,v in pairs(tbl) do
        if v == value then return true end
    end
    return false
end

-- Return single line string consisting of values in 'tbl' separated by comma.
-- Used for printing the dependencies/provides/conflicts.
function table_tostring(tbl, label)
    assert(type(tbl) == "table", "utils.table_tostring: Argument 'tbl' is not a table.")
    local str = ""
    for k,v in pairs(tbl) do
        if type(v) == "table" then
            str = str .. table_tostring(v, k)
        else
            if label ~= nil then
                str = str .. tostring(v) .. " [" .. tostring(label) .. "]" .. ", "
            else
                str = str .. tostring(v) .. ", "
            end
        end
    end
    return str
end

-- Return table made up from values of the string, separated by separator.
function make_table(str, separator)
    assert(type(str) == "string", "utils.make_table: Argument 'str' is not a string.")
    assert(type(separator) == "string", "utils.make_table: Argument 'separator' is not a string.")

    local tbl = {}
    for val in str:gmatch("(.-)" .. separator) do
        table.insert(tbl, val)
    end
    local last_val = str:gsub(".-" .. separator, "")
    if last_val and last_val ~= "" then
        table.insert(tbl, last_val)
    end
    return tbl
end

-- Return whether the 'cache_timeout' for 'file' has expired.
function cache_timeout_expired(cache_timeout, file)
    assert(type(cache_timeout) == "number", "utils.cache_timeout_expired: Argument 'cache_timeout' is not a number.")
    assert(type(file) == "string", "utils.cache_timeout_expired: Argument 'file' is not a string.")
    return sys.last_modification_time(file) + cache_timeout < sys.current_time()
end

-- Return the string 'str', with all magic (pattern) characters escaped.
function escape_magic(str)
    assert(type(str) == "string", "utils.escape: Argument 'str' is not a string.")
    local escaped = str:gsub('[%-%.%+%[%]%(%)%^%%%?%*%^%$]','%%%1')
    return escaped
end

-- Return the boolean representation of an 'arg'.
function to_boolean(arg)
    return not not arg
end


math.randomseed(os.time())

-- Return pseudo-random number in range [0, 1], [1, n] or [n, m].
function rand(...)
    return math.random(...)
end

-- Perform check of system dependency, which isn't provided in the LuaDist
-- installation itself and if it is missing, print instructions how
-- to install it. The 'command' is used for testing, 'name' when printing
-- information to the user.
function system_dependency_available(name, command)
    assert(type(name) == "string", "utils.system_dependency_available: Argument 'name' is not a string.")
    assert(type(command) == "string", "utils.system_dependency_available: Argument 'command' is not a string.")

    if not sys.exec(command) then
        print("Error: command '" .. name .. "' not found on system. See installation instructions at\nhttps://github.com/LuaDist/Repository/wiki/Installation-of-System-Dependencies")
        return false
    end

    return true
end

-- Obtain LuaDist location by checking available package locations
function get_luadist_location()
  local paths = {}
  local path = package.path:gsub("([^;]+)", function(c) table.insert(paths, c) end)

  for _, path in pairs(paths) do
    if (sys.is_abs(path) and path:find("[/\\]lib[/\\]lua[/\\]%?.lua$")) then
      -- Remove path to lib/lua
      path = path:gsub("[/\\]lib[/\\]lua[/\\]%?.lua$", "")
      -- Clean the path up a bit
      path = path:gsub("[/\\]bin[/\\]%.[/\\]%.%.", "")
      path = path:gsub("[/\\]bin[/\\]%.%.", "")
      return path
    end
  end
  return nil
end

