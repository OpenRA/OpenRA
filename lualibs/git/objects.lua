local util = require 'git.util'

local assert, next, io, print, os, type, string, pairs, tostring =
	assert, next, io, print, os, type, string, pairs, tostring
local join_path = git.util.join_path

local require = require

local isPosix = package.config:sub(1,1) == '/' -- wild guess

module(...)

Commit = {}
Commit.__index = Commit

function Commit:tree()
	return self.repo:tree(self.tree_sha)
end

function Commit:checkout(path)
	assert(path, 'path argument missing')
	self:tree():checkoutTo(path)
end


Tree = {}
Tree.__index = function (t,k)
	if Tree[k] then return Tree[k] end
	return t:entry(k)
end

function Tree:entries()
	return function(t, n)
		local n, entry = next(t, n)
		if entry then
			local object
			if entry.type == 'tree' then
				object = self.repo:tree(entry.id)
			elseif entry.type == 'blob' then
				object = self.repo:blob(entry.id)
				object.mode = entry.mode
			elseif entry.type == 'commit' then
				-- this is possibly a commit in a submodule, 
				-- do not retrieve it from current repo
				object = entry
			else
				error('Unknown entry type: ' .. entry.type)
			end
			return n, entry.type, object
		end
	end, self._entries
end

function Tree:entry(n)
	local e = self._entries[n]
	if not e then return end
	if e.type == 'tree' then
		return self.repo:tree(e.id)
	elseif e.type == 'commit' then
		return self.repo:commit(e.id)
	elseif e.type == 'blob' then
		return self.repo:blob(e.id)
	else
		error('Unknown entry type: ' .. e.type)
	end
end

function Tree:walk(func, path)
	path = path or '.'
	assert(type(func) == "function", "argument is not a function")
	local function walk(tree, path)
		for name, type, entry in tree:entries() do
			local entry_path = join_path(path, name)
			func(entry, entry_path, type)

			if type == "tree" then
				walk(entry, entry_path)
			end
		end
	end
	walk(self, path)
end

function Tree:checkoutTo(path)
	util.make_dir(path)
	self:walk(function (entry, entry_path, type)
		if type == 'tree' then
			util.make_dir(entry_path)
		elseif type == 'blob' then
			local out = assert(io.open(entry_path, 'wb'))
			out:write(entry:content())
			out:close()
			if isPosix then
				local mode = entry.mode:sub(-3,-1) -- fixme: is this ok?
				local cmd = 'chmod '..mode..' "'..entry_path..'"'
				os.execute(cmd)
			end
		elseif type == 'commit' then
			-- this is a submodule referencing a commit,
			-- make a directory for it
			util.make_dir(entry_path)
		else
			error('Unknown entry type: ', type)
		end
	end, path)
end

Blob = {}
Blob.__index = Blob

function Blob:content()
	if self.stored then
		local f = self.repo:raw_object(self.id)
		local ret = f:read('*a') or ""
		f:close()
		return ret
	else
		return self.data
	end
end

