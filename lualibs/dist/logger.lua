-- Simple logger for LuaDist.

module ("dist.logger", package.seeall)

local cfg = require "dist.config"
local sys = require "dist.sys"

-- Open 'log_file' and return a log, or nil and error msg on error.
local function get_log(log_file)
    log_file = log_file or cfg.log_file
    assert(type(log_file) == "string", "log.get_log: Argument 'log_file' is not a string.")
    log_file = sys.abs_path(log_file)

    sys.make_dir(sys.parent_dir(log_file))
    local log, err = io.open(log_file, "a")
    if not log then
        return nil, "Error: can't open a logfile '" .. log_file .. "': " .. err
    else
        return log
    end
end

-- Set the default log.
local log_file = get_log(cfg.log_file)

-- Log levels used.
local log_levels = {
    DEBUG = 0, -- Fine-grained informational events that are most useful to debug an application.
    INFO  = 1, -- Informational messages that highlight the progress of the application at coarse-grained level.
    WARN  = 2, -- Potentially harmful situations.
    ERROR = 3, -- Error events that might still allow the application to continue running.
    FATAL = 4, -- Very severe error events that would presumably lead the application to abort.
}

-- Write 'message' with 'level' to 'log'.
local function write(level, ...)
    assert(type(level) == "string", "log.write: Argument 'level' is not a string.")
    assert(#arg > 0, "log.write: No message arguments provided.")
    assert(type(log_levels[level]) == "number", "log.write: Unknown log level used: '" .. level .. "'.")

    level = level:upper()
    local message = table.concat(arg, " ")

    -- Check if writing for this log level is enabled.
    if cfg.write_log_level and log_levels[level] >= log_levels[cfg.write_log_level] then
        log_file:write(os.date("%Y-%m-%d %H:%M:%S") .. " [" .. level .. "]\t" .. message .. "\n")
        log_file:flush()
    end

    -- Check if printing for this log level is enabled.
    if cfg.print_log_level and log_levels[level] >= log_levels[cfg.print_log_level] then
        print(message)
    end
end

-- Functions with defined log levels for simple use.
function debug(...) return write("DEBUG", ...) end
function info(...) return write("INFO", ...) end
function warn(...) return write("WARN", ...) end
function error(...) return write("ERROR", ...) end
function fatal(...) return write("FATAL", ...) end

-- Function with explicitly specified log level.
function log(level, ...) return write(level, ...) end
