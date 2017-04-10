--[[
   The MIT License

   Copyright (c) 2010 Ignacio Burgueño

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in
   all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
   THE SOFTWARE.

   Source: https://github.com/ignacio/StackTracePlus
]]
-- tables
local _G = _G
local string, io, debug, coroutine = string, io, debug, coroutine

-- functions
local tostring, print, require = tostring, print, require
local next, assert = next, assert
local pcall, type, pairs, ipairs = pcall, type, pairs, ipairs
local error = error

assert(debug, "debug table must be available at this point")

local io_open = io.open
local string_gmatch = string.gmatch
local string_sub = string.sub
local table_concat = table.concat

local _M = {
	max_tb_output_len = 70	-- controls the maximum length of the 'stringified' table before cutting with ' (more...)'
}

-- this tables should be weak so the elements in them won't become uncollectable
local m_known_tables = { [_G] = "_G (global table)" }
local function add_known_module(name, desc)
	local ok, mod = pcall(require, name)
	if ok then
		m_known_tables[mod] = desc
	end
end

add_known_module("string", "string module")
add_known_module("io", "io module")
add_known_module("os", "os module")
add_known_module("table", "table module")
add_known_module("math", "math module")
add_known_module("package", "package module")
add_known_module("debug", "debug module")
add_known_module("coroutine", "coroutine module")

-- lua5.2
add_known_module("bit32", "bit32 module")
-- luajit
add_known_module("bit", "bit module")
add_known_module("jit", "jit module")


local m_user_known_tables = {}

local m_known_functions = {}
for _, name in ipairs{
	-- Lua 5.2, 5.1
	"assert",
	"collectgarbage",
	"dofile",
	"error",
	"getmetatable",
	"ipairs",
	"load",
	"loadfile",
	"next",
	"pairs",
	"pcall",
	"print",
	"rawequal",
	"rawget",
	"rawlen",
	"rawset",
	"require",
	"select",
	"setmetatable",
	"tonumber",
	"tostring",
	"type",
	"xpcall",

	-- Lua 5.1
	"gcinfo",
	"getfenv",
	"loadstring",
	"module",
	"newproxy",
	"setfenv",
	"unpack",
	-- TODO: add table.* etc functions
} do
	if _G[name] then
		m_known_functions[_G[name]] = name
	end
end



local m_user_known_functions = {}

local function safe_tostring (value)
	local ok, err = pcall(tostring, value)
	if ok then return err else return ("<failed to get printable value>: '%s'"):format(err) end
end

-- Private:
-- Parses a line, looking for possible function definitions (in a very naïve way)
-- Returns '(anonymous)' if no function name was found in the line
local function ParseLine(line)
	assert(type(line) == "string")
	--print(line)
	local match = line:match("^%s*function%s+(%w+)")
	if match then
		--print("+++++++++++++function", match)
		return match
	end
	match = line:match("^%s*local%s+function%s+(%w+)")
	if match then
		--print("++++++++++++local", match)
		return match
	end
	match = line:match("^%s*local%s+(%w+)%s+=%s+function")
	if match then
		--print("++++++++++++local func", match)
		return match
	end
	match = line:match("%s*function%s*%(")	-- this is an anonymous function
	if match then
		--print("+++++++++++++function2", match)
		return "(anonymous)"
	end
	return "(anonymous)"
end

-- Private:
-- Tries to guess a function's name when the debug info structure does not have it.
-- It parses either the file or the string where the function is defined.
-- Returns '?' if the line where the function is defined is not found
local function GuessFunctionName(info)
	--print("guessing function name")
	if type(info.source) == "string" and info.source:sub(1,1) == "@" then
		local file, err = io_open(info.source:sub(2), "r")
		if not file then
			print("file not found: "..tostring(err))	-- whoops!
			return "?"
		end
		local line
		for i = 1, info.linedefined do
			line = file:read("*l")
		end
		if not line then
			print("line not found")	-- whoops!
			return "?"
		end
		return ParseLine(line)
	else
		local line
		local lineNumber = 0
		for l in string_gmatch(info.source, "([^\n]+)\n-") do
			lineNumber = lineNumber + 1
			if lineNumber == info.linedefined then
				line = l
				break
			end
		end
		if not line then
			print("line not found")	-- whoops!
			return "?"
		end
		return ParseLine(line)
	end
