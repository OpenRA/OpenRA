-- System functions

module ("dist.sys", package.seeall)

local cfg = require "dist.config"
local utils = require "dist.utils"
local lfs = require "lfs"

-- Return the path separator according to the platform.
function path_separator()
    if cfg.arch == "Windows" then
        return "\\"
    else
        return "/"
    end
end

-- Return path with wrong separators replaced with the right ones.
function check_separators(path)
    assert(type(path) == "string", "sys.check_separators: Argument 'path' is not a string.")
    if cfg.arch == "Windows" then
        return path:gsub("/", "\\")
    else
        return path
    end
end

-- Return the path with the unnecessary trailing separator removed.
function remove_trailing(path)
    assert(type(path) == "string", "sys.remove_trailing: Argument 'path' is not a string.")
    if path:sub(-1) == path_separator() and not is_root(path) then path = path:sub(1,-2) end
    return path
end

-- Return the path with the all occurences of '/.' or '\.' (representing
-- the current directory) removed.
function remove_curr_dir_dots(path)
    assert(type(path) == "string", "sys.remove_curr_dir_dots: Argument 'path' is not a string.")
    while path:match(path_separator() .. "%." .. path_separator()) do                       -- match("/%./")
        path = path:gsub(path_separator() .. "%." .. path_separator(), path_separator())    -- gsub("/%./", "/")
    end
    return path:gsub(path_separator() .. "%.$", "")                                         -- gsub("/%.$", "")
end

-- Return string argument quoted for a command line usage.
function quote(argument)
    assert(type(argument) == "string", "sys.quote: Argument 'argument' is not a string.")

    -- TODO: This seems like a not very nice hack. Why is it needed?
    --       Wouldn't it be better to fix the problem where it originates?
    -- replace '/' path separators for '\' on Windows
    if cfg.arch == "Windows" and argument:match("^[%u%U.]?:?[/\\].*") then
        argument = argument:gsub("//","\\"):gsub("/","\\")
    end

    -- Windows doesn't recognize paths starting with two slashes or backslashes
    -- so we double every backslash except for the first one
    if cfg.arch == "Windows" and argument:match("^[/\\].*") then
        local prefix = argument:sub(1,1)
        argument = argument:sub(2):gsub("\\",  "\\\\")
        argument = prefix .. argument
    else
        argument = argument:gsub("\\",  "\\\\")
    end
    argument = argument:gsub('"',  '\\"')

    return '"' .. argument .. '"'
end

-- Run the system command (in current directory).
-- Return true on success, nil on fail and log string.
-- When optional 'force_verbose' parameter is true, then the output will be shown
-- even when not in debug or verbose mode.
function exec(command, force_verbose)
    force_verbose = force_verbose or false
    assert(type(command) == "string", "sys.exec: Argument 'command' is not a string.")
    assert(type(force_verbose) == "boolean", "sys.exec: Argument 'force_verbose' is not a boolean.")

    if not (cfg.verbose or cfg.debug or force_verbose) then
        if cfg.arch == "Windows" then
            command = command .. " > NUL 2>&1"
        else
            command = command .. " > /dev/null 2>&1"
        end
    end

    if cfg.debug then print("Executing the command: " .. command) end
    local ok, str, status  = os.execute(command)

    -- os.execute returned values on failure are:
    --  nil or true, "exit", n or true, "signal", n for lua >= 5.2
    --  status ~= 0 for lua 5.x < 5.2
    if ok == nil or (str == "exit" and status ~= 0) or str == "signal" or (ok ~= 0 and ok ~= true) then
        return nil, "Error when running the command: " .. command
    else
        return true, "Sucessfully executed the command: " .. command
    end
end

-- Execute the 'command' and returns its output as a string.
function capture_output(command)
    assert(type(command) == "string", "sys.exec: Argument 'command' is not a string.")

    local executed, err = io.popen(command, "r")
    if not executed then return nil, "Error running the command '" .. command .. "':" .. err end

    local captured, err = executed:read("*a")
    if not captured then return nil, "Error reading the output of command '" .. command .. "':" .. err end

    executed:close()
    return captured
end

-- Return whether the path is a root.
function is_root(path)
    assert(type(path) == "string", "sys.is_root: Argument 'path' is not a string.")
    return utils.to_boolean(path:find("^[a-zA-Z]:[/\\]$") or path:find("^[/\\]$"))
end

-- Return whether the path is absolute.
function is_abs(path)
    assert(type(path) == "string", "sys.is_abs: Argument 'path' is not a string.")
    return utils.to_boolean(path:find("^[a-zA-Z]:[/\\].*$") or path:find("^[/\\].*$"))
