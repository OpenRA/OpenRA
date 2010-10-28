using System.Collections.Generic;

namespace OpenRA.Traits
{
    public class ScaleInfo : ITraitInfo
    {
        public readonly float Value = 1f; /* default */

        public ScaleInfo() { }		/* only because we have other ctors */

        public object Create(ActorInitializer init) { return new Scale(init.self, this); }
    }

    public class Scale : IRenderModifier
    {
        Actor self;
        public ScaleInfo Info { get; protected set; }

        public Scale(Actor self, ScaleInfo info)
		{
			this.Info = info;
			this.self = self;
		}

        public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
        {
            var r2 = new List<Renderable>(r);
            var r3 = new List<Renderable>();

            for (int i = 0; i < r2.Count;i++)
            {
                var renderable = r2[i];

                renderable.Scale = Info.Value;
                r3.Add(renderable);
               // yield return renderable;
            }

            return r3;
        }
    }
}
