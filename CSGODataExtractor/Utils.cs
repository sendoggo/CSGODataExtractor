using DemoInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using System.Data;
using CSGODataExtractor;
using LiveCharts;
using LiveCharts.Defaults;
using System.Windows.Forms;
using MoreLinq;
using System.Data.OleDb;
using System.Security.Cryptography;
using CSGODataExtractor.Data_Structs;

namespace CSGODataExtractor
{
    public static class Utilities
    {
        // Parse data from .dem file and dumps it into self-titled folder
        // Splits data into:
        //      players.csv - All the players within the demo
        //      ticks.csv - Info of every player at every tick
        //      kills.csv - Kills related info
        public static void DataExtractor(string fileName)
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                Console.WriteLine("Parsing demo: " + fileName);
                using (var parser = new DemoParser(fileStream))
                {
                    parser.ParseHeader();

                    // Get map name
                    string map = parser.Map;

                    // Generate csv files
                    string[] _output = fileName.Split(new[] { "/", "." }, StringSplitOptions.None);
                    string demo_name = _output[_output.Length - 2];

                    Dictionary<long, string> playersDict = new Dictionary<long, string>();
                    List<PlayerData> playersRecords = new List<PlayerData>();
                    List<KillData> killsRecords = new List<KillData>();
                    List<TickData> ticksRecords = new List<TickData>();


                    parser.TickDone += (sender, e) =>
                    {
                        foreach (var p in parser.PlayingParticipants)

                            // "tick", "steamID", "viewangleYaw", "viewanglePitch", "currentTime"

                            ticksRecords.Add
                            (
                               new TickData()
                               {
                                   tick = parser.IngameTick,
                                   steamID = p.SteamID,

                                   viewAngleX = Math.Round(p.ViewDirectionX, 5),
                                   aimPunchAngleX = p.AimPunchAngleX,
                                   trueViewAngleX = Math.Round(p.ViewDirectionX + 2.0 * p.AimPunchAngleX, 5),
                                   viewAngleY = Math.Round(p.ViewDirectionY, 5),
                                   aimPunchAngleY = p.AimPunchAngleY,
                                   trueViewAngleY = Math.Round(p.ViewDirectionY + 2.0 * p.AimPunchAngleY, 5),
                                   currentTime = Math.Truncate(1000000 * parser.CurrentTime) / 1000000,

                               }
                           );
                    };

                    parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                    {
                        // If killer = null, the victim died from fall damage, excluding BOTS, nade kills, and knife kills (Not sure if the check works)
                        if (
                            e.Killer != null && e.Killer.SteamID > 0 &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.Knife) &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.Molotov) &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.HE) &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.Smoke) &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.Flash) &&
                            !e.Weapon.Weapon.Equals(EquipmentElement.Decoy) &&
                            !e.Killer.Team.Equals(e.Victim.Team) &&
                            parser.IngameTick > 10
                            )
                        {
                            // "tick", "attackerSteamID", "isHeadshot", "isWallbang", "weapon", "targetDistance"

                            killsRecords.Add
                            (
                               new KillData()
                               {
                                   tick = parser.IngameTick,
                                   steamID = e.Killer.SteamID,

                                   isHeadshot = e.Headshot ? 1 : 0,
                                   isWallbang = e.PenetratedObjects > 0 ? 1 : 0,
                                   weapon = (int)e.Weapon.Weapon,
                                   targetDistance = Math.Round(Math.Sqrt(Math.Pow(e.Victim.Position.X - e.Killer.Position.X, 2) + Math.Pow(e.Victim.Position.Y - e.Killer.Position.Y, 2) + Math.Pow(e.Victim.Position.Z - e.Killer.Position.Z, 2)), 2),
                                   currentTime = Math.Truncate(1000000 * parser.CurrentTime) / 1000000,

                               }
                           );

                            if (!playersDict.ContainsKey(e.Killer.SteamID))
                                playersDict[e.Killer.SteamID] = e.Killer.Name;

                        }
                    };

                    parser.ParseToEnd();
                    Console.WriteLine("Finished!");

                    foreach (var player in playersDict)
                        playersRecords.Add
                            (new PlayerData() { steamID = player.Key, username = player.Value });


                    try
                    {

                        string folderPath = demo_name + "."+ Math.Round(parser.TickRate) + "." + map + ".demo\\";


                        DirectoryInfo di = Directory.CreateDirectory(folderPath);
                        using (var ticksOutputStream = new StreamWriter(Path.Combine(folderPath, "ticks.csv")))
                        using (var csv = new CsvWriter(ticksOutputStream, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(ticksRecords);
                        }

                        using (var killsOutputStream = new StreamWriter(Path.Combine(folderPath, "kills.csv")))
                        using (var csv = new CsvWriter(killsOutputStream, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(killsRecords);
                        }

                        using (var playersOutputStream = new StreamWriter(Path.Combine(folderPath, "players.csv")))
                        using (var csv = new CsvWriter(playersOutputStream, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(playersRecords);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("The process failed: {0}", e.ToString());
                    }
                    finally { }


                }
            }
        }

        // Interpolates data between players.csv, kills.csv and ticks.csv
        // and generate features file
        public static void FeatureExtractor(string killsFile, string ticksFile, string playersFile, string outputFile)
        {

            long steamID = 0;
            using (var playersFileStream = new StreamReader(playersFile))
            using (var playersCsv = new CsvReader(playersFileStream, CultureInfo.InvariantCulture))
            {
                playersCsv.Configuration.Delimiter = ",";
                var records = playersCsv.GetRecords<PlayerData>();
                var menu = new EasyConsole.Menu();
                foreach (var r in records)
                    menu.Add(r.steamID + " " + r.username, () => steamID = r.steamID);
                menu.Display();
            }

            Console.WriteLine(steamID);
            uint result = 1;
            var menu1 = new EasyConsole.Menu()
                .Add("Non-Cheater", () => result = 0)
                .Add("Cheater", () => result = 1);

            menu1.Display();

            //List<KillData> killRecords; List<TickData> tickRecords;
            using (var killsReader = new StreamReader(killsFile))
            using (var ticksReader = new StreamReader(ticksFile))
            using (var killsCsv = new CsvReader(killsReader, CultureInfo.InvariantCulture))
            using (var ticksCsv = new CsvReader(ticksReader, CultureInfo.InvariantCulture))
            {
                killsCsv.Configuration.Delimiter = ",";
                ticksCsv.Configuration.Delimiter = ",";
                var killsRecords = killsCsv.GetRecords<KillData>().Where(x => x.steamID == steamID).ToList();
                var ticksRecord = ticksCsv.GetRecords<TickData>().Where(x => x.steamID == steamID).ToList();

                var tickBeforeKill =
                    (from x in ticksRecord
                     join tAtBefore in (from k in killsRecords
                                        select (from t in ticksRecord
                                                where t.tick < k.tick
                                                select t).Max(x => x.tick)) on x.tick equals tAtBefore

                     select new QueryData()
                     {
                         killTick = x.tick,
                         viewAngleX = x.viewAngleX,
                         viewAngleY = x.viewAngleY,
                         currentTime = x.currentTime,
                     }).ToList();


                var tickATKill =
                    (from k in killsRecords
                     join t in ticksRecord on k.tick equals t.tick
                     where k.steamID == steamID && t.steamID == k.steamID
                     select new QueryData()
                     {
                         killTick = k.tick,
                         weapon = k.weapon,
                         isHeadshot = k.isHeadshot,
                         isWallbang = k.isWallbang,
                         targetDistance = k.targetDistance,
                         viewAngleX = t.viewAngleX,
                         viewAngleY = t.viewAngleY,
                         currentTime = t.currentTime,
                     }
                     ).ToList();

                List<FeaturesData> records = new List<FeaturesData>();
                foreach (var item in tickATKill.Zip(tickBeforeKill, Tuple.Create))
                {
                    double dvaX = Math.Round(Math.Abs(Math.Abs(item.Item1.viewAngleX - item.Item2.viewAngleX + 180) % 360 - 180), 4);
                    double dvaY = Math.Round(Math.Abs(Math.Abs(item.Item1.viewAngleY - item.Item2.viewAngleY + 180) % 360 - 180), 4);
                    double vd = Math.Round(Math.Sqrt(Math.Pow(dvaX, 2) + Math.Pow(dvaY, 2)),4);
                    double td = Math.Truncate(1000000 * (item.Item1.currentTime - item.Item2.currentTime)) / 1000000;

                    records.Add
                    (
                        new FeaturesData()
                        {
                            tick = item.Item1.killTick,
                            steamID = steamID,
                            demoName = killsFile,


                            isHeadshot = item.Item1.isHeadshot,
                            isWallbang = item.Item1.isWallbang,
                            targetDistance = item.Item1.targetDistance,
                            deltaViewAngleX = dvaX,
                            deltaViewAngleY = dvaY,
                            viewDiff = vd,
                            viewDiffSpeed = Math.Round((vd / td),4),

                            label = result
                        }
                    );
                }

                bool exists = File.Exists(outputFile);
                using (var writer = new StreamWriter(outputFile, true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    //if (exists) csv.SetHeader(false);
                    csv.Configuration.HasHeaderRecord = !exists;
                    csv.WriteRecords(records);
                }

            }

            /* 
             *  
             * var query = @""+
                "SELECT isHeadshot, isWallbang, targetDistance, viewAngleX, viewAngleY, "+
                "(SELECT TOP 1 viewAngleX "+
                "FROM (SELECT * FROM [ticks.csv] WHERE steamID = " + steamID + @" ORDER BY tick DESC) TB "+
                "WHERE TB.tick < K.tick) AS xbeforet, "+
                "(SELECT TOP 1 viewAngleY "+
                "FROM (SELECT * FROM [ticks.csv] WHERE steamID = " + steamID + @" ORDER BY tick DESC) TB "+
                "WHERE TB.tick < K.tick) AS ybeforet "+
                "FROM ([kills.csv] K INNER JOIN "+
                        "[ticks.csv] T ON K.steamID = T.steamID) "+
                "WHERE K.tick = T.tick AND T.steamID = " + steamID + "";
                */
        }

        public static void FeaturesExtractorV2(string ticksFile, string playersFile)
        {
            long steamID = 0;
            using (var playersFileStream = new StreamReader(playersFile))
            using (var playersCsv = new CsvReader(playersFileStream, CultureInfo.InvariantCulture))
            {
                playersCsv.Configuration.Delimiter = ",";
                var players = playersCsv.GetRecords<PlayerData>();
                foreach (var r in players)
                {
                    steamID = r.steamID;
                    using (var ticksReader = new StreamReader(ticksFile))
                    using (var ticksCsv = new CsvReader(ticksReader, CultureInfo.InvariantCulture))
                    {
                        ticksCsv.Configuration.Delimiter = ",";
                        var ticksRecord = ticksCsv.GetRecords<TickData>().Where(x => x.steamID == steamID).ToList();

                        TickData temp = new TickData() 
                        { 
                            steamID = steamID,
                            viewAngleX = 0f,
                            viewAngleY = 0f,
                            currentTime = 0,
                        };
                        List<ExtraTickData> extraData = new List<ExtraTickData>();
                        for (int i = 0; i < ticksRecord.Count; i++)
                        {
                            if (i == 0)
                            {
                                extraData.Add(new ExtraTickData
                                {
                                    tick = ticksRecord[i].tick,
                                    steamID = steamID,

                                    viewAngleX = ticksRecord[i].viewAngleX,
                                    aimPunchAngleX = ticksRecord[i].aimPunchAngleX,
                                    trueViewAngleX = ticksRecord[i].trueViewAngleX,
                                    viewAngleY = ticksRecord[i].viewAngleX,
                                    aimPunchAngleY = ticksRecord[i].aimPunchAngleY,
                                    trueViewAngleY = ticksRecord[i].trueViewAngleY,
                                    currentTime = ticksRecord[i].currentTime,

                                    deltaViewAngleX = 0,
                                    deltaViewAngleY = 0,
                                    viewDiff = 0,
                                    viewDiffSpeed = 0,
                                });
                                temp.viewAngleX = ticksRecord[i].viewAngleX;
                                temp.viewAngleY = ticksRecord[i].viewAngleY;
                                temp.currentTime = ticksRecord[i].currentTime;
                            }
                            else
                            {
                                double dvaX = Math.Round(Math.Abs(Math.Abs(ticksRecord[i].viewAngleX - temp.viewAngleX + 180) % 360 - 180), 4);
                                double dvaY = Math.Round(Math.Abs(Math.Abs(ticksRecord[i].viewAngleY - temp.viewAngleY + 180) % 360 - 180), 4);
                                double vd = Math.Round(Math.Sqrt(Math.Pow(dvaX, 2) + Math.Pow(dvaY, 2)),4);
                                double td = Math.Truncate(1000000 * (ticksRecord[i].currentTime - temp.currentTime)) / 1000000;
                                extraData.Add(new ExtraTickData
                                {
                                    tick = ticksRecord[i].tick,
                                    steamID = steamID,

                                    viewAngleX = ticksRecord[i].viewAngleX,
                                    aimPunchAngleX = ticksRecord[i].aimPunchAngleX,
                                    trueViewAngleX = ticksRecord[i].trueViewAngleX,
                                    viewAngleY = ticksRecord[i].viewAngleY,
                                    aimPunchAngleY = ticksRecord[i].aimPunchAngleY,
                                    trueViewAngleY = ticksRecord[i].trueViewAngleY,
                                    currentTime = ticksRecord[i].currentTime,

                                    deltaViewAngleX = dvaX,
                                    deltaViewAngleY = dvaY,
                                    viewDiff = vd,
                                    viewDiffSpeed = Math.Round((vd / td), 4),
                                });
                                temp.viewAngleX = ticksRecord[i].viewAngleX;
                                temp.viewAngleY = ticksRecord[i].viewAngleY;
                                temp.currentTime = ticksRecord[i].currentTime;

                            }
                        }

                        string extraTickDataPath = Path.Combine(Path.GetDirectoryName(playersFile), "extraDataTicks.csv");
                        
                        bool exists = File.Exists(extraTickDataPath);
                        using (var writer = new StreamWriter(extraTickDataPath, true))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            //if (exists) csv.SetHeader(false);
                            csv.Configuration.HasHeaderRecord = !exists;
                            csv.WriteRecords(extraData);
                        }
                        Console.WriteLine("Finished player: " + steamID);
                    }
                }
            }
        }

        public static void DataInterpolator(string killsFile, string extraDataTicksFile, string playersFile, string outputFile)
        {
            int tickRate = Convert.ToInt32(Path.GetDirectoryName(playersFile).Split('.')[1]);

            // add error if demo isn't 32, 64, or 128 ticks
            using (var playersFileStream = new StreamReader(playersFile))
            using (var playersCsv = new CsvReader(playersFileStream, CultureInfo.InvariantCulture))
            {
                playersCsv.Configuration.Delimiter = ",";
                var players = playersCsv.GetRecords<PlayerData>();
                foreach (var r in players)
                {
                    using (var killsReader = new StreamReader(killsFile))
                    using (var extraTicksReader = new StreamReader(extraDataTicksFile))
                    using (var killsCsv = new CsvReader(killsReader, CultureInfo.InvariantCulture))
                    using (var extraTicksCsv = new CsvReader(extraTicksReader, CultureInfo.InvariantCulture))
                    {

                        killsCsv.Configuration.Delimiter = ",";
                        extraTicksCsv.Configuration.Delimiter = ",";
                        
                        var killsRecords = killsCsv.GetRecords<KillData>().Where(x => x.steamID == r.steamID).ToList();
                        var ticksRecord = extraTicksCsv.GetRecords<ExtraTickData>().Where(x => x.steamID == r.steamID).ToList();
                        
                        
                        List<ExtraFeaturesData> records = new List<ExtraFeaturesData>();
                        foreach (var kill in killsRecords)
                        {
                            var ticksAtKill =
                                (from t in ticksRecord
                                 where t.tick <= kill.tick && t.currentTime > (kill.currentTime - 0.252) && t.currentTime <= kill.currentTime
                                 orderby t.tick descending
                                 select new ExtraTickData()
                                 {
                                     deltaViewAngleX = t.deltaViewAngleX,
                                     deltaViewAngleY = t.deltaViewAngleY,
                                     viewDiff = t.viewDiff,
                                 }).Take(33).ToList();
                            switch (tickRate)
                            {
                                case 32:
                                    records.Add(new ExtraFeaturesData()
                                    {
                                        tick = kill.tick,
                                        steamID = r.steamID,
                                        demoName = killsFile,


                                        isHeadshot = kill.isHeadshot,
                                        isWallbang = kill.isWallbang,
                                        targetDistance = kill.targetDistance,

                                        viewDiff = ticksAtKill[0].viewDiff,
                                        viewDiff1 = ticksAtKill[1].viewDiff,
                                        viewDiff2 = ticksAtKill[2].viewDiff,
                                        viewDiff3 = ticksAtKill[3].viewDiff,
                                        viewDiff4 = ticksAtKill[4].viewDiff,
                                        viewDiff5 = ticksAtKill[5].viewDiff,
                                        viewDiff6 = ticksAtKill[6].viewDiff,
                                        viewDiff7 = ticksAtKill[7].viewDiff,
                                        viewDiff8 = ticksAtKill[8].viewDiff,

                                        deltaViewAngleX = ticksAtKill[0].deltaViewAngleX,
                                        deltaViewAngleX1 = ticksAtKill[1].deltaViewAngleX,
                                        deltaViewAngleX2 = ticksAtKill[2].deltaViewAngleX,
                                        deltaViewAngleX3 = ticksAtKill[3].deltaViewAngleX,
                                        deltaViewAngleX4 = ticksAtKill[4].deltaViewAngleX,
                                        deltaViewAngleX5 = ticksAtKill[5].deltaViewAngleX,
                                        deltaViewAngleX6 = ticksAtKill[6].deltaViewAngleX,
                                        deltaViewAngleX7 = ticksAtKill[7].deltaViewAngleX,
                                        deltaViewAngleX8 = ticksAtKill[8].deltaViewAngleX,

                                        deltaViewAngleY = ticksAtKill[0].deltaViewAngleY,
                                        deltaViewAngleY1 = ticksAtKill[1].deltaViewAngleY,
                                        deltaViewAngleY2 = ticksAtKill[2].deltaViewAngleY,
                                        deltaViewAngleY3 = ticksAtKill[3].deltaViewAngleY,
                                        deltaViewAngleY4 = ticksAtKill[4].deltaViewAngleY,
                                        deltaViewAngleY5 = ticksAtKill[5].deltaViewAngleY,
                                        deltaViewAngleY6 = ticksAtKill[6].deltaViewAngleY,
                                        deltaViewAngleY7 = ticksAtKill[7].deltaViewAngleY,
                                        deltaViewAngleY8 = ticksAtKill[8].deltaViewAngleY,

                                    });
                                    break;

                                case 64:
                                    records.Add(new ExtraFeaturesData()
                                    {
                                        tick = kill.tick,
                                        steamID = r.steamID,
                                        demoName = killsFile,


                                        isHeadshot = kill.isHeadshot,
                                        isWallbang = kill.isWallbang,
                                        targetDistance = kill.targetDistance,

                                        viewDiff = ticksAtKill[0].viewDiff,
                                        viewDiff1 = ticksAtKill[2].viewDiff,
                                        viewDiff2 = ticksAtKill[4].viewDiff,
                                        viewDiff3 = ticksAtKill[6].viewDiff,
                                        viewDiff4 = ticksAtKill[8].viewDiff,
                                        viewDiff5 = ticksAtKill[10].viewDiff,
                                        viewDiff6 = ticksAtKill[12].viewDiff,
                                        viewDiff7 = ticksAtKill[14].viewDiff,
                                        viewDiff8 = ticksAtKill[16].viewDiff,

                                        deltaViewAngleX = ticksAtKill[0].deltaViewAngleX,
                                        deltaViewAngleX1 = ticksAtKill[2].deltaViewAngleX,
                                        deltaViewAngleX2 = ticksAtKill[4].deltaViewAngleX,
                                        deltaViewAngleX3 = ticksAtKill[6].deltaViewAngleX,
                                        deltaViewAngleX4 = ticksAtKill[8].deltaViewAngleX,
                                        deltaViewAngleX5 = ticksAtKill[10].deltaViewAngleX,
                                        deltaViewAngleX6 = ticksAtKill[12].deltaViewAngleX,
                                        deltaViewAngleX7 = ticksAtKill[14].deltaViewAngleX,
                                        deltaViewAngleX8 = ticksAtKill[16].deltaViewAngleX,

                                        deltaViewAngleY = ticksAtKill[0].deltaViewAngleY,
                                        deltaViewAngleY1 = ticksAtKill[2].deltaViewAngleY,
                                        deltaViewAngleY2 = ticksAtKill[4].deltaViewAngleY,
                                        deltaViewAngleY3 = ticksAtKill[6].deltaViewAngleY,
                                        deltaViewAngleY4 = ticksAtKill[8].deltaViewAngleY,
                                        deltaViewAngleY5 = ticksAtKill[10].deltaViewAngleY,
                                        deltaViewAngleY6 = ticksAtKill[12].deltaViewAngleY,
                                        deltaViewAngleY7 = ticksAtKill[14].deltaViewAngleY,
                                        deltaViewAngleY8 = ticksAtKill[16].deltaViewAngleY,

                                    });
                                    break;

                                case 128:
                                    records.Add(new ExtraFeaturesData()
                                    {
                                        tick = kill.tick,
                                        steamID = r.steamID,
                                        demoName = killsFile,


                                        isHeadshot = kill.isHeadshot,
                                        isWallbang = kill.isWallbang,
                                        targetDistance = kill.targetDistance,

                                        viewDiff = ticksAtKill[0].viewDiff,
                                        viewDiff1 = ticksAtKill[4].viewDiff,
                                        viewDiff2 = ticksAtKill[8].viewDiff,
                                        viewDiff3 = ticksAtKill[12].viewDiff,
                                        viewDiff4 = ticksAtKill[16].viewDiff,
                                        viewDiff5 = ticksAtKill[20].viewDiff,
                                        viewDiff6 = ticksAtKill[24].viewDiff,
                                        viewDiff7 = ticksAtKill[28].viewDiff,
                                        viewDiff8 = ticksAtKill[32].viewDiff,

                                        deltaViewAngleX = ticksAtKill[0].deltaViewAngleX,
                                        deltaViewAngleX1 = ticksAtKill[4].deltaViewAngleX,
                                        deltaViewAngleX2 = ticksAtKill[8].deltaViewAngleX,
                                        deltaViewAngleX3 = ticksAtKill[12].deltaViewAngleX,
                                        deltaViewAngleX4 = ticksAtKill[16].deltaViewAngleX,
                                        deltaViewAngleX5 = ticksAtKill[20].deltaViewAngleX,
                                        deltaViewAngleX6 = ticksAtKill[24].deltaViewAngleX,
                                        deltaViewAngleX7 = ticksAtKill[28].deltaViewAngleX,
                                        deltaViewAngleX8 = ticksAtKill[32].deltaViewAngleX,

                                        deltaViewAngleY = ticksAtKill[0].deltaViewAngleY,
                                        deltaViewAngleY1 = ticksAtKill[4].deltaViewAngleY,
                                        deltaViewAngleY2 = ticksAtKill[8].deltaViewAngleY,
                                        deltaViewAngleY3 = ticksAtKill[12].deltaViewAngleY,
                                        deltaViewAngleY4 = ticksAtKill[16].deltaViewAngleY,
                                        deltaViewAngleY5 = ticksAtKill[20].deltaViewAngleY,
                                        deltaViewAngleY6 = ticksAtKill[24].deltaViewAngleY,
                                        deltaViewAngleY7 = ticksAtKill[28].deltaViewAngleY,
                                        deltaViewAngleY8 = ticksAtKill[32].deltaViewAngleY,

                                    });
                                    break;

                                default:
                                    records.Add(new ExtraFeaturesData()
                                    {
                                        tick = kill.tick,
                                        steamID = r.steamID,
                                        demoName = killsFile,

                                        isHeadshot = kill.isHeadshot,
                                        isWallbang = kill.isWallbang,
                                        targetDistance = kill.targetDistance,

                                        viewDiff = ticksAtKill[0].viewDiff,
                                        viewDiff1 = ticksAtKill[1].viewDiff,
                                        viewDiff2 = ticksAtKill[2].viewDiff,
                                        viewDiff3 = ticksAtKill[3].viewDiff,
                                        viewDiff4 = ticksAtKill[4].viewDiff,
                                        viewDiff5 = ticksAtKill[5].viewDiff,
                                        viewDiff6 = ticksAtKill[6].viewDiff,
                                        viewDiff7 = ticksAtKill[7].viewDiff,
                                        viewDiff8 = ticksAtKill[8].viewDiff,

                                        deltaViewAngleX = ticksAtKill[0].deltaViewAngleX,
                                        deltaViewAngleX1 = ticksAtKill[1].deltaViewAngleX,
                                        deltaViewAngleX2 = ticksAtKill[2].deltaViewAngleX,
                                        deltaViewAngleX3 = ticksAtKill[3].deltaViewAngleX,
                                        deltaViewAngleX4 = ticksAtKill[4].deltaViewAngleX,
                                        deltaViewAngleX5 = ticksAtKill[5].deltaViewAngleX,
                                        deltaViewAngleX6 = ticksAtKill[6].deltaViewAngleX,
                                        deltaViewAngleX7 = ticksAtKill[7].deltaViewAngleX,
                                        deltaViewAngleX8 = ticksAtKill[8].deltaViewAngleX,

                                        deltaViewAngleY = ticksAtKill[0].deltaViewAngleY,
                                        deltaViewAngleY1 = ticksAtKill[1].deltaViewAngleY,
                                        deltaViewAngleY2 = ticksAtKill[2].deltaViewAngleY,
                                        deltaViewAngleY3 = ticksAtKill[3].deltaViewAngleY,
                                        deltaViewAngleY4 = ticksAtKill[4].deltaViewAngleY,
                                        deltaViewAngleY5 = ticksAtKill[5].deltaViewAngleY,
                                        deltaViewAngleY6 = ticksAtKill[6].deltaViewAngleY,
                                        deltaViewAngleY7 = ticksAtKill[7].deltaViewAngleY,
                                        deltaViewAngleY8 = ticksAtKill[8].deltaViewAngleY,

                                    });
                                    break;

                            }

                            
                             Console.WriteLine("Finished tick: " + kill.tick);
                        }

                        

                        bool exists = File.Exists(outputFile);
                        using (var writer = new StreamWriter(outputFile, true))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            //if (exists) csv.SetHeader(false);
                            csv.Configuration.RegisterClassMap<ExtraFeaturesDataMap>();
                            csv.Configuration.HasHeaderRecord = !exists;
                            csv.WriteRecords(records);
                        }
                        Console.WriteLine("Finished player: " + r.steamID);
                    }
                }
            }
        }
        public static void LabeledEntriesExtractor(string input, string output)
        {
            
            using (var inputFileStream = new StreamReader(input))
            using (var inputCsv = new CsvReader(inputFileStream, CultureInfo.InvariantCulture))
            {
                inputCsv.Configuration.Delimiter = ",";
                // sort this by labels
                var entries = inputCsv.GetRecords<ExtraFeaturesData>().Where(x => x.Label != "").ToList();

                bool exists = File.Exists(output);
                using (var writer = new StreamWriter(output, true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    //if (exists) csv.SetHeader(false);
                    csv.Configuration.HasHeaderRecord = !exists;
                    csv.WriteRecords(entries);
                }
            }

            
        }

        public static void UnlabeledFeatureExtractor(string killsFile, string ticksFile, string playersFile, string outputFile)
        {
            long steamID = 0;
            using (var playersFileStream = new StreamReader(playersFile))
            using (var playersCsv = new CsvReader(playersFileStream, CultureInfo.InvariantCulture))
            {
                playersCsv.Configuration.Delimiter = ",";
                var players = playersCsv.GetRecords<PlayerData>();
                foreach (var r in players)
                {
                    steamID = r.steamID;
                    using (var killsReader = new StreamReader(killsFile))
                    using (var ticksReader = new StreamReader(ticksFile))
                    using (var killsCsv = new CsvReader(killsReader, CultureInfo.InvariantCulture))
                    using (var ticksCsv = new CsvReader(ticksReader, CultureInfo.InvariantCulture))
                    {
                        killsCsv.Configuration.Delimiter = ",";
                        ticksCsv.Configuration.Delimiter = ",";
                        var killsRecords = killsCsv.GetRecords<KillData>().ToList();
                        var ticksRecord = ticksCsv.GetRecords<TickData>();



                        var tickBySteamID =
                            (from t in ticksRecord
                             where t.steamID == steamID
                             select t).ToList();

                        var killBySteamID =
                            (from k in killsRecords
                             where k.steamID == steamID
                             select k).ToList();

                        var tickBeforeKill =
                            (from x in tickBySteamID
                             join tAtBefore in (from k in killBySteamID
                                                select (from t in tickBySteamID
                                                        where t.tick < k.tick
                                                        select t).Max(x => x.tick)) on x.tick equals tAtBefore

                             select new QueryData()
                             {
                                 killTick = x.tick,
                                 viewAngleX = x.viewAngleX,
                                 viewAngleY = x.viewAngleY,
                                currentTime = x.currentTime,
                             }).ToList();


                        var tickATKill =
                            (from k in killBySteamID
                             join t in tickBySteamID on k.tick equals t.tick
                             where k.steamID == steamID && t.steamID == k.steamID
                             select new QueryData()
                             {
                                 killTick = k.tick,
                                 weapon = k.weapon,
                                 isHeadshot = k.isHeadshot,
                                 isWallbang = k.isWallbang,
                                 targetDistance = k.targetDistance,
                                 viewAngleX = t.viewAngleX,
                                 viewAngleY = t.viewAngleY,
                                 currentTime = t.currentTime,
                             }
                             ).ToList();

                        List<FeaturesData> records = new List<FeaturesData>();
                        foreach (var item in tickATKill.Zip(tickBeforeKill, Tuple.Create))
                        {
                            double dvaX = Math.Round(Math.Abs(Math.Abs(item.Item1.viewAngleX - item.Item2.viewAngleX + 180) % 360 - 180), 4);
                            double dvaY = Math.Round(Math.Abs(Math.Abs(item.Item1.viewAngleY - item.Item2.viewAngleY + 180) % 360 - 180), 4);
                            double vd = Math.Round(Math.Sqrt(Math.Pow(dvaX, 2) + Math.Pow(dvaY, 2)),4);
                            double td = Math.Truncate(1000000 * (item.Item1.currentTime - item.Item2.currentTime)) / 1000000;


                            records.Add
                            (
                                new FeaturesData()
                                {
                                    tick = item.Item1.killTick,
                                    steamID = steamID,
                                    demoName = killsFile,

                                    isHeadshot = item.Item1.isHeadshot,
                                    isWallbang = item.Item1.isWallbang,
                                    targetDistance = item.Item1.targetDistance,
                                    deltaViewAngleX = dvaX,
                                    deltaViewAngleY = dvaY,
                                    viewDiff = vd,
                                    viewDiffSpeed = Math.Round((vd / td),4),
                                }
                            );
                        }

                        bool exists = File.Exists(outputFile);
                        using (var writer = new StreamWriter(outputFile, true))
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            //if (exists) csv.SetHeader(false);
                            csv.Configuration.RegisterClassMap<FeaturesDataMap>();
                            csv.Configuration.HasHeaderRecord = !exists;
                            csv.WriteRecords(records);
                        }
                        Console.WriteLine("Finished player: " +steamID);
                    }
                }
            }
        }

        // Help menu
        public static void Help()
        {
            Console.WriteLine("CS:GO Data Extractor");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Usage: [--help] [--folder folderPath] [--FE files] [--plot ticksFile.csv steamID tick@kill]  [file1.dem [file2.dem ...]]");

            Console.WriteLine("--help");
            Console.WriteLine("    Displays this help");

            Console.WriteLine("[file1.dem <file2.dem ...>]");
            Console.WriteLine("    Parses demo files based off the path.");
            Console.WriteLine("    Can take multiple files in input, ");
            Console.WriteLine("    separated by spaces.");

            Console.WriteLine("--folder folderPath");
            Console.WriteLine("    Parses all demos within a folder and generate");
            Console.WriteLine("    folders for each demo. ");

            Console.WriteLine("--features folderPath  outputfile.csv");
            Console.WriteLine("    Interpolates data between players.csv, kills.csv and ticks.csv ");
            Console.WriteLine("    and generates a feature file.");

            Console.WriteLine("------------------TEST METHODS---------------------------");
            Console.WriteLine("--plot ticksFile.csv steamID tick@kill");
            Console.WriteLine("    NEED REWORKING.");
            Console.WriteLine("    TESTING PURPOUSES: Prints out plot of the crosshair movements");
            Console.WriteLine("    10 ticks before and after the kill.");

            Console.WriteLine("--printPlayers file.dem");
            Console.WriteLine("    TESTING PURPOUSES: Displays all players within a demo files.");


        }

        // DEBUGGING METHOD:
        // Parse players within .dem files and dumps on console
        public static void PlayersExtractor(string fileName)
        {
            using (var fileStream = File.OpenRead(fileName))
            {
                Console.WriteLine("Parsing demo: " + fileName);
                using (var parser = new DemoParser(fileStream))
                {
                    parser.ParseHeader();
                    Dictionary<long, string> playersDict = new Dictionary<long, string>();
                    parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                    {
                        //the killer is null if you're killed by the world - eg. by falling
                        if (e.Killer != null && e.Killer.SteamID > 0)
                        {
                            if (!playersDict.ContainsKey(e.Killer.SteamID))
                                playersDict[e.Killer.SteamID] = e.Killer.Name;


                        }
                    };

                    parser.ParseToEnd();
                    Console.WriteLine("Finished!");
                    foreach (var p in playersDict)
                        Console.WriteLine(p.Key + "," + p.Value);
                }
            }
        }

        // DEBUGGING METHOD:
        // Displays successions of angles at tick at kill - NEEDS REWORKING
        public static void PlotAngles(int tickAtKill, long steamID, string ticksFile)
        {
            List<TickData> tickRecords; List<QueryData> output;
            using (var ticksReader = new StreamReader(ticksFile))
            using (var ticksCsv = new CsvReader(ticksReader, CultureInfo.InvariantCulture))
            {
                ticksCsv.Configuration.Delimiter = ",";
                tickRecords = ticksCsv.GetRecords<TickData>().ToList();

                output =
                (
                    from t in tickRecords
                    where (t.steamID == steamID) && (t.tick < (tickAtKill + 10)) && (t.tick > (tickAtKill - 10))
                    select

                    new QueryData()
                    {

                        //killTick = t.tick,
                        viewAngleX = t.viewAngleX,
                        viewAngleY = t.viewAngleY,

                        /*aimPunchAngleX = t.aimPunchAngleX,
                        aimPunchAngleY = t.aimPunchAngleY,
                        currentTime = t.currentTime*/


                    }
                ).ToList();
            }

            List<double> X = new List<double>();
            List<double> Y = new List<double>();
            for (int i = 1; i < output.Count; i++)
            {
                /*X.Add(Math.Round(Math.Abs(Math.Abs(output[i].viewAngleX - output[i - 1].viewAngleX + 180) % 360 - 180), 4));
                Y.Add(Math.Round(Math.Abs(Math.Abs(output[i].viewAngleY - output[i - 1].viewAngleY + 180) % 360 - 180), 4));*/
                //Console.WriteLine(output[i] + " - " + output[i - 1] + " = "+ test[i-1]);

                X.Add(output[i].viewAngleX);
                Y.Add(output[i].viewAngleY);
            }
            Application.Run(new plot(X, Y));

        }

        // DEBUGGING METHOD:
        // Dumps all data in a graph - NEEDS REWORKING
        public static void PlotUnlabeledData(string unlabeledFile)
        {
            List<FeaturesData> unlabeledRecords; List<double> output;

            using (var unlabeledReader = new StreamReader(unlabeledFile))
            using (var unlabeladCsv = new CsvReader(unlabeledReader, CultureInfo.InvariantCulture))
            {
                unlabeladCsv.Configuration.Delimiter = ",";
                unlabeladCsv.Configuration.RegisterClassMap<FeaturesDataMap>();
                unlabeledRecords = unlabeladCsv.GetRecords<FeaturesData>().ToList();

                output =
                (
                    from t in unlabeledRecords
                    select t.viewDiffSpeed

                ).ToList();
            }

            Application.Run(new bigPlot(output));

        }

    }
}
