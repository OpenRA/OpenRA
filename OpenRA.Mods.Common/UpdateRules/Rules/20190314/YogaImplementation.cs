using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
    public class YogaImplementation : UpdateRule
    {
        public override string Name { get { return "Implementation of Facebook Yoga layout engine for widgets."; } }
        public override string Description
        {
            get
            {
                return "Renamed old layout properties to ensure compatibility before being dropped in the future.";
            }
        }

        public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNode chromeNode)
        {
            var positionType = chromeNode.LastChildMatching("PositionType");
            if (positionType == null)
                chromeNode.AddNode(new MiniYamlNode("PositionType", "Absolute"));

            var x = chromeNode.LastChildMatching("X");
            if (x != null)
                x.RenameKey("ClassicX");

            var y = chromeNode.LastChildMatching("Y");
            if (y != null)
                y.RenameKey("ClassicY");

            var width = chromeNode.LastChildMatching("Width");
            if (width != null)
                width.RenameKey("ClassicWidth");

            var height = chromeNode.LastChildMatching("Height");
            if (height != null)
                height.RenameKey("ClassicHeight");

            var children = chromeNode.LastChildMatching("Children");
            if (children != null)
                foreach (var child in children.Value.Nodes)
                    UpdateChromeNode(modData, child);

            yield break;
        }
    }
}
