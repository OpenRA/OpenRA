using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using IjwFramework.Types;

namespace OpenRa.Game
{
    class SharedResources
    {
        static Lazy<IniFile> rules = new Lazy<IniFile>( () => new IniFile( FileSystem.Open( "rules.ini" )));
        public static IniFile Rules { get { return rules.Value; } }
    }
}
