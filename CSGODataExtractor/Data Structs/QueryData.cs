using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace CSGODataExtractor
{
    public class QueryData
    {
        public int killTick { get; set; }
        public long steamID { get; set; }
        public int weapon { get; set; }
        public int isHeadshot { get; set; }
        public int isWallbang { get; set; }
        public double targetDistance { get; set; }
        public double viewAngleX { get; set; }
        public double viewAngleY { get; set; }
        /*public double aimPunchAngleX { get; set; }
        public double aimPunchAngleY { get; set; }*/
        public double currentTime { get; set; }
    }
}
