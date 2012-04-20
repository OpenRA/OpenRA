-- Recursive object dumper, for debugging.
-- (c) 2010 David Manura, MIT License.

local M = {}

-- My own object dumper.
-- Intended for debugging, not serialization, with compact formatting.
-- Robust against recursion.
-- Renders Metalua table tag fields specially {tag=X, ...} --> "`X{...}".
-- On first call, only pass parameter o.
-- CATEGORY: AST debug
local ignore_keys_ = {lineinfo=true}
local norecurse_keys_ = {parent=true, ast=true}
local function dumpstring_key_(k, isseen, newindent)
  local ks = type(k) == 'string' and k:match'^[%a_][%w_]*$' and k or
             '[' .. M.dumpstring(k, isseen, newindent) .. ']'
  return ks
end
local function sort_keys_(a, b)
  if type(a) == 'number' and type(b) == 'number' then
    return a < b
  elseif type(a) == 'number' then
    return false
  elseif type(b) == 'number' then
    return true
  elseif type(a) == 'string' and type(b) == 'string' then
    return a < b
  else
    return tostring(a) < tostring(b) -- arbitrary
  end
end
function M.dumpstring(o, isseen, indent, key)
  isseen = isseen or {}
  indent = indent or ''

  if type(o) == 'table' then
    if isseen[o] or norecurse_keys_[key] then
      return (type(o.tag) == 'string' and '`' .. o.tag .. ':' or '') .. tostring(o)
    else isseen[o] = true end -- avoid recursion

    local used = {}

    local tag = o.tag
    local s = '{'
    if type(o.tag) == 'string' then
      s = '`' .. tag .. s; used['tag'] = true
    end
    local newindent = indent .. '  '

    local ks = {}; for k in pairs(o) do ks[#ks+1] = k end
    table.sort(ks, sort_keys_)
    --for i,k in ipairs(ks) do print ('keys', k) end

    local forcenummultiline
    for k in pairs(o) do
       if type(k) == 'number' and type(o[k]) == 'table' then forcenummultiline = true end
    end

    -- inline elements
    for _,k in ipairs(ks) do
      if used[k] then -- skip
      elseif ignore_keys_[k] then used[k] = true
      elseif (type(k) ~= 'number' or not forcenummultiline) and
              type(k) ~= 'table' and (type(o[k]) ~= 'table' or norecurse_keys_[k])
      then
        s = s .. dumpstring_key_(k, isseen, newindent) .. '=' .. M.dumpstring(o[k], isseen, newindent, k) .. ', '
        used[k] = true
      end
    end

    -- elements on separate lines
    local done
    for _,k in ipairs(ks) do
      if not used[k] then
        if not done then s = s .. '\n'; done = true end
        s = s .. newindent .. dumpstring_key_(k, isseen) .. '=' .. M.dumpstring(o[k], isseen, newindent, k) .. ',\n'
      end
    end
    s = s:gsub(',(%s*)$', '%1')
    s = s .. (done and indent or '') .. '}'
    return s
  elseif type(o) == 'string' then
    return string.format('%q', o)
  else
    return tostring(o)
  end
end

return M

