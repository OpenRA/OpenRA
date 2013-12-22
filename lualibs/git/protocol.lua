local socket = require 'socket'
local urllib = require 'socket.url'
local lfs = require 'lfs'

local Repo = git.repo.Repo
local Pack = git.pack.Pack
local join_path = git.util.join_path
local parent_dir = git.util.parent_dir
local make_dir = git.util.make_dir
local correct_separators = git.util.correct_separators

local assert, error, getmetatable, io, os, pairs, print, require, string, tonumber =
	assert, error, getmetatable, io, os, pairs, print, require, string, tonumber

local _VERSION, newproxy = _VERSION, newproxy

module(...)

local GIT_PORT = 9418

local function git_connect(host)
	local sock = assert(socket.connect(host, GIT_PORT))
	local gitsocket = {}

	function gitsocket:send(data)
		if not data then -- flush packet
			sock:send('0000')
		else
			local len = #data + 4
			len = string.format("%04x", len)
			assert(sock:send(len .. data))
		end
	end

	function gitsocket:receive()
		local len = assert(sock:receive(4))
		len = tonumber(len, 16)
		if len == 0 then return end -- flush packet
		local data = assert(sock:receive(len - 4))
		return data
	end

	function gitsocket:close()
		sock:close()
	end

	return gitsocket
end

local function addFinalizer(object, finalizer)
	if _VERSION <= "Lua 5.1" then
		local gc = newproxy(true)
		getmetatable(gc).__gc = finalizer
		object.__gc = gc
	else
		local mt = getmetatable(object)
		if mt then mt.__gc = finalizer
		else setmetatable(object, {__gc = finalizer})
		end
	end
end

local function git_fetch(host, path, repo, head, supress_progress)
	local s = git_connect(host)
	s:send('git-upload-pack '..path..'\0host='..host..'\0')

	local refs, refsbyname = {}, {}
	repeat
		local ref = s:receive()
		if ref then
			local sha, name = ref:sub(1,40), ref:sub(42, -2)
			refs[sha] = name
			refsbyname[name] = sha
		end
	until not ref

	local wantedSha
	local headsha = head and refsbyname[head]

	for sha, ref in pairs(refs) do
		-- we implicitly want this ref
		local wantObject = true 
		-- unless we ask for a specific head
		if headsha then         
			if sha ~= headsha then
				wantObject = false
			else
				wantedSha = sha
			end
		end
		-- or we already have it
		if repo and repo:has_object(sha) then
			wantObject = false
		end
		if wantObject then
			s:send('want '..sha..' multi_ack_detailed side-band-64k ofs-delta\n')
		end
	end

	if head and not wantedSha then
		error("Server does not have "..head)
	end

	s:send('deepen 1')
	s:send()
	while s:receive() do end
	s:send('done\n')
	
	assert(s:receive() == "NAK\n")
	
	local packname = os.tmpname() .. '.pack'
	local packfile = assert(io.open(packname, 'wb'))
	repeat
		local got = s:receive()
		if got then
			-- get sideband channel, 1=pack data, 2=progress, 3=error
			local cmd = string.byte(got:sub(1,1))
			local data = got:sub(2)
			if cmd == 1 then
				packfile:write(data)
			elseif cmd == 2 then
				if not supress_progress then io.write(data) end
			else
				error(data)
			end
		end
	until not got

	packfile:close()
	s:close()

	local pack = Pack.open(packname)
	if repo then
		pack:unpack(repo)
		repo.isShallow = true
		if wantedSha then
			local headfile = correct_separators(join_path(repo.dir, head))
			assert(make_dir(parent_dir(headfile)))
			local f = assert(io.open(headfile, 'wb'))
			f:write(wantedSha)
			f:close()
		end
	end

	addFinalizer(pack, function()
		os.remove(packname)
	end)

	return pack, wantedSha
end

function fetch(url, repo, head, supress_progress)
	if repo then assert(getmetatable(repo) == Repo, "arg #2 is not a repository") end
	url = urllib.parse(url)
	if url.scheme == 'git' then
		local pack, sha = git_fetch(url.host, url.path, repo, head, supress_progress)
		return pack, sha
	else
		error('unsupported scheme: '..url.scheme)
	end
end

function remotes(url)
	-- TODO: refactor common code
	url = assert(urllib.parse(url))

	if url.scheme ~= 'git' then
		error('unsupported scheme: '..url.scheme)
	end

	local host, path = url.host, url.path

	local s = git_connect(host)
	s:send('git-upload-pack '..path..'\0host='..host..'\0')

	local remote = {}
	repeat
		local ref = s:receive()
		if ref then
			local sha, name = ref:sub(1,40), ref:sub(42, -2)
			remote[name] = sha
		end
	until not ref

	s:close()

	return remote
end
