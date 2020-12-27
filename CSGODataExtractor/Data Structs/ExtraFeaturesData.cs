using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGODataExtractor.Data_Structs
{
    public class ExtraFeaturesData
    {
        public int tick { get; set; }
        public long steamID { get; set; }
        public string demoName { get; set; }
        /*public int weapon { get; set; }*/
        public int isHeadshot { get; set; }
        public int isWallbang { get; set; }
        public double targetDistance { get; set; }
        public double viewDiff { get; set; }
        public double viewDiff1 { get; set; }
        public double viewDiff2 { get; set; }
        public double viewDiff3 { get; set; }
        public double viewDiff4 { get; set; }
        public double viewDiff5 { get; set; }
        public double viewDiff6 { get; set; }
        public double viewDiff7 { get; set; }
        public double viewDiff8 { get; set; }
        public double deltaViewAngleX { get; set; }
        public double deltaViewAngleX1 { get; set; }
        public double deltaViewAngleX2 { get; set; }
        public double deltaViewAngleX3 { get; set; }
        public double deltaViewAngleX4 { get; set; }
        public double deltaViewAngleX5 { get; set; }
        public double deltaViewAngleX6 { get; set; }
        public double deltaViewAngleX7 { get; set; }
        public double deltaViewAngleX8 { get; set; }
        public double deltaViewAngleY { get; set; }
        public double deltaViewAngleY1 { get; set; }
        public double deltaViewAngleY2 { get; set; }
        public double deltaViewAngleY3 { get; set; }
        public double deltaViewAngleY4 { get; set; }
        public double deltaViewAngleY5 { get; set; }
        public double deltaViewAngleY6 { get; set; }
        public double deltaViewAngleY7 { get; set; }
        public double deltaViewAngleY8 { get; set; }
        public string Label { get; set; }
    }

    public sealed class ExtraFeaturesDataMap : ClassMap<ExtraFeaturesData>
    {
        public ExtraFeaturesDataMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.Label).Ignore();
        }
    }
}
