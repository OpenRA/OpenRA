# Project Description

[ZeroBrane Studio](http://studio.zerobrane.com/) is a lightweight cross-platform Lua IDE with code completion,
syntax highlighting, remote debugger, code analyzer, live coding,
and debugging support for several Lua engines
([Lua 5.1](http://studio.zerobrane.com/doc-lua-debugging),
[Lua 5.2](http://studio.zerobrane.com/doc-lua52-debugging),
[Lua 5.3](http://studio.zerobrane.com/doc-lua53-debugging),
[LuaJIT](http://studio.zerobrane.com/doc-luajit-debugging),
[LÖVE](http://notebook.kulchenko.com/zerobrane/love2d-debugging),
[Moai](http://notebook.kulchenko.com/zerobrane/moai-debugging-with-zerobrane-studio),
[Gideros](http://notebook.kulchenko.com/zerobrane/gideros-debugging-with-zerobrane-studio-ide),
[Corona](http://notebook.kulchenko.com/zerobrane/debugging-and-live-coding-with-corona-sdk-applications-and-zerobrane-studio),
[Marmalade Quick](http://notebook.kulchenko.com/zerobrane/marmalade-quick-debugging-with-zerobrane-studio),
[Cocos2d-x](http://notebook.kulchenko.com/zerobrane/cocos2d-x-simulator-and-on-device-debugging-with-zerobrane-studio),
[GSL-shell](http://notebook.kulchenko.com/zerobrane/gsl-shell-debugging-with-zerobrane-studio),
[Adobe Lightroom](http://notebook.kulchenko.com/zerobrane/debugging-lightroom-plugins-zerobrane-studio-ide),
[OpenResty/Nginx](http://notebook.kulchenko.com/zerobrane/debugging-openresty-nginx-lua-scripts-with-zerobrane-studio),
[Lapis](http://notebook.kulchenko.com/zerobrane/lapis-debugging-with-zerobrane-studio),
[Moonscript](http://notebook.kulchenko.com/zerobrane/moonscript-debugging-with-zerobrane-studio),
and others). It originated from the [Estrela Editor](http://www.luxinia.de/index.php/Estrela/).

![ZeroBrane Studio debugger screenshot](http://studio.zerobrane.com/images/debugging.png)

## Features

* Written in Lua, so easily customizable.
* Small, portable, and cross-platform (Windows, Mac OSX, and Linux).
* Auto-completion for functions, keywords, and custom APIs.
* Interactive console to directly test code snippets with local and remote execution.
* Integrated debugger with local and [remote debugging](http://studio.zerobrane.com/doc-remote-debugging)
for [Lua 5.1](http://studio.zerobrane.com/doc-lua-debugging),
[Lua 5.2](http://studio.zerobrane.com/doc-lua52-debugging),
[Lua 5.3](http://studio.zerobrane.com/doc-lua53-debugging),
[LuaJIT](http://studio.zerobrane.com/doc-luajit-debugging),
and [other Lua engines](http://studio.zerobrane.com/documentation#debugging).
* [Live coding](http://studio.zerobrane.com/documentation#live_coding)
with [Lua](http://notebook.kulchenko.com/zerobrane/live-coding-in-lua-bret-victor-style),
[LÖVE](http://notebook.kulchenko.com/zerobrane/live-coding-with-love),
[Gideros](http://notebook.kulchenko.com/zerobrane/gideros-live-coding-with-zerobrane-studio-ide),
[Moai](http://notebook.kulchenko.com/zerobrane/live-coding-with-moai-and-zerobrane-studio),
[Corona SDK](http://notebook.kulchenko.com/zerobrane/debugging-and-live-coding-with-corona-sdk-applications-and-zerobrane-studio),
GSL-shell, and other engines.
* Function outline.
* Fuzzy search with `Go To File`, `Go To Symbol`, and `Insert Library Function`.
* Several ways to extend the current functionality:
  - specs (`spec/`): specifications for file syntax, lexer, and keywords;
  - apis (`api/`): descriptions for [code completion and tooltips](http://studio.zerobrane.com/doc-api-auto-complete);
  - interpreters (`interpreters/`): components for setting debugging and run-time project environment;
  - packages (`packages/`): [plugins](http://studio.zerobrane.com/doc-plugin) that provide additional functionality;
  - config (`cfg/`): settings for styles, color themes, and other preferences;
  - translations (`cfg/i18n/`): [translations](http://studio.zerobrane.com/doc-translation) of the menus and messages to other languages;
  - tools (`tools/`): additional tools.

## Documentation

* A [short and simple overview](http://studio.zerobrane.com/doc-getting-started) for those who are new to this development environment.
* A list of [frequently asked questions](http://studio.zerobrane.com/doc-faq) about the IDE.
* [Tutorials and demos](http://studio.zerobrane.com/tutorials) that cover debugging and live coding for different environments.
* [Tips and tricks](http://studio.zerobrane.com/doc-tips-and-tricks).

## Installation

ZeroBrane Studio can be installed into and run from any folder.
No compilation is needed, although the scripts to compile required libraries for Windows, OSX, and Linux platforms are available in the `build/` folder.

## Usage

```
Open file(s):
  zbstudio [option] [<project directory>] <filename> [<filename>...]
  non-options are treated as a project directory to set or a file to open

Set project directory:
  zbstudio <project directory> [<filename>...]
  (0.39+) a directory passed as a parameter will be set as the project directory

Override default configuration:
  zbstudio -cfg "<luacode overriding config>" [<filename>]
  e.g.: zbstudio -cfg "editor.fontsize=12" somefile.lua

Load custom configuration:
  zbstudio -cfg path/file.lua [<filename>]
  e.g.: zbstudio -cfg cfg/estrela.lua
```

If you are loading a file, you can also request the cursor to be set on a particular line or at a particular position by using `filename:<line>` and `filename:p<pos>` syntax (0.71+).

## Contributing

See [CONTRIBUTING](CONTRIBUTING.md).

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

See [LICENSE](LICENSE).
