-- Working with manifest and dist.info files

module ("dist.manifest", package.seeall)

local cfg = require "dist.config"
local git = require "dist.git"
local sys = require "dist.sys"
local utils = require "dist.utils"

-- Return the manifest table from 'manifest_file'. If the manifest is in cache,
-- then the cached version is used. You can set the cache timeout value in
-- 'config.cache_timeout' variable.
-- If optional 'force_no_cache' parameter is true, then the cache is not used.
function get_manifest(manifest_file, force_no_cache)
    manifest_file = manifest_file or sys.make_path(cfg.root_dir, cfg.manifest_file)
    force_no_cache = force_no_cache or false

    assert(type(manifest_file) == "string", "manifest.get_manifest: Argument 'manifest_file' is not a string.")
    assert(type(force_no_cache) == "boolean", "manifest.get_manifest: Argument 'force_no_cache' is not a boolean.")
    manifest_file = sys.abs_path(manifest_file)

    -- download new manifest to the cache if not present or cache not used or cache expired
    if not sys.exists(manifest_file) or force_no_cache or not cfg.cache or utils.cache_timeout_expired(cfg.cache_timeout, manifest_file) then
        local manifest_dest = sys.parent_dir(manifest_file) or sys.current_dir()
        local ok, err = download_manifest(manifest_dest, cfg.repos)
        if not ok then return nil, "Error when downloading manifest: " .. err end
    end

    -- load manifest from cache
    local status, ret = load_manifest(manifest_file)
    if not status then return nil, "Error when loading manifest: " .. ret end

    return ret
end

-- Download manifest from the table of git 'repository_urls' to 'dest_dir' and return true on success
-- and nil and error message on error.
function download_manifest(dest_dir, repository_urls)
    dest_dir = dest_dir or sys.make_path(cfg.root_dir, cfg.cache_dir)
    repository_urls = repository_urls or cfg.repos
    if type(repository_urls) == "string" then repository_urls = {repository_urls} end

    assert(type(dest_dir) == "string", "manifest.download_manifest: Argument 'dest_dir' is not a string.")
    assert(type(repository_urls) == "table", "manifest.download_manifest: Argument 'repository_urls' is not a table or string.")
    dest_dir = sys.abs_path(dest_dir)

    -- define used files and directories
    local manifest_filename = sys.extract_name(cfg.manifest_file)
    local manifest_file = sys.make_path(dest_dir, manifest_filename)
    local temp_dir = sys.make_path(cfg.root_dir, cfg.temp_dir)

    -- ensure that destination directory exists
    local ok, err = sys.make_dir(dest_dir)
    if not ok then return nil, err end

    -- retrieve manifests from repositories and collect them into one manifest table
    local manifest = {}

    if #repository_urls == 0 then return nil, "No repository url specified." end

    print("Downloading repository information...")
    for k, repo in pairs(repository_urls) do
        local clone_dir = sys.make_path(temp_dir, "repository_" .. tostring(k))

        -- clone the repo and add its '.gitmodules' file to the manifest table

        ok, err = git.create_repo(clone_dir)

        local sha
        if ok then sha, err = git.fetch_branch(clone_dir, repo, "master") end
        if sha then ok, err = git.checkout_sha(sha, clone_dir) end

        if not (ok and sha) then
            if not cfg.debug then sys.delete(clone_dir) end
            return nil, "Error when downloading the manifest from repository with url: '" .. repo .. "': " .. err
        else
            for _, pkg in pairs(load_gitmodules(sys.make_path(clone_dir, ".gitmodules"))) do
                table.insert(manifest, pkg)
            end
        end
        if not cfg.debug then sys.delete(clone_dir) end
    end

    -- save the new manifest table to the file
    ok, err = save_manifest(manifest, manifest_file)
    if not ok then return nil, err end

    return true
end

-- A secure loadfile function
-- If file code chunk has upvalues, the first upvalue is set to the given
-- environement, if that parameter is given, or to the value of the global environment.
local function secure_loadfile(file, env)
    assert(type(file) == "string", "secure_loadfile: Argument 'file' is not a string.")

    -- use the given (or create a new) restricted environment
    local env = env or {}

    -- load the file and run in a protected call with the restricted env
    -- setfenv is deprecated in lua 5.2 in favor of giving env in arguments
    -- the additional loadfile arguments are simply ignored for previous lua versions
    local f, err = loadfile(file, 'bt', env)
    if f then
        if setfenv ~= nil then
            setfenv(f, env)
        end
        return pcall(f)
    else
        return nil, err
    end
end

