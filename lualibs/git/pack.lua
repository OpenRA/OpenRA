local io = io
local core = require 'git.core'

local assert, pcall, print, select, setmetatable, string, type, unpack =
	assert, pcall, print, select, setmetatable, string, type, unpack

local ord = string.byte
local fmt = string.format
local concat, insert = table.concat, table.insert

local band = core.band
local rshift, lshift = core.rshift, core.lshift

local to_hex = git.util.to_hex
local from_hex = git.util.from_hex
local object_sha = git.util.object_sha
local binary_sha = git.util.binary_sha
local readable_sha = git.util.readable_sha
local tmpfile = git.util.tmpfile
local reader = git.util.reader

module(...)

-- read git/Documentation/technical/pack-format.txt for some inspiration

-- 1 = commit, 2 = tree ...
local types = {'commit', 'tree', 'blob', 'tag', '???', 'ofs_delta', 'ref_delta'}

-- read a 4 byte unsigned integer stored in network order
local function read_int(f)
	local s = f:read(4)
	local a,b,c,d = s:byte(1,4)
	return a*256^3 + b*256^2 + c*256 + d
end

-- read in the type and file length
local function read_object_header(f)
	local b = ord(f:read(1))
	local type = band(rshift(b, 4), 0x7)
	local len = band(b, 0xF)
	local ofs = 0
	while band(b, 0x80) ~= 0 do
		b = ord(f:read(1))
		len = len + lshift(band(b, 0x7F), ofs * 7 + 4)
		ofs = ofs + 1
	end
	return len, type
end

-- reads in the delta header and returns the offset where original data is stored
local function read_delta_header(f)
	local b = ord(f:read(1))
	local offset = band(b, 0x7F)
	while band(b, 0x80) ~= 0 do
		offset = offset + 1
		b = ord(f:read(1))
		offset = lshift(offset, 7) + band(b, 0x7F)
	end
	return offset
end

-- read just enough of file `f` to uncompress `size` bytes
local function uncompress_by_len(f, size)
	local z = core.inflate()
	local chunks = {}
	local CHUNK_SIZE = 1024
	local curr_pos = f:seek()
	local inflated, eof, total
	-- read until end of zlib-compresed stream
	while not eof do
		local data = f:read(CHUNK_SIZE)
		inflated, eof, total = z(data)
		insert(chunks, inflated)
	end
	-- repair the current position in stream
	f:seek('set', curr_pos + total)
	return concat(chunks)
end

-- uncompress the object from the current location in `f`
local function unpack_object(f, len, type)
	local data = uncompress_by_len(f, len)
	return data, len, type
end

-- returns a size value encoded in delta data
local function delta_size(f)
	local size = 0
	local i = 0
	repeat
		local b = ord(f:read(1))
		size = size + lshift(band(b, 0x7F), i)
		i = i + 7
	until band(b, 0x80) == 0
	return size
end

