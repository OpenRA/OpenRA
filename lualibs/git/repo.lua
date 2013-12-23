local util = require 'git.util'
local objects = require 'git.objects'
local core = require 'git.core'
local pack = require 'git.pack'

local join_path = util.join_path
local decompressed = util.decompressed
local read_until_nul = util.read_until_nul
local to_hex = util.to_hex
local object_sha = util.object_sha
local readable_sha = util.readable_sha

local deflate = core.deflate

local lfs = require 'lfs'
local assert, error, io, ipairs, print, os, setmetatable, string, table =
	assert, error, io, ipairs, print, os, setmetatable, string, table

module(...)

Repo = {}
Repo.__index = Repo

-- retrieves an object identified by `sha` from the repository or its packs
-- returns a file-like object (supports 'read', 'seek' and 'close'), the size
-- of the object and its type
-- errors when the object does not exist
function Repo:raw_object(sha)
	-- first, look in 'objects' directory
	-- first byte of sha is the directory, the rest is name of object file
	sha = readable_sha(sha)
	local dir = sha:sub(1,2)
	local file = sha:sub(3)
	local path = join_path(self.dir, 'objects', dir, file)

	if not lfs.attributes(path, 'size') then
		-- then, try to look in packs
		for _, pack in ipairs(self.packs) do
			local obj, len, typ = pack:get_object(sha)
			if obj then
				return obj, len, typ
			end
		end
		error('Object not found in object neither in packs: '..sha)
	else		
		-- the objects are zlib compressed
		local f = decompressed(path)

		-- retrieve the type and length - <type> SP <len> \0 <data...>
		local content = read_until_nul(f)
		local typ, len = content:match('(%w+) (%d+)')

		return f, len, typ
	end
end

--- Store a new object into the repository in `objects` directory.
-- @param data A string containing the contents of the new file.
-- @param len The length of the data.
-- @param type One of 'commit', 'blob', 'tree', 'tag'
function Repo:store_object(data, len, type)
	local sha = readable_sha(object_sha(data, len, type))
	local dir = sha:sub(1,2)
	local file = sha:sub(3)
	util.make_dir(join_path(self.dir, 'objects', dir))
	local path = join_path(self.dir, 'objects', dir, file)
	local fo = assert(io.open(path, 'wb'))
	local header = type .. ' ' .. len .. '\0'
	local compressed = deflate()(header .. data, "finish")
	fo:write(compressed)
	fo:close()
end

local function resolvetag(f)
	local tag
	local line = f:read()
	while line do
		tag = line:match('^object (%x+)$')
		if tag then break end
		line = f:read()
	end
	f:close()
	return tag
end

function Repo:commit(sha)
	local f, len, typ = self:raw_object(sha)
	while typ == 'tag' do
		sha = assert(resolvetag(f), 'could not parse tag for '..readable_sha(sha))
		f, len, typ = self:raw_object(sha)
	end
	assert(typ == 'commit', string.format('%s (%s) is not a commit', sha, typ))

	local commit = { id = sha, repo = self, stored = true, parents = {} }
	repeat
		local line = f:read()
		if not line then break end

		local space = line:find(' ') or 0
		local word = line:sub(1, space - 1)
		local afterSpace = line:sub(space + 1)

		if word == 'tree' then
			commit.tree_sha = afterSpace
		elseif word == 'parent' then
			table.insert(commit.parents, afterSpace)
		elseif word == 'author' then
			commit.author = afterSpace
		elseif word == 'committer' then
			commit.committer = afterSpace
		elseif commit.message then
			table.insert(commit.message, line)
		elseif line == '' then
			commit.message = {}
		end
	until false -- ends with break
	f:close()

	commit.message = table.concat(commit.message, '\n')

	return setmetatable(commit, objects.Commit)
end