-- Load and return manifest table from the manifest file.
-- If manifest file not present, return nil.
function load_manifest(manifest_file)
    manifest_file = manifest_file or sys.make_path(cfg.root_dir, cfg.manifest_file)

    return secure_loadfile(sys.abs_path(manifest_file))
end

-- Load '.gitmodules' file and returns manifest table.
-- If the file is not present, return nil.
function load_gitmodules(gitmodules_file)
    gitmodules_file = gitmodules_file or sys.make_path(cfg.root_dir, cfg.manifest_file)
    assert(type(gitmodules_file) == "string", "manifest.load_gitmodules: Argument 'gitmodules_file' is not a string.")
    gitmodules_file = sys.abs_path(gitmodules_file)

    if sys.exists(gitmodules_file) then
        -- load the .gitmodules file
        local file, err = io.open(gitmodules_file, "r")
        if not file then return nil, "Error when opening the .gitmodules file '" .. gitmodules_file .. "':" .. err end

        local mf_text = file:read("*a")
        file:close()
        if not mf_text then return nil, "Error when reading the .gitmodules file '" .. gitmodules_file .. "':" .. err end

        manifest = {}
        for url in mf_text:gmatch("git://%S+/%S+") do
            pkg = {name = url:match("git://%S+/(%S+)%.git") or url:match("git://%S+/(%S+)"), version = "scm", path = url}
            table.insert(manifest, pkg)
        end

        return manifest
    else
        return nil, "Error when loading the .gitmodules: file '" .. gitmodules_file .. "' doesn't exist."
    end
end

-- Save manifest table to the 'file'
function save_manifest(manifest_table, file)
    assert(type(manifest_table) == "table", "manifest.save_distinfo: Argument 'manifest_table' is not a table.")
    assert(type(file) == "string", "manifest.save_distinfo: Argument 'file' is not a string.")
    file = sys.abs_path(file)

    -- Print table 'tbl' to io stream 'file'.
    local function print_table(file, tbl, in_nested_table)
        for k, v in pairs(tbl) do
            -- print key
            if in_nested_table then file:write("\t\t") end
            if type(k) ~= "number" then
                file:write("['" .. k .. "']" .. " = ")
            end
            -- print value
            if type(v) == "table" then
                file:write("{\n")
                print_table(file, v, true)
                if in_nested_table then file:write("\t") end
                file:write("\t}")
            else
                if in_nested_table then file:write("\t") end
                if type(v) == "string" then
                    file:write('[[' .. v .. ']]')
                else
                    file:write(tostring(v))
                end
            end
            file:write(",\n")
        end
    end

    local manifest_file = io.open(file, "w")
    if not manifest_file then return nil, "Error when saving manifest: cannot open the file '" .. file .. "'." end

    manifest_file:write('return {\n')
    print_table(manifest_file, manifest_table)
    manifest_file:write('},\ntrue')
    manifest_file:close()

    return true
end

-- Load and return package info table from the distinfo_file file.
-- If file not present, return nil.
function load_distinfo(distinfo_file)
    assert(type(distinfo_file) == "string", "manifest.load_distinfo: Argument 'distinfo_file' is not a string.")
    distinfo_file = sys.abs_path(distinfo_file)

    -- load the distinfo file
    local distinfo_env = {}
    local status, ret = secure_loadfile(distinfo_file, distinfo_env)
    if not status then return nil, "Error when loading package info: " .. ret end

    return distinfo_env
end

-- Save distinfo table to the 'file'
function save_distinfo(distinfo_table, file)
    assert(type(distinfo_table) == "table", "manifest.save_distinfo: Argument 'distinfo_table' is not a table.")
    assert(type(file) == "string", "manifest.save_distinfo: Argument 'file' is not a string.")
    file = sys.abs_path(file)

    -- Print table 'tbl' to io stream 'file'.
    local function print_table(file, tbl, in_nested_table)
        for k, v in pairs(tbl) do
            -- print key
            if type(k) ~= "number" then
                file:write(k .. " = ")
            end
            -- print value
            if type(v) == "table" then
                file:write("{\n")
                print_table(file, v, true)
                file:write("}\n")
            elseif type(v) == "string" then
                if in_nested_table then
                    file:write('[[' .. v .. ']]')
                else
                    file:write('"' .. v .. '"')
                end
            else
                file:write(v)
            end
            if in_nested_table then
                file:write(",")
            end
            file:write("\n")
        end
    end

    local distinfo_file = io.open(file, "w")
    if not distinfo_file then return nil, "Error when saving dist-info table: cannot open the file '" .. file .. "'." end

    print_table(distinfo_file, distinfo_table)
    distinfo_file:close()

    return true
end
