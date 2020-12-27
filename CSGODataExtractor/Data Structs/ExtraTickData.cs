using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGODataExtractor.Data_Structs
{
    class ExtraTickData
    {
        public int tick { get; set; }
        public long steamID { get; set; }
        public double viewAngleX { get; set; }
        public double viewAngleY { get; set; }
        public double currentTime { get; set; }
        public float aimPunchAngleX { get; set; }
        public double trueViewAngleX { get; set; }
        public float aimPunchAngleY { get; set; }
        public double trueViewAngleY { get; set; }
        public double deltaViewAngleX { get; set; }
        public double deltaViewAngleY { get; set; }
        public double viewDiff { get; set; }
        public double viewDiffSpeed { get; set; }
    }
}