-- returns a patched object from string `base` according to `delta` data
local function patch_object(base, delta, base_type)
	-- insert delta codes into temporary file
	local df = reader(delta)

	-- retrieve original and result size (for checks)
	local orig_size = delta_size(df)
	assert(#base == orig_size, fmt('#base(%d) ~= orig_size(%d)', #base, orig_size))

	local result_size = delta_size(df)
	local size = result_size

	local result = {}

	-- process the delta codes
	local cmd = df:read(1)
	while cmd do
		cmd = ord(cmd)
		if cmd == 0 then
			error('unexpected delta code 0')
		elseif band(cmd, 0x80) ~= 0 then -- copy a selected part of base data
			local cp_off, cp_size = 0, 0
			-- retrieve offset
			if band(cmd, 0x01) ~= 0 then cp_off = ord(df:read(1)) end
			if band(cmd, 0x02) ~= 0 then cp_off = cp_off + ord(df:read(1))*256 end
			if band(cmd, 0x04) ~= 0 then cp_off = cp_off + ord(df:read(1))*256^2 end
			if band(cmd, 0x08) ~= 0 then cp_off = cp_off + ord(df:read(1))*256^3 end
			-- retrieve size
			if band(cmd, 0x10) ~= 0 then cp_size = ord(df:read(1)) end
			if band(cmd, 0x20) ~= 0 then cp_size = cp_size + ord(df:read(1))*256 end
			if band(cmd, 0x40) ~= 0 then cp_size = cp_size + ord(df:read(1))*256^2 end
			if cp_size == 0 then cp_size = 0x10000 end
			if cp_off + cp_size > #base or cp_size > size then break end
			-- get the data and append it to result
			local data = base:sub(cp_off + 1, cp_off + cp_size)
			insert(result, data)
			size = size - cp_size
		else -- insert new data
			if cmd > size then break end
			local data = df:read(cmd)
			insert(result, data)
			size = size - cmd
		end
		cmd = df:read(1)
	end

	df:close()

	result = concat(result)
	assert(#result == result_size, fmt('#result(%d) ~= result_size(%d)', #result, result_size))
	return result, result_size, base_type
end

Pack = {}
Pack.__index = Pack

-- read an object from the current location in pack, or from a specific `offset`
-- if specified
function Pack:read_object(offset, ignore_data)
	local f = self.pack_file
	if offset then
		f:seek('set', offset)
	end
	local curr_pos = f:seek()

	local len, type = read_object_header(f)
	if type < 5 then -- commit, tree, blob, tag
		return unpack_object(f, len, type)
	elseif type == 6 then -- ofs_delta
		local offset = read_delta_header(f)
		local delta_data = uncompress_by_len(f, len)
		if not ignore_data then
			-- the offset is negative from the current location
			local base, base_len, base_type = self:read_object(curr_pos - offset)
			return patch_object(base, delta_data, base_type)
		end
	elseif type == 7 then -- ref_delta
		local sha = f:read(20)
		local delta_data = uncompress_by_len(f, len)
		if not ignore_data then
			-- lookup the object in the pack by sha
			-- FIXME: maybe lookup in repo/other packs
			local base_offset = self.index[binary_sha(sha)]
			local base, base_len, base_type = self:read_object(base_offset)
			return patch_object(base, delta_data, base_type)
		end
	else
		error('unknown object type: '..type)
	end
end

-- returns true if this pack contains the given object
function Pack:has_object(sha)
	return self.index[binary_sha(sha)] ~= nil
end

-- if the object name `sha` exists in the pack, returns a temporary file with the
-- object content, length and type, otherwise returns nil
function Pack:get_object(sha)
	local offset = self.index[binary_sha(sha)]
	if not offset then
		print('!!! Failed to find object', readable_sha(sha))
	end

	local data, len, type = self:read_object(offset)
	print(readable_sha(sha), len, type, data)
	local f = tmpfile()
	f:write(data)
	f:seek('set', 0)

	return f, len, types[type]
end

function Pack:unpack(repo)
	for i=1, self.nobjects do
		local offset = self.offsets[i]
		local data, len, type = self:read_object(offset)
		repo:store_object(data, len, types[type])
	end
end

-- parses the index
function Pack:parse_index(index_file)
	local f = index_file

	local head = f:read(4)
	assert(head == '\255tOc', "Incorrect header: " .. head)
	local version = read_int(f)
	assert(version == 2, "Incorrect version: " .. version)

	-- first the fanout table (how many objects are in the index, whose
	-- first byte is below or equal to i)
	local fanout = {}
	for i=0, 255 do
		local nobjs = read_int(f)
		fanout[i] = nobjs
	end

	-- the last element in fanout is the number of all objects in index
	local count = fanout[255]

	-- then come the sorted object names (=sha hash)
	local tmp = {}
	for i=1,count do
		local sha = f:read(20)
		tmp[i] = { sha = sha }
	end

	-- then the CRCs (assume ok, skip them)
	for i=1, count do
		local crc = f:read(4)
	end

	-- then come the offsets - read just the 32bit ones, does not handle packs > 2G
	for i=1, count do
		local offset = read_int(f)
		tmp[i].offset = offset
	end

	-- construct the lookup table
	local lookup = {}
	for i=1, count do
		lookup[tmp[i].sha] = tmp[i].offset
	end
	self.index = lookup
end

-- constructs the index/offsets if the index file is missing
function Pack:construct_index(path)
	local index = {}
	for i=1, self.nobjects do
		local offset = self.offsets[i]
		local data, len, type = self:read_object(offset)
		local sha = object_sha(data, len, types[type])
		index[binary_sha(sha)] = offset
	end
	self.index = index
end

function Pack:close()
	self.pack_file:close()
end

function Pack.open(path)
	local fp = assert(io.open(path, 'rb')) -- stays open
	
	-- read the pack header
	local head = fp:read(4)
	assert(head == 'PACK', "Incorrect header: " .. head)
	local version = read_int(fp)
	assert(version == 2, "Incorrect version: " .. version)
	local nobj = read_int(fp)

	local pack = setmetatable({
		offsets = {},
		nobjects = nobj,
		pack_file = fp,
	}, Pack)
	
	-- fill the offsets by traversing through the pack
	for i=1,nobj do
		pack.offsets[i] = fp:seek()
		-- ignore the object data, we only need the offset in the pack
		pack:read_object(nil, true)
	end

	-- read the index
	local fi = io.open((path:gsub('%.pack$', '.idx')), 'rb')
	if fi then
		pack:parse_index(fi)
		fi:close()
	else
		pack:construct_index(path)
	end

	return pack
end

return Pack