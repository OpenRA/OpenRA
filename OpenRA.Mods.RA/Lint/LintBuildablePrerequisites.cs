using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
    class LintBuildablePrerequisites : ILintPass
    {
        public void Run(Action<string> emitError)
        {
            emitError("Hello World");
        }
    }
}
