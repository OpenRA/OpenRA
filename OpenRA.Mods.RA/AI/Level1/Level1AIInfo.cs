using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// The info object of the Level1AI.
    /// </summary>
    internal class Level1AIInfo : IBotInfo, ITraitInfo
    {
        /// <summary>
        /// The default name of the AI, overriden in System.yaml AI configurations.
        /// </summary>
        public string Name = "Level 1 AI";

        #region Settings
        /// <summary>
        /// The default squad size for an attack move, override in the System.yaml AI configuration
        /// </summary>
        public int SquadSize = 8;

        /// <summary>
        /// The fraction of units to build.
        /// </summary>
        [FieldLoader.LoadUsing("LoadUnits")]
        public Dictionary<string, float> UnitsToBuild = null;

        /// <summary>
        /// The fractions of buildings to build
        /// </summary>
        [FieldLoader.LoadUsing("LoadBuildings")]
        public Dictionary<string, float> BuildingFractions = null;

        /// <summary>
        /// Flag indicating whether the buildings should be repaired.
        /// </summary>
        public bool ShouldRepairBuildings = true;
        #endregion

        #region IBotInfo Members
        /// <summary>
        /// Returns the AI name
        /// </summary>
        string IBotInfo.Name { get { return this.Name; } }
        #endregion

        #region ITraitInfo Members
        /// <summary>
        /// Creates the actor for the given info element
        /// </summary>
        /// <param name="init"></param>
        /// <returns></returns>
        public object Create(ActorInitializer init)
        {
            return new Level1AI(this);
        }
        #endregion

        #region YAML Helper
        /// <summary>
        /// YAML Converter
        /// </summary>
        /// <param name="y"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private static object LoadActorList(MiniYaml y, string field)
        {
            if (y == null)
            {
                throw new ArgumentNullException("y");
            }
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentException("field cannot be empty", "field");
            }

            return y.NodesDict[field].Nodes.ToDictionary(
                t => t.Key,
                t => FieldLoader.GetValue<float>(field, t.Value.Value));
        }

        /// <summary>
        /// YAML Loader for UnitsToBuild
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static object LoadUnits(MiniYaml y)
        {
            return LoadActorList(y, "UnitsToBuild");
        }

        /// <summary>
        /// YAML Loader for BuildingFractions
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static object LoadBuildings(MiniYaml y)
        {
            return LoadActorList(y, "BuildingFractions");
        }
        #endregion
    }
}
