using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGODataExtractor
{
    class KillData
    {
        //tick;attackerSteamID;victimSteamID;isHeadshot;isWallbang;weapon;targetDistance
        public int tick { get; set; }
        public long steamID { get; set; }
        public int isHeadshot { get; set; }
        public int isWallbang { get; set; }
        public int weapon { get; set; }
        public double targetDistance { get; set; }
        public double currentTime { get; set; }
    }
}
