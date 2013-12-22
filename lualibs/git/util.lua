local lfs = require 'lfs'
local core = require 'git.core'
local deflate = core.deflate
local inflate = core.inflate
local sha = core.sha

module(..., package.seeall)

local BUF_SIZE = 4096

local dirsep = package.config:sub(1,1)

-- replaces '/' path separators on Windows with the correct ones ('\\')
function correct_separators(path)
	return path:gsub('/', dirsep)
end

-- joins several path components into a single path, uses system-specific directory
-- separator, cleans input, i.e. join_path('a/', 'b', 'c/') => 'a/b/c'
function join_path(...)
	local n = select('#', ...)
	local args = {...}
	for i=1,n do
		args[i] = args[i]:gsub(dirsep..'?$', '')
	end
	return table.concat(args, dirsep, 1, n)
end

-- Return the path with the all occurences of '/.' or '\.' (representing
-- the current directory) removed.
local function remove_curr_dir_dots(path)
	while path:match(dirsep .. "%." .. dirsep) do             -- match("/%./")
		path = path:gsub(dirsep .. "%." .. dirsep, dirsep)    -- gsub("/%./", "/")
	end
	return path:gsub(dirsep .. "%.$", "")                     -- gsub("/%.$", "")
end

-- Return whether the path is a root.
local function is_root(path)
	return path:find("^[%u%U.]?:?[/\\]$")
end

-- Return the path with the unnecessary trailing separator removed.
local function remove_trailing(path)
	if path:sub(-1) == dirsep and not is_root(path) then path = path:sub(1,-2) end
	return path
end

-- Extract file or directory name from its path.
local function extract_name(path)
	if is_root(path) then return path end

	path = remove_trailing(path)
	path = path:gsub("^.*" .. dirsep, "")
	return path
end

-- Return the string 'str', with all magic (pattern) characters escaped.
local function escape_magic(str)
	local escaped = str:gsub('[%-%.%+%[%]%(%)%^%%%?%*%^%$]','%%%1')
	return escaped
end

-- Return parent directory of the 'path' or nil if there's no parent directory.
-- If 'path' is a path to file, return the directory the file is in.
function parent_dir(path)
	path = remove_curr_dir_dots(path)
	path = remove_trailing(path)

	local dir = path:gsub(escape_magic(extract_name(path)) .. "$", "")
	if dir == "" then
		return nil
	else
		return remove_trailing(dir)
	end
end

-- Make a new directory, making also all of its parent directories that doesn't exist.
function make_dir(path)
	if lfs.attributes(path) then
		return true
	else
		local par_dir = parent_dir(path)
		if par_dir then
			assert(make_dir(par_dir))
		end
		return lfs.mkdir(path)
	end
end


-- Reader class
-- adapted from Penlight: https://raw.github.com/stevedonovan/Penlight/master/lua/pl/stringio.lua
 
local SR = {}
SR.__index = SR

function SR:_read(fmt)
    local i,str = self.i,self.str
    local sz = #str
    if i > sz then return nil, "past end of file" end
    local res
    if fmt == '*l' or fmt == '*L' then
        local idx = str:find('\n',i) or (sz+1)
        res = str:sub(i,fmt == '*l' and idx-1 or idx)
        self.i = idx+1
    elseif fmt == '*a' then
        res = str:sub(i)
        self.i = sz+1
    elseif fmt == '*n' then
        local _,i2,i2,idx
        _,idx = str:find ('%s*%d+',i)
        _,i2 = str:find ('^%.%d+',idx+1)
        if i2 then idx = i2 end
        _,i2 = str:find ('^[eE][%+%-]*%d+',idx+1)
        if i2 then idx = i2 end
        local val = str:sub(i,idx)
        res = tonumber(val)
        self.i = idx+1
    elseif type(fmt) == 'number' then
        res = str:sub(i,i+fmt-1)
        self.i = i + fmt
    else
        error("bad read format",2)
    end
    return res
		end

function SR:read(...)
    if select('#',...) == 0 then
        return self:_read('*l')
    else
        local res, fmts = {},{...}
        for i = 1, #fmts do
            res[i] = self:_read(fmts[i])
			end
        return unpack(res)
				end
			end

function SR:seek(whence,offset)
    local base
    whence = whence or 'cur'
    offset = offset or 0
    if whence == 'set' then
        base = 1
    elseif whence == 'cur' then
        base = self.i
    elseif whence == 'end' then
        base = #self.str
	end
    self.i = base + offset
    return self.i
end

function SR:close() -- for compatibility only
end

--- create a file-like object for reading from a given string.
-- @param s The input string.
function reader(s)
    return setmetatable({str=s,i=1},SR)
end


-- decompress the file and return a handle to temporary uncompressed file
function decompressed(path)
	local fi = assert(io.open(path, 'rb'))
	local result = {}

	local z = inflate()
	repeat
		local str = fi:read(BUF_SIZE)
		local data = z(str)
		if type(data) == 'string' then
			result[#result+1] = data
		else print('!!!', data) end
	until not str
	fi:close()

	return reader(table.concat(result))
end

-- reads until the byte \0, consumes it and returns the string up to the \0
function read_until_nul(f)
	local t = {}
	repeat
		local c = f:read(1)
		if c and c ~= '\0' then t[#t+1] = c end
	until not c or c == '\0'
	if #t > 0 then
		return table.concat(t)
	else
		return nil
	end
end

-- converts a string to lowercase hex
function to_hex(s)
	return (s:gsub('.', function(c)
		return string.format('%02x', string.byte(c))
	end))
end

-- converts a string from hex to binary
function from_hex(s)
	return (s:gsub('..', function(cc)
		return string.char(tonumber(cc, 16))
	end))
end

-- always returns readable (hex) hash
function readable_sha(s)
	if #s ~= 40 then return to_hex(s)
	else return s end
end

-- always returns binary hash
function binary_sha(s)
	if #s ~= 20 then return from_hex(s)
	else return s end
end

function object_sha(data, len, type)
	local header = type .. ' ' .. len .. '\0'
	local res = sha(header .. data)
	return res
end

function deflate(data)
	local c = deflate()
	return c(data, "finish")
end
