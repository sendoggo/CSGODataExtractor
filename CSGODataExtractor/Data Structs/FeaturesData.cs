using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGODataExtractor
{
    public class FeaturesData
    {
        public int tick { get; set; }
        public long steamID { get; set; }
        public string demoName { get; set; }
        /*public int weapon { get; set; }*/
        public int isHeadshot { get; set; }
        public int isWallbang { get; set; }
        public double targetDistance { get; set; }
        public double deltaViewAngleX { get; set; }
        public double deltaViewAngleY { get; set; }
        public double viewDiff { get; set; }
        public double viewDiffSpeed { get; set; }
        public uint label { get; set; }
    }

    public sealed class FeaturesDataMap : ClassMap<FeaturesData>
    {
        public FeaturesDataMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.label).Ignore();
        }
    }
}