end

---
-- Dumper instances are used to analyze stacks and collect its information.
--
local Dumper = {}

Dumper.new = function(thread)
	local t = { lines = {} }
	for k,v in pairs(Dumper) do t[k] = v end

	t.dumping_same_thread = (thread == coroutine.running())

	-- if a thread was supplied, bind it to debug.info and debug.get
	-- we also need to skip this additional level we are introducing in the callstack (only if we are running
	-- in the same thread we're inspecting)
	if type(thread) == "thread" then
		t.getinfo = function(level, what)
			if t.dumping_same_thread and type(level) == "number" then
				level = level + 1
			end
			return debug.getinfo(thread, level, what)
		end
		t.getlocal = function(level, loc)
			if t.dumping_same_thread then
				level = level + 1
			end
			return debug.getlocal(thread, level, loc)
		end
	else
		t.getinfo = debug.getinfo
		t.getlocal = debug.getlocal
	end

	return t
end

-- helpers for collecting strings to be used when assembling the final trace
function Dumper:add (text)
	self.lines[#self.lines + 1] = text
end
function Dumper:add_f (fmt, ...)
	self:add(fmt:format(...))
end
function Dumper:concat_lines ()
	return table_concat(self.lines)
end

---
-- Private:
-- Iterates over the local variables of a given function.
--
-- @param level The stack level where the function is.
--
function Dumper:DumpLocals (level)
	local prefix = "\t "
	local i = 1

	if self.dumping_same_thread then
		level = level + 1
	end

	local name, value = self.getlocal(level, i)
	if not name then
		return
	end
	self:add("\tLocal variables:\r\n")
	while name do
		if type(value) == "number" then
			self:add_f("%s%s = number: %g\r\n", prefix, name, value)
		elseif type(value) == "boolean" then
			self:add_f("%s%s = boolean: %s\r\n", prefix, name, tostring(value))
		elseif type(value) == "string" then
			self:add_f("%s%s = string: %q\r\n", prefix, name, value)
		elseif type(value) == "userdata" then
			self:add_f("%s%s = %s\r\n", prefix, name, safe_tostring(value))
		elseif type(value) == "nil" then
			self:add_f("%s%s = nil\r\n", prefix, name)
		elseif type(value) == "table" then
			if m_known_tables[value] then
				self:add_f("%s%s = %s\r\n", prefix, name, m_known_tables[value])
			elseif m_user_known_tables[value] then
				self:add_f("%s%s = %s\r\n", prefix, name, m_user_known_tables[value])
			else
				local txt = "{"
				for k,v in pairs(value) do
					txt = txt..safe_tostring(k)..":"..safe_tostring(v)
					if #txt > _M.max_tb_output_len then
						txt = txt.." (more...)"
						break
					end
					if next(value, k) then txt = txt..", " end
				end
				self:add_f("%s%s = %s  %s\r\n", prefix, name, safe_tostring(value), txt.."}")
			end
		elseif type(value) == "function" then
			local info = self.getinfo(value, "nS")
			local fun_name = info.name or m_known_functions[value] or m_user_known_functions[value]
			if info.what == "C" then
				self:add_f("%s%s = C %s\r\n", prefix, name, (fun_name and ("function: " .. fun_name) or tostring(value)))
			else
				local source = info.short_src
				if source:sub(2,7) == "string" then
					source = source:sub(9)
				end
				--for k,v in pairs(info) do print(k,v) end
				fun_name = fun_name or GuessFunctionName(info)
				self:add_f("%s%s = Lua function '%s' (defined at line %d of chunk %s)\r\n", prefix, name, fun_name, info.linedefined, source)
			end
		elseif type(value) == "thread" then
			self:add_f("%sthread %q = %s\r\n", prefix, name, tostring(value))
		end
		i = i + 1
		name, value = self.getlocal(level, i)
	end
end


---
-- Public:
-- Collects a detailed stack trace, dumping locals, resolving function names when they're not available, etc.
-- This function is suitable to be used as an error handler with pcall or xpcall
--
-- @param thread An optional thread whose stack is to be inspected (defaul is the current thread)
-- @param message An optional error string or object.
-- @param level An optional number telling at which level to start the traceback (default is 1)
--
-- Returns a string with the stack trace and a string with the original error.
--
function _M.stacktrace(thread, message, level)
	if type(thread) ~= "thread" then
		-- shift parameters left
		thread, message, level = nil, thread, message
	end

	thread = thread or coroutine.running()

	level = level or 1

	local dumper = Dumper.new(thread)

	local original_error

	if type(message) == "table" then
		dumper:add("an error object {\r\n")
		local first = true
		for k,v in pairs(message) do
			if first then
				dumper:add("  ")
				first = false
			else
				dumper:add(",\r\n  ")
			end
			dumper:add(safe_tostring(k))
			dumper:add(": ")
			dumper:add(safe_tostring(v))
		end
		dumper:add("\r\n}")
		original_error = dumper:concat_lines()
	elseif type(message) == "string" then
		dumper:add(message)
		original_error = message
	end

	dumper:add("\r\n")
	dumper:add[[
Stack Traceback
===============
]]
	--print(error_message)

	local level_to_show = level
	if dumper.dumping_same_thread then level = level + 1 end

	local info = dumper.getinfo(level, "nSlf")
	while info do
		if info.what == "main" then
			if string_sub(info.source, 1, 1) == "@" then
				dumper:add_f("(%d) main chunk of file '%s' at line %d\r\n", level_to_show, string_sub(info.source, 2), info.currentline)
			else
				dumper:add_f("(%d) main chunk of %s at line %d\r\n", level_to_show, info.short_src, info.currentline)
			end
		elseif info.what == "C" then
			--print(info.namewhat, info.name)
			--for k,v in pairs(info) do print(k,v, type(v)) end
			local function_name = m_user_known_functions[info.func] or m_known_functions[info.func] or info.name or tostring(info.func)
			dumper:add_f("(%d) %s C function '%s'\r\n", level_to_show, info.namewhat, function_name)
			--dumper:add_f("%s%s = C %s\r\n", prefix, name, (m_known_functions[value] and ("function: " .. m_known_functions[value]) or tostring(value)))
		elseif info.what == "tail" then
			--print("tail")
			--for k,v in pairs(info) do print(k,v, type(v)) end--print(info.namewhat, info.name)
			dumper:add_f("(%d) tail call\r\n", level_to_show)
			dumper:DumpLocals(level)
		elseif info.what == "Lua" then
			local source = info.short_src
			local function_name = m_user_known_functions[info.func] or m_known_functions[info.func] or info.name
			if source:sub(2, 7) == "string" then
				source = source:sub(9)
			end
			local was_guessed = false
			if not function_name or function_name == "?" then
				--for k,v in pairs(info) do print(k,v, type(v)) end
				function_name = GuessFunctionName(info)
				was_guessed = true
			end
			-- test if we have a file name
			local function_type = (info.namewhat == "") and "function" or info.namewhat
			if info.source and info.source:sub(1, 1) == "@" then
				dumper:add_f("(%d) Lua %s '%s' at file '%s:%d'%s\r\n", level_to_show, function_type, function_name, info.source:sub(2), info.currentline, was_guessed and " (best guess)" or "")
			elseif info.source and info.source:sub(1,1) == '#' then
				dumper:add_f("(%d) Lua %s '%s' at template '%s:%d'%s\r\n", level_to_show, function_type, function_name, info.source:sub(2), info.currentline, was_guessed and " (best guess)" or "")
			else
				dumper:add_f("(%d) Lua %s '%s' at line %d of chunk '%s'\r\n", level_to_show, function_type, function_name, info.currentline, source)
			end
			dumper:DumpLocals(level)
		else
			dumper:add_f("(%d) unknown frame %s\r\n", level_to_show, info.what)
		end

		level = level + 1
		level_to_show = level_to_show + 1
		info = dumper.getinfo(level, "nSlf")
	end

	return dumper:concat_lines(), original_error
end

--
-- Adds a table to the list of known tables
function _M.add_known_table(tab, description)
	if m_known_tables[tab] then
		error("Cannot override an already known table")
	end
	m_user_known_tables[tab] = description
end

--
-- Adds a function to the list of known functions
function _M.add_known_function(fun, description)
	if m_known_functions[fun] then
		error("Cannot override an already known function")
	end
	m_user_known_functions[fun] = description
end

return _M
