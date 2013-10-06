# Project Description

[ZeroBrane Studio](http://studio.zerobrane.com/) is a lightweight Lua IDE with code completion, syntax
highlighting, remote debugger, code analyzer, live coding, and debugging
support for several Lua engines (LuaJIT, Löve 2D, Moai, Gideros, Corona, Marmalade Quick,
MobileLua, GSL-shell, and others). It originated from the [Estrela Editor](http://www.luxinia.de/index.php/Estrela/).

## Features

* Written in Lua, so easily customizable.
* Small, portable, and cross-platform (Windows, Mac OSX, and Linux).
* Auto-completion for functions, keywords, and custom APIs.
* Interactive console to directly test code snippets with local and remote execution.
* Integrated debugger (with support for local and remote debugging).
* Live coding with Lua ([demo](http://notebook.kulchenko.com/zerobrane/live-coding-in-lua-bret-victor-style)), Löve 2D ([demo](http://notebook.kulchenko.com/zerobrane/live-coding-with-love)), Gideros ([demo](http://notebook.kulchenko.com/zerobrane/gideros-live-coding-with-zerobrane-studio-ide)), Moai ([demo](http://notebook.kulchenko.com/zerobrane/live-coding-with-moai-and-zerobrane-studio)), and Corona SDK ([demo](http://notebook.kulchenko.com/zerobrane/debugging-and-live-coding-with-corona-sdk-applications-and-zerobrane-studio)).
* Support for plugin-like components:
  - specs (spec/): file syntax, lexer, keywords (e.g. glsl);
  - apis (api/): for code-completion and tool-tips;
  - interpreters (interpreters/): how a project is run;
  - config (cfg/): contains style and basic editor settings;
  - tools (tools/): additional tools (e.g. DirectX/Cg shader compiler...).

## Documentation

* A [short and simple overview](http://studio.zerobrane.com/doc-getting-started.html) for those who are new to this development environment.
* A list of [frequently asked questions](http://studio.zerobrane.com/doc-faq.html) about the IDE.
* [Tutorials and demos](http://studio.zerobrane.com/tutorials.html) that cover debugging and live coding for different environments.

## Screenshot

![ZeroBrane Studio debugger screenshot](http://studio.zerobrane.com/images/debugging.png)

## Usage

```
Open file(s):
  zbstudio <filename> [<filename>...]
  any non-option will be treated as filename

Set project directory:
  zbstudio <project directory> [<filename>...]
  (0.39+) a directory passed as a parameter will be set as the project directory

Overriding default configuration:
  zbstudio -cfg "<luacode overriding config>" [<filename>]
  e.g.: zbstudio -cfg "editor.fontsize=12" somefile.lua

Loading custom configuration:
  zbstudio -cfg path/file.lua [<filename>]
  e.g.: zbstudio -cfg cfg/estrela.lua
```

## Author

### ZeroBrane Studio and MobDebug

  **ZeroBrane LLC:** Paul Kulchenko (paul@kulchenko.com)

### Estrela Editor

  **Luxinia Dev:** Christoph Kubisch (crazybutcher@luxinia.de)

## Where is Estrela?

The projects have been merged and ZeroBrane Studio will lead the future.
Please reassociate files with ZeroBrane Studio. To keep your history of files
and projects copy the contents of the `EstrelaEditor.ini` in your HOME
directory to `ZeroBraneStudio.ini`. If you have used Estrela for graphics
shader authoring or luxinia, create/modify the `cfg/user.lua` to include the
content of `cfg/estrela.lua` to load all tools and specifications by default
again.
  
## License

See LICENSE file.