end

-- Return whether the specified file or directory exists.
function exists(path)
    assert(type(path) == "string", "sys.exists: Argument 'path' is not a string.")
    local attr, err = lfs.attributes(path)
    return utils.to_boolean(attr), err
end

-- Return whether the 'file' exists and is a file.
function is_file(file)
    assert(type(file) == "string", "sys.is_file: Argument 'file' is not a string.")
    return lfs.attributes(file, "mode") == "file"
end

-- Return whether the 'dir' exists and is a directory.
function is_dir(dir)
    assert(type(dir) == "string", "sys.is_dir: Argument 'dir' is not a string.")
    return lfs.attributes(dir, "mode") == "directory"
end

-- Return the current working directory
function current_dir()
    local dir, err = lfs.currentdir()
    if not dir then return nil, err end
    return dir
end

-- Return an iterator over the directory 'dir'.
-- If 'dir' doesn't exist or is not a directory, return nil and error message.
function get_directory(dir)
    dir = dir or current_dir()
    assert(type(dir) == "string", "sys.get_directory: Argument 'dir' is not a string.")
    if is_dir(dir) then
        return lfs.dir(dir)
    else
        return nil, "Error: '".. dir .. "' is not a directory."
    end
end

-- Extract file or directory name from its path.
function extract_name(path)
    assert(type(path) == "string", "sys.extract_name: Argument 'path' is not a string.")
    if is_root(path) then return path end

    path = remove_trailing(path)
    path = path:gsub("^.*" .. path_separator(), "")
    return path
end

-- Return parent directory of the 'path' or nil if there's no parent directory.
-- If 'path' is a path to file, return the directory the file is in.
function parent_dir(path)
    assert(type(path) == "string", "sys.parent_dir: Argument 'path' is not a string.")
    path = remove_curr_dir_dots(path)
    path = remove_trailing(path)

    local dir = path:gsub(utils.escape_magic(extract_name(path)) .. "$", "")
    if dir == "" then
        return nil
    else
        return make_path(dir)
    end
end

-- Returns the table of all parent directories of 'path' up to the directory
-- specified by 'boundary_path' (exclusive).
function parents_up_to(path, boundary_path)
    assert(type(path) == "string", "sys.parents_up_to: Argument 'path' is not a string.")
    assert(type(boundary_path) == "string", "sys.parents_up_to: Argument 'boundary_path' is not a string.")
    boundary_path = remove_trailing(boundary_path)

    -- helper function to recursively collect the parent directories
    local function collect_parents(_path, _parents)
        local _parent = parent_dir(_path)
        if _parent and _parent ~= boundary_path then
            table.insert(_parents, _parent)
            return collect_parents(_parent, _parents)
        else
            return _parents
        end
    end

    return collect_parents(path, {})
end

-- Compose path composed from specified parts or current
-- working directory when no part specified.
function make_path(...)
    -- arg is deprecated in lua 5.2 in favor of table.pack we mimic here
    local arg = {n=select('#',...),...}
    local parts = arg
    assert(type(parts) == "table", "sys.make_path: Argument 'parts' is not a table.")

    local path, err
    if parts.n == 0 then
        path, err = current_dir()
    else
        path, err = table.concat(parts, path_separator())
    end
    if not path then return nil, err end

    -- squeeze repeated occurences of a file separator
    path = path:gsub(path_separator() .. "+", path_separator())

    -- remove unnecessary trailing path separator
    path = remove_trailing(path)

    return path
end

-- Return absolute path from 'path'
function abs_path(path)
    assert(type(path) == "string", "sys.get_abs_path: Argument 'path' is not a string.")
    if is_abs(path) then return path end

    local cur_dir, err = current_dir()
    if not cur_dir then return nil, err end

    return make_path(cur_dir, path)
end

-- Returns path to the temporary directory of OS.
function tmp_dir()
    return os.getenv("TMPDIR") or os.getenv("TEMP") or os.getenv("TMP") or "/tmp"
end

-- Returns temporary file (or directory) path (with optional prefix).
function tmp_name(prefix)
    prefix = prefix or ""
    assert(type(prefix) == "string", "sys.tmp_name: Argument 'prefix' is not a string.")
    return make_path(tmp_dir(), prefix .. "luadist_" .. utils.rand(10000000000))
end

