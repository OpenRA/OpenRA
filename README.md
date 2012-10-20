# Project Description

[ZeroBrane Studio](http://studio.zerobrane.com/) is a lightweight Lua IDE with code completion, syntax
highlighting, remote debugger, code analyzer, live coding, and debugging
support for several Lua engines (LuaJIT, Löve 2D, Moai, Gideros, MobileLua,
and others).

## Features

* Written in Lua, so easily customizable.
* Small, portable, and cross-platform (Windows, Mac OSX, and Linux).
* Auto-completion for functions, keywords, and custom APIs.
* Interactive console to directly test code snippets with local and remote execution.
* Integrated debugger (with support for local and remote debugging).
* Live coding with Lua ([demo](http://notebook.kulchenko.com/zerobrane/live-coding-in-lua-bret-victor-style)), Löve 2D ([demo](http://notebook.kulchenko.com/zerobrane/live-coding-with-love)), and Gideros ([demo](http://notebook.kulchenko.com/zerobrane/gideros-live-coding-with-zerobrane-studio-ide)).
* Support for plugin-like components:
  - applications: overall control of applications settings;
  - specs (spec/): file syntax, lexer, keywords;
  - apis (api/): for code-completion and tool-tips;
  - interpreters (interpreters/): how a project is run;
  - config (cfg/): contains style and basic editor settings;
  - tools (tools/): additional tools.

## Screenshot

![ZeroBrane Studio debugger screenshot](http://studio.zerobrane.com/images/debugging.png)

## Usage

```
Open file(s):
  <zbstudio> <filename> [<filename>...]
  any non-option will be treated as filename

Overriding default configuration:
  <zbstudio> -cfg "<luacode overriding config>" [<filename>]
  e.g.: zbstudio -cfg "editor.fontsize=12" somefile.lua

Loading custom configuration:
  <zbstudio> -cfg "config/file.lua" [<filename>]
  e.g.: zbstudio -cfg cfg/estrela.lua
```

## Author

### ZeroBrane Studio and MobDebug

  **ZeroBrane LLC:** Paul Kulchenko (paul@kulchenko.com)

### Estrela Editor

  **Luxinia Dev:** Christoph Kubisch (crazybutcher@luxinia.de)

## License

See LICENSE file.
