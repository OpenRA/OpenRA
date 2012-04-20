local T = {} -- types

-- istype[o] iff o represents a type (i.e. set of values)
T.istype = {}

-- iserror[o] iff o represents an error type (created via T.error).
T.iserror = {}

-- istabletype[o] iff o represents a table type (created by T.table).
T.istabletype = {}

-- Number type
T.number = {}
setmetatable(T.number, T.number)
function T.number.__tostring(self)
  return 'number'
end
T.istype[T.number] = true

-- String type
T.string = {}
setmetatable(T.string, T.string)
function T.string.__tostring(self)
  return 'string'
end
T.istype[T.string] = true

-- Boolean type
T.boolean = {}
setmetatable(T.boolean, T.boolean)
function T.boolean.__tostring(self)
  return 'boolean'
end
T.istype[T.boolean] = true

-- Table type
function T.table(t)
  T.istype[t] = true
  T.istabletype[t] = true
  return t
end

-- Universal type.  This is a superset of all other types.
T.universal = {}
setmetatable(T.universal, T.universal)
function T.universal.__tostring(self)
  return 'unknown'
end
T.istype[T.universal] = true

-- nil type.  Represents `nil` but can be stored in tables.
T['nil'] = {}
setmetatable(T['nil'], T['nil'])
T['nil'].__tostring = function(self)
  return 'nil'
end
T.istype[T['nil']] = true

-- None type.  Represents a non-existent value, in a similar way
-- that `none` is used differently from `nil` in the Lua C API.
T.none = {}
setmetatable(T.none, T.none)
function T.none.__tostring(self)
  return 'none'
end
T.istype[T.none] = true

-- Error type
local CError = {}; CError.__index = CError
function CError.__tostring(self) return "error:" .. tostring(self.value) end
function T.error(val)
  local self = setmetatable({value=val}, CError)
  T.istype[self] = true
  T.iserror[self] = true
  return self
end


-- Gets a type that is a superset of the two given types.
function T.superset_types(a, b)
  if T.iserror[a] then return a end
  if T.iserror[b] then return b end
  if rawequal(a, b) then -- note: including nil == nil
    return a
  elseif type(a) == 'string' or a == T.string then
    if type(b) == 'string' or b == T.string then
      return T.string
    else
      return T.universal
    end
  elseif type(a) == 'number' or a == T.number then
    if type(b) == 'number' or b == T.number then
      return T.number
    else
      return T.universal
    end
  elseif type(a) == 'boolean' or a == T.boolean then
    if type(b) == 'boolean' or b == T.boolean then
      return T.boolean
    else
      return T.universal
    end
  else
    return T.universal -- IMPROVE
  end
end
--[[TESTS:
assert(T.superset_types(2, 2) == 2)
assert(T.superset_types(2, 3) == T.number)
assert(T.superset_types(2, T.number) == T.number)
assert(T.superset_types(T.number, T.string) == T.universal)
print 'DONE'
--]]

-- Determines whether type `o` certainly evaluates to true (true),
-- certainly evaluates to false (false) or could evaluate to either
-- true of false ('?').
function T.boolean_cast(o)
  if T.iserror[o] then -- special case
    return '?'
  elseif o == nil or o == false or o == T['nil'] then -- all subsets of {nil, false}
    return false
  elseif o == T.universal or o == T.boolean then -- all supersets of boolean
    return '?'
  else -- all subsets of  universal - {nil, false}
    return true
  end
end

return T
