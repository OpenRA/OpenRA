using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Widgets;
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets {
	public class ObserverStatsWidget : ContainerWidget {
		public readonly int BarsTeamEmphasizeThickness;
		public readonly Color BarsTeamEmphasizeColor1;
		public readonly Color BarsTeamEmphasizeColor2;
		public readonly int BarsTeamsSpacing;
		public readonly int BarsNoTeamExtraSpacing;
		public readonly string DefaultSelectedOption;
	}
}
