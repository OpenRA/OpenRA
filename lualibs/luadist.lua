#!/usr/bin/env lua

-- Command line interface to LuaDist-git.

local dist = require "dist"
local utils = require "dist.utils"
local depends = require "dist.depends"
local package = require "dist.package"
local mf = require "dist.manifest"
local cfg = require "dist.config"
local sys = require "dist.sys"

-- CLI commands of Luadist.
local commands
commands = {

    -- Print help for this command line interface.
    ["help"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] <COMMAND> [ARGUMENTS...] [-VARIABLES...]

    Commands:

        help      - print this help
        install   - install modules
        remove    - remove modules
        refresh   - update information about modules in repositories
        list      - list installed modules
        info      - show information about modules
        search    - search repositories for modules
        fetch     - download modules
        make      - manually deploy modules from local paths
        upload    - upload installed modules to their repositories
        tree      - print dependency tree of a module
        selftest  - run the selftest of LuaDist

    To get help on specific command, run:

        luadist help <COMMAND>
        ]],
        run = function (deploy_dir, help_item)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            help_item = help_item or {}
            assert(type(deploy_dir) == "string", "luadist.help: Argument 'deploy_dir' is not a string.")
            assert(type(help_item) == "table", "luadist.help: Argument 'help_item' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            if not help_item or not commands[help_item[1]] then
                help_item = "help"
            else
                help_item = help_item[1]
            end

            print_info()
            print(commands[help_item].help)
            return 0
        end
    },

    -- Install modules.
    ["install"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] install MODULES... [-VARIABLES...]

    The 'install' command will install specified MODULES to
    DEPLOYMENT_DIRECTORY. LuaDist will also automatically resolve, download
    and install all dependencies.

    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used.

    You can use * (an asterisk sign) in the name of the module as a wildcard
    with the meaning 'any symbols' (in most shells, the module name then must
    be quoted to prevent the expansion of asterisk by the shell itself).

    Optional CMake VARIABLES in -D format (e.g. -Dvariable=value) or LuaDist
    configuration VARIABLES (e.g. -variable=value) can be specified.

    The -simulate configuration option makes LuaDist only to simulate the
    installation of modules (no modules will be really installed).
        ]],

        run = function (deploy_dir, modules, cmake_variables)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            if type(modules) == "string" then modules = {modules} end
            cmake_variables = cmake_variables or {}
            assert(type(deploy_dir) == "string", "luadist.install: Argument 'deploy_dir' is not a string.")
            assert(type(modules) == "table", "luadist.install: Argument 'modules' is not a string or table.")
            assert(type(cmake_variables) == "table", "luadist.install: Argument 'cmake_variables' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            if cfg.simulate then
                print("NOTE: this is just simulation.")
            end

            if #modules == 0 then
                print("No modules to install specified.")
                return 0
            end

            local ok, err = dist.install(modules, deploy_dir, cmake_variables)
            if not ok then
                print(err)
                os.exit(1)
            else
               print((cfg.simulate and "Simulated installation" or "Installation") .. " successful.")
               return 0
            end
        end
    },

    -- Remove modules.
    ["remove"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] remove MODULES... [-VARIABLES...]

    The 'remove' command will remove specified MODULES from
    DEPLOYMENT_DIRECTORY. If no module is specified, all modules
    will be removed.

    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used. If no MODULES are specified, all installed modules
    will be removed.

    You can use * (an asterisk sign) in the name of the module as a wildcard
    with the meaning 'any symbols' (in most shells, the module name then must
    be quoted to prevent the expansion of asterisk by the shell itself).

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.

    WARNING: dependencies between modules are NOT taken into account when
    removing modules!
        ]],

        run = function (deploy_dir, modules)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            if type(modules) == "string" then modules = {modules} end
            assert(type(deploy_dir) == "string", "luadist.remove: Argument 'deploy_dir' is not a string.")
            assert(type(modules) == "table", "luadist.remove: Argument 'modules' is not a string or table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local num, err = dist.remove(modules, deploy_dir)
            if not num then
                print(err)
                os.exit(1)
            else
               print("Removed modules: " .. num)
               return 0
            end
        end
    },

    -- Update repositories.
    ["refresh"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] refresh [-VARIABLES...]

    The 'refresh' command will update information about modules in all software
    repositories of specified DEPLOYMENT_DIRECTORY. Also, the cached dependency
    manifest, built from previous installations or invocations of 'tree'
    functionality will be deleted.

    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            assert(type(deploy_dir) == "string", "luadist.refresh: Argument 'deploy_dir' is not a string.")
            deploy_dir = sys.abs_path(deploy_dir)

            -- TODO: should be deleting the dep_manifest decoupled from refreshing the repository info?
            -- delete cached dependency manifest
            local dep_manifest_file = sys.abs_path(sys.make_path(deploy_dir, cfg.dep_cache_file))
            local dep_mf_deleted = false
            if sys.exists(dep_manifest_file) then
                sys.delete(dep_manifest_file)
                dep_mf_deleted = true
            end

            -- refresh repository information
            local ok, err = dist.update_manifest(deploy_dir)
            if not ok then
                print(err)
                os.exit(1)
            else
               print("Repositories successfuly updated" .. (dep_mf_deleted and " and dependency cache deleted" or "") .. ".")
               return 0
            end
        end
    },

    -- Manually deploy modules.
    ["make"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] make MODULE_PATHS... [-VARIABLES...]

    The 'make' command will manually deploy modules from specified local
    MODULE_PATHS into the DEPLOYMENT_DIRECTORY.

    The MODULE_PATHS will be preserved. If DEPLOYMENT_DIRECTORY is not
    specified, the deployment directory of LuaDist is used.

    Optional CMake VARIABLES in -D format (e.g. -Dvariable=value) or LuaDist
    configuration VARIABLES (e.g. -variable=value) can be specified.

    The -simulate configuration option makes LuaDist only to simulate the
    deployment of modules (no modules will be really deployed).

    WARNING: this command does NOT check whether the dependencies of deployed
    modules are satisfied or not!
        ]],

        run = function (deploy_dir, module_paths, cmake_variables)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            module_paths = module_paths or {}
            cmake_variables = cmake_variables or {}
            assert(type(deploy_dir) == "string", "luadist.make: Argument 'deploy_dir' is not a string.")
            assert(type(module_paths) == "table", "luadist.make: Argument 'module_paths' is not a table.")
            assert(type(cmake_variables) == "table", "luadist.make: Argument 'cmake_variables' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            if cfg.simulate then
                print("NOTE: this is just simulation.")
            end

            if #module_paths == 0 then
                print("No module paths to deploy specified.")
                return 0
            end

            local ok, err = dist.make(deploy_dir, module_paths, cmake_variables)
            if not ok then
                print(err)
                os.exit(1)
            end
            print((cfg.simulate and "Simulated deployment" or "Deployment") .. " successful.")
            return 0
        end
    },

    -- Download modules.
    ["fetch"] = {
        help = [[
Usage: luadist [FETCH_DIRECTORY] fetch MODULES... [-VARIABLES...]

    The 'fetch' command will download specified MODULES to the FETCH_DIRECTORY.

    If no FETCH_DIRECTORY is specified, the temporary directory of LuaDist
    deployment directory (i.e. ']] .. cfg.temp_dir .. [[') is used.
    If the version is not specified in module name, the most recent version
    available will be downloaded.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (fetch_dir, modules)
            fetch_dir = fetch_dir or dist.get_deploy_dir()
            modules = modules or {}
            assert(type(fetch_dir) == "string", "luadist.fetch: Argument 'fetch_dir' is not a string.")
            assert(type(modules) == "table", "luadist.fetch: Argument 'modules' is not a table.")
            fetch_dir = sys.abs_path(fetch_dir)

            -- if the default parameter (i.e. deploy_dir) is passed, use the default temp_dir
            if fetch_dir == dist.get_deploy_dir() then
                fetch_dir = sys.make_path(fetch_dir, cfg.temp_dir)
            end

            if #modules == 0 then
                print("No modules to download specified.")
                return 0
            end

            local ok, err = dist.fetch(modules, fetch_dir)
            if not ok then
                print(err)
                os.exit(1)
            else
                print("Modules successfuly downloaded to '" .. fetch_dir .. "'.")
                return 0
            end
        end
    },

    -- Upload modules.
    ["upload"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] upload MODULES... [-VARIABLES...]

    The 'upload' command will upload the binary versions of specified MODULES,
    installed in the DEPLOYMENT_DIRECTORY, to their LuaDist repositories.

    Base url of repositories is given by configuration variable 'upload_url'
    (by default ']] .. cfg.upload_url .. [[') which you can change.
    E.g.: Binary version of module 'lua', installed in DEPLOYMENT_DIRECTORY,
    will now be uploaded to repository ']] .. cfg.upload_url .. [[lua.git'.

    Organization of uploaded modules and their repositories is subject
    to the conventions described in more detail in the source code
    of the 'dist.upload_modules()' function (file 'dist/init.lua').

    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used. If no MODULES are specified, all installed modules
    will be uploaded.

    You can use * (an asterisk sign) in the name of the module as a wildcard
    with the meaning 'any symbols' (in most shells, the module name then must
    be quoted to prevent the expansion of asterisk by the shell itself).

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir, modules)
            -- check if we have git
            local ok = utils.system_dependency_available("git", "git --version")
            if not ok then os.exit(1) end

            deploy_dir = deploy_dir or dist.get_deploy_dir()
            if type(modules) == "string" then modules = {modules} end
            assert(type(deploy_dir) == "string", "luadist.upload: Argument 'deploy_dir' is not a string.")
            assert(type(modules) == "table", "luadist.upload: Argument 'modules' is not a string or table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local num, err = dist.upload_modules(deploy_dir, modules, cfg.upload_url)
            if not num then
                print(err)
                os.exit(1)
            else
               print("Uploaded modules: " .. num)
               return 0
            end
        end
    },

    -- List installed modules.
    ["list"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] list [STRINGS...] [-VARIABLES...]

    The 'list' command will list all modules installed in specified
    DEPLOYMENT_DIRECTORY, which contain one or more optional STRINGS.

    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used. If STRINGS are not specified, all installed modules
    are listed.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir, strings)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            strings = strings or {}
            assert(type(deploy_dir) == "string", "luadist.list: Argument 'deploy_dir' is not a string.")
            assert(type(strings) == "table", "luadist.list: Argument 'strings' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local deployed = dist.get_deployed(deploy_dir)
            deployed  = depends.filter_packages_by_strings(deployed, strings)

            print("\nInstalled modules:")
            print("==================\n")
            for _, pkg in pairs(deployed) do
                print("  " .. pkg.name .. "-" .. pkg.version .. "\t(" .. pkg.arch .. "-" .. pkg.type .. ")" .. (pkg.provided_by and "\t [provided by " .. pkg.provided_by .. "]" or ""))
            end
            print()
            return 0
        end
    },

    -- Search for modules in repositories.
    ["search"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] search [STRINGS...] [-VARIABLES...]

    The 'search' command will list all modules from repositories, which contain
    one or more STRINGS.

    If no STRINGS are specified, all available modules are listed.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir, strings)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            strings = strings or {}
            assert(type(deploy_dir) == "string", "luadist.search: Argument 'deploy_dir' is not a string.")
            assert(type(strings) == "table", "luadist.search: Argument 'strings' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local available, err = mf.get_manifest()
            if not available then
                print(err)
                os.exit(1)
            end

            available = depends.filter_packages_by_strings(available, strings)
            available = depends.sort_by_names(available)

            print("\nModules found:")
            print("==============\n")
            for _, pkg in pairs(available) do
                print("  " .. pkg.name)
            end
            print()
            return 0
        end
    },

    -- Show information about modules.
    ["info"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] info [MODULES...] [-VARIABLES...]

    The 'info' command shows information about specified modules from
    repositories. This command also shows whether modules are installed
    in DEPLOYMENT_DIRECTORY.

    If no MODULES are specified, all available modules are shown.
    If DEPLOYMENT_DIRECTORY is not specified, the deployment directory
    of LuaDist is used.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir, modules)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            modules = modules or {}
            assert(type(deploy_dir) == "string", "luadist.info: Argument 'deploy_dir' is not a string.")
            assert(type(modules) == "table", "luadist.info: Argument 'modules' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local manifest, err = mf.get_manifest()
            if not manifest then
                print(err)
                os.exit(1)
            end

            -- if no packages specified explicitly, show just info from .gitmodules for all packages available
            if #modules == 0 then

                modules = manifest
                modules = depends.sort_by_names(modules)
                local deployed = dist.get_deployed(deploy_dir)

                print("")
                for _, pkg in pairs(modules) do
                    print("  " .. pkg.name)
                    print("  Repository url: " .. (pkg.path or "N/A"))
                    print()
                end
                return 0

            -- if some packages explicitly specified, retrieve and show detailed info about them
            else

                if #modules > 5 then
                    print("NOTE: More than 5 modules specified - operation may take a longer time.")
                end

                local deployed = dist.get_deployed(deploy_dir)

                for _, module in pairs(modules) do
                    manifest, err = package.get_versions_info(module, manifest, deploy_dir, deployed)
                    if not manifest then
                        print(err)
                        os.exit(1)
                    end
                end

                modules = depends.find_packages(modules, manifest)
                modules = depends.sort_by_names(modules)

                print("")
                for _, pkg in pairs(modules) do
                    print("  " .. pkg.name .. "-" .. pkg.version .. "  (" .. pkg.arch .. "-" .. pkg.type ..")" .. (pkg.from_installed and "  [info taken from installed version]" or ""))
                    print("  Description: " .. (pkg.desc or "N/A"))
                    print("  Author: " .. (pkg.author or "N/A"))
                    print("  Homepage: " .. (pkg.url or "N/A"))
                    print("  License: " .. (pkg.license or "N/A"))
                    print("  Repository url: " .. (pkg.path or "N/A"))
                    print("  Maintainer: " .. (pkg.maintainer or "N/A"))
                    if pkg.provides then print("  Provides: " .. utils.table_tostring(pkg.provides)) end
                    if pkg.depends then print("  Depends: " .. utils.table_tostring(pkg.depends)) end
                    if pkg.conflicts then print("  Conflicts: " .. utils.table_tostring(pkg.conflicts)) end
                    print("  State: " .. (depends.is_installed(pkg.name, deployed, pkg.version) and "installed" or "not installed"))
                    print()
                end
                return 0
            end

        end
    },

    -- Print dependency tree.
    ["tree"] = {
        help = [[
Usage: luadist [DEPLOYMENT_DIRECTORY] tree [MODULES...] [-VARIABLES...]

    The 'tree' command prints dependency tree for specified modules.

    If no MODULES are specified, trees for all available modules are printed.
    This information about modules is being cached in dependency manifest.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (deploy_dir, modules)
            deploy_dir = deploy_dir or dist.get_deploy_dir()
            modules = modules or {}
            assert(type(deploy_dir) == "string", "luadist.info: Argument 'deploy_dir' is not a string.")
            assert(type(modules) == "table", "luadist.info: Argument 'modules' is not a table.")
            deploy_dir = sys.abs_path(deploy_dir)

            local manifest, err = mf.get_manifest()
            if not manifest then
                print(err)
                os.exit(1)
            end

            -- if no modules specified explicitly, assume all modules
            if #modules == 0 then modules = depends.sort_by_names(manifest) end
            print("Getting dependency information... (this may take a lot of time)")

            for _, module in pairs(modules) do

                -- if all modules are being queried, extract the name
                if type(module) == "table" then module = module.name end

                local dep_manifest, err = dist.dependency_info(module, deploy_dir)
                if not dep_manifest then
                    print(err)
                    os.exit(1)
                else

                    -- print the dependency tree
                    local heading = "Dependency tree for '" .. module .. "' (on " .. cfg.arch .. "-" .. cfg.type .. "):"
                    print("\n" .. heading .. "")
                    print(string.rep("=", #heading) .. "\n")

                    for _, pkg in pairs(dep_manifest) do

                        local pkg_version, pkg_tag = pkg.version, pkg.version
                        if pkg.was_scm_version then
                            pkg_version, pkg_tag = "scm", "HEAD"
                        end
                        print("  " .. pkg.name .. "-" .. pkg_version .. " (" .. pkg.path .. ", " .. pkg_tag .. ")")
                        if pkg.depends then
                            for _, dep in pairs(pkg.depends) do
                                if type(dep) ~= "table" then
                                    local found = depends.sort_by_versions(depends.find_packages(dep, dep_manifest))[1]
                                    if not found then
                                        print("Could not find the dependency '" .. dep .. "' in the dependency manifest.")
                                        os.exit(1)
                                    end
                                    print("    * " .. found.name .. "-" .. found.version .. " (" .. found.path .. ", " .. found.version .. ")")
                                end
                            end
                        end
                        print()
                    end

                end
            end
            return 0

        end
    },

    -- Selftest of LuaDist.
    ["selftest"] = {
        help = [[
Usage: luadist [TEST_DIRECTORY] selftest [-VARIABLES...]

    The 'selftest' command runs tests of LuaDist, located in TEST_DIRECTORY and
    displays the results.

    If no TEST_DIRECTORY is specified, the default test directory of LuaDist
    deployment directory (i.e. ']] .. cfg.test_dir .. [[') is used.

    Optional LuaDist configuration VARIABLES (e.g. -variable=value) can be
    specified.
        ]],

        run = function (test_dir)
            test_dir = test_dir or dist.get_deploy_dir()
            assert(type(test_dir) == "string", "luadist.selftest: Argument 'deploy_dir' is not a string.")
            test_dir = sys.abs_path(test_dir)

            -- if the default parameter (i.e. deploy_dir) is passed, use the default test_dir
            if test_dir == dist.get_deploy_dir() then
                test_dir = sys.make_path(test_dir, cfg.test_dir)
            end

            -- try to get an iterator over test files and check it
            local test_iterator, err = sys.get_directory(test_dir)
            if not test_iterator then
                print("Running tests from '" .. test_dir .. "' failed: " .. err)
                os.exit(1)
            end

            -- run the tests
            print("\nRunning tests:")
            print("==============")
            for test_file in sys.get_directory(test_dir) do
                test_file = sys.make_path(test_dir, test_file)
                if sys.is_file(test_file) then
                    print()
                    print(sys.extract_name(test_file) .. ":")
                    dofile(test_file)
                end
            end
            print()
            return 0
        end
    },
}

-- Run the functionality of LuaDist 'command' in the 'deploy_dir' with other items
-- or settings/variables starting at 'other_idx' index of special variable 'arg'.
local function run_command(deploy_dir, command, other_idx)
    deploy_dir = deploy_dir or dist.get_deploy_dir()
    assert(type(deploy_dir) == "string", "luadist.run_command: Argument 'deploy_dir' is not a string.")
    assert(type(command) == "string", "luadist.run_command: Argument 'command' is not a string.")
    assert(not other_idx or type(other_idx) == "number", "luadist.run_command: Argument 'other_idx' is not a number.")
    deploy_dir = sys.abs_path(deploy_dir)

    local items = {}
    local cmake_variables = {}

    -- parse items after the command (and LuaDist or CMake variables)
    if other_idx then
        for i = other_idx, #arg do

            -- CMake variable
            if arg[i]:match("^%-D(.-)=(.*)$") then
                local variable, value = arg[i]:match("^%-D(.-)=(.*)$")
                cmake_variables[variable] = value

            -- LuaDist variable
            elseif arg[i]:match("^%-(.-)=(.*)$") then
                local variable, value = arg[i]:match("^%-(.-)=(.*)$")
                apply_settings(variable, value)

            -- LuaDist boolean variable with implicit 'true' value
            elseif arg[i]:match("^%-(.-)$") then
                local variable, value = arg[i]:match("^%-(.-)$")
                apply_settings(variable, "true")

            -- not a LuaDist or CMake variable
            else
                table.insert(items, arg[i])
            end
        end
    end

    -- run the required LuaDist functionality
    return commands[command].run(sys.abs_path(deploy_dir), items, cmake_variables)
end

-- Print information about Luadist (version, license, etc.).
function print_info()
    print([[
LuaDist-git ]].. cfg.version .. [[ - Lua package manager for the LuaDist deployment system.
Released under the MIT License. See https://github.com/luadist/luadist-git
          ]])
    return 0
end

-- Convenience function for printing the main luadist help.
function print_help()
    return run_command(nil, "help")
end

-- Set the LuaDist 'variable' to the 'value'.
-- See available settings in 'dist.config' module.
function apply_settings(variable, value)
    assert(type(variable) == "string", "luadist.apply_settings: Argument 'variable' is not a string.")
    assert(type(value) == "string", "luadist.apply_settings: Argument 'value' is not a string.")

    -- check whether the settings variable exists
    if cfg[variable] == nil then
        print("Unknown LuaDist configuration option: '" .. variable .. "'.")
        os.exit(1)

    -- ensure the right type

    elseif type(cfg[variable]) == "boolean" then
        value = value:lower()
        if value == "true" or value == "yes" or value == "on" or value == "1" then
            value = true
        elseif value == "false" or value == "no" or value == "off" or value == "0" then
            value = false
        else
            print("Value of LuaDist option '" .. variable .. "' must be a boolean.")
            os.exit(1)
        end

    elseif type(cfg[variable]) == "number" then
        value = tonumber(value)
        if not value then
            print("Value of LuaDist option '" .. variable .. "' must be a number.")
            os.exit(1)
        end

    elseif type(cfg[variable]) == "table" then
        local err
        value, err = utils.make_table(value, ",")
        if not value then
            print("Error when parsing the LuaDist variable '" .. variable .. "': " .. err)
            os.exit(1)
        end
    end

    -- set the LuaDist variable
    cfg[variable] = value

end

-- Parse command line input and run the required command.
if pcall(debug.getlocal, 4, 1) then
    return commands -- return commands when used as module
elseif not commands[arg[1]] and commands[arg[2]] then
    -- deploy_dir specified
    return run_command(arg[1], arg[2], 3)
elseif commands[arg[1]] then
    -- deploy_dir not specified
    return run_command(dist.get_deploy_dir(), arg[1], 2)
else
    -- unknown command
    if arg[1] then
        print("Unknown command '" .. arg[1] .. "'. Printing help...\n")
        print_help()
        os.exit(1)
    end
    return print_help()
end