function Repo:tree(sha)
	local f, len, typ = self:raw_object(sha)
	assert(typ == 'tree', string.format('%s (%s) is not a tree', sha, typ))

	local tree = { id = sha, repo = self, stored = true, _entries = {} }

	while true do
		local info = read_until_nul(f)
		if not info then break end
		local entry_sha = to_hex(f:read(20))
		local mode, name = info:match('^(%d+)%s(.+)$')
		local entry_type = 'blob'
		if mode == '40000' then
			entry_type = 'tree'
		elseif mode == '160000' then
			entry_type = 'commit'
		end
		tree._entries[name] = { mode = mode, id = entry_sha, type = entry_type }
	end

	f:close()

	return setmetatable(tree, objects.Tree)
end

-- retrieves a Blob
function Repo:blob(sha)
	local f, len, typ = self:raw_object(sha)
	f:close() -- can be reopened in Blob:content()

	assert(typ == 'blob', string.format('%s (%s) is not a blob', sha, typ))
	return setmetatable({ 
		id = sha,
		len = len,
		repo = self,
		stored = true }, objects.Blob)
end

function Repo:head()
	return self:commit(self.refs.HEAD)
end

function Repo:has_object(sha)
	local dir = sha:sub(1,2)
	local file = sha:sub(3)
	local path = join_path(self.dir, 'objects', dir, file)

	if lfs.attributes(path, 'size') then return true end

	for _, pack in ipairs(self.packs) do
		local has = pack:has_object(sha)
		if has then return true end
	end

	return false
end

function Repo:checkout(sha, target)
	if not target then target = self.workDir end
	assert(target, 'target directory not specified')

	local commit = self:commit(sha)
	commit:checkout(target)

	-- if the repo was checked out using the deepen command (one level of history only)
	-- mark the commit's parent as shalow, that is it has no history
	if self.isShallow then
		-- if it has a parent, mark it shallow
		if commit.parents[1] then
			local f = assert(io.open(self.dir .. '/shallow', "w"))
			f:write(commit.parents[1], '\n')
			f:close()
		end
	end
end

function Repo:close()
	for _, pack in ipairs(self.packs) do
		pack:close()
	end
end	

function create(dir)
	if not dir:match('%.git.?$') then
		dir = join_path(dir, '.git')
	end
	
	util.make_dir(dir)
	util.make_dir(dir .. '/branches')
	util.make_dir(dir .. '/hooks')
	util.make_dir(dir .. '/info')	
	util.make_dir(dir .. '/objects/info')
	util.make_dir(dir .. '/objects/pack')
	util.make_dir(dir .. '/refs/heads')
	util.make_dir(dir .. '/refs/tags')
	util.make_dir(dir .. '/refs/remotes')

	do
		local f = assert(io.open(dir .. "/HEAD", "w"))
		f:write("ref: refs/heads/master\n")
		f:close()
	end

	local refs = {}
	local packs = {}

	return setmetatable({
		dir = dir,
		refs = refs,
		packs = packs,
	}, Repo)
end

-- opens a repository located in working directory `dir` or directly a .git repo
function open(dir)
	local workDir = dir
	if not dir:match('%.git.?$') then
		dir = join_path(dir, '.git')
	else
		workDir = nil -- no working directory, working directly with repo
	end

	local refs = {}
	for _,d in ipairs{'refs/heads', 'refs/tags'} do
		for fn in lfs.dir(join_path(dir, d)) do
			if fn ~= '.' and fn ~= '..' then
				local path = join_path(dir, d, fn)
				local f = assert(io.open(path), 'rb')
				local ref = f:read()
				refs[join_path(d, fn)] = ref
				f:close()
			end
		end
	end

	local packs = {}
	for fn in lfs.dir(join_path(dir, 'objects/pack')) do
		if fn:match('%.pack$') then
			local path = join_path(dir, 'objects/pack', fn)
			table.insert(packs, pack.open(path))
		end
	end

	local head = io.open(join_path(dir, 'HEAD'), 'rb')
	if head then
		local src = head:read()
		local HEAD = src:match('ref: (.-)$')
		refs.HEAD = refs[HEAD]
		head:close()
	end

	return setmetatable({
		dir = dir,
		workDir = workDir,
		refs = refs,
		packs = packs,
	}, Repo)
end

return Repo