-- Return table of all paths in 'dir'
function get_file_list(dir)
    dir = dir or current_dir()
    assert(type(dir) == "string", "sys.get_directory: Argument 'dir' is not a string.")
    if not exists(dir) then return nil, "Error getting file list of '" .. dir .. "': directory doesn't exist." end

    local function collect(path, all_paths)
        for item in get_directory(path) do

            local item_path = make_path(path, item)
            local _, last = item_path:find(dir .. path_separator(), 1, true)
            local path_to_insert = item_path:sub(last + 1)

            if is_file(item_path) then
                table.insert(all_paths, path_to_insert)
            elseif is_dir(item_path) and item ~= "." and item ~= ".." then
                table.insert(all_paths, path_to_insert)
                collect(item_path, all_paths)
            end
        end
    end

    local all_paths = {}
    collect(dir, all_paths)

    return all_paths
end

-- Return time of the last modification of 'file'.
function last_modification_time(file)
    assert(type(file) == "string", "sys.last_modification_time: Argument 'file' is not a string.")
    return lfs.attributes(file, "modification")
end

-- Return the current time (in seconds since epoch).
function current_time()
    return os.time()
end

-- Change the current working directory and return 'true' and previous working
-- directory on success and 'nil' and error message on error.
function change_dir(dir_name)
    assert(type(dir_name) == "string", "sys.change_dir: Argument 'dir_name' is not a string.")
    local prev_dir = current_dir()
    local ok, err = lfs.chdir(dir_name)
    if ok then
        return ok, prev_dir
    else
        return nil, err
    end
end

-- Make a new directory, making also all of its parent directories that doesn't exist.
function make_dir(dir_name)
    assert(type(dir_name) == "string", "sys.make_dir: Argument 'dir_name' is not a string.")
    if exists(dir_name) then
        return true
    else
        local par_dir = parent_dir(dir_name)
        if par_dir then
            local ok, err = make_dir(par_dir)
            if not ok then return nil, err end
        end
        return lfs.mkdir(dir_name)
    end
end

-- Move file (or directory) to the destination directory
function move_to(file_or_dir, dest_dir)
    assert(type(file_or_dir) == "string", "sys.move_to: Argument 'file_or_dir' is not a string.")
    assert(type(dest_dir) == "string", "sys.move_to: Argument 'dest_dir' is not a string.")
    assert(is_dir(dest_dir), "sys.move_to: Destination '" .. dest_dir .."' is not a directory.")

    -- Extract file/dir name from its path
    local file_or_dir_name = extract_name(file_or_dir)

    return os.rename(file_or_dir, make_path(dest_dir, file_or_dir_name))
end

-- rename file (or directory) to the new name.
function rename(file, new_name)
    assert(type(file) == "string", "sys.rename: Argument 'file' is not a string.")
    assert(type(new_name) == "string", "sys.rename: Argument 'new_name' is not a string.")
    assert(not exists(new_name), "sys.rename: desired filename already exists.")

    return os.rename(file, new_name)
end

-- Copy 'source' to the destination directory 'dest_dir'.
-- If 'source' is a directory, then recursive copying is used.
-- For non-recursive copying of directories use the make_dir() function.
function copy(source, dest_dir)
    assert(type(source) == "string", "sys.copy: Argument 'file_or_dir' is not a string.")
    assert(type(dest_dir) == "string", "sys.copy: Argument 'dest_dir' is not a string.")
    assert(is_dir(dest_dir), "sys.copy: destination '" .. dest_dir .."' is not a directory.")

    if cfg.arch == "Windows" then
        if is_dir(source) then
            make_dir(make_path(dest_dir, extract_name(source)))
            return exec("xcopy /E /I /Y /Q " .. quote(source) .. " " .. quote(dest_dir .. "\\" .. extract_name(source)))
        else
            return exec("copy /Y " .. quote(source) .. " " .. quote(dest_dir))
        end
    else
        if is_dir(source) then
            return exec("cp -fRH " .. quote(source) .. " " .. quote(dest_dir))
        else
            return exec("cp -fH " .. quote(source) .. " " .. quote(dest_dir))
        end
    end
end

-- Delete the specified file or directory
function delete(path)
    assert(type(path) == "string", "sys.delete: Argument 'path' is not a string.")
    assert(is_abs(path), "sys.delete: Argument 'path' is not an absolute path.")

    if cfg.arch == "Windows" then
        if not exists(path) then
            return true
        elseif is_file(path) then
            return os.remove(path)
        else
            return exec("rd /S /Q " .. quote(path))
        end
    else
        return exec("rm -rf " .. quote(path))
    end
end
