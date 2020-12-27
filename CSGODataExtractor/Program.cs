using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfo;
using System.IO;
using System.Runtime.CompilerServices;
using NumSharp.Utilities;
using System.Windows.Forms;


namespace CSGODataExtractor
{

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                Utilities.Help();
                return;
            }
            else
            {
                switch (args[0])
                {
                    case "--folder":
                        if (Directory.Exists(args[1]))
                        {

                            string[] files = Directory.GetFiles(args[1], "*.dem");
                            foreach (var fileName in files)
                            {
                                Console.WriteLine(fileName);
                                Utilities.DataExtractor(fileName);
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Folder not found");
                            break;
                        }

                    case "--features":
                        if (Directory.Exists(args[1]) && args[2] != "")
                        {
                            string[] killsFile = Directory.GetFiles(args[1], "kills.csv");
                            string[] ticksFile = Directory.GetFiles(args[1], "ticks.csv");
                            string[] players = Directory.GetFiles(args[1], "players.csv");
                            Utilities.FeatureExtractor(killsFile[0], ticksFile[0], players[0], args[2]);
                            break;                            
                        }
                        else
                        {
                            Console.WriteLine("Folder not found");
                            break;
                        }

                    case "--featuresV2":
                        if (Directory.Exists(args[1]))
                        {
                            string[] demoDirs = Directory.GetDirectories(args[1], "*.demo");
                            foreach (var dir in demoDirs)
                            {
                                Console.WriteLine("Dumping: " + dir);
                                string[] ticksFile = Directory.GetFiles(dir, "ticks.csv");
                                string[] players = Directory.GetFiles(dir, "players.csv");
                                Utilities.FeaturesExtractorV2(ticksFile[0], players[0]);
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Folder not found");
                            break;
                        }

                    case "--dataInterpolator":
                        if (Directory.Exists(args[1]))
                        {
                            string[] demoDirs = Directory.GetDirectories(args[1], "*.demo");
                            foreach (var dir in demoDirs)
                            {
                                Console.WriteLine("Dumping: " + dir);
                                string[] killsFile = Directory.GetFiles(dir, "kills.csv");
                                string[] extraTicksFile = Directory.GetFiles(dir, "extraDataTicks.csv");
                                string[] players = Directory.GetFiles(dir, "players.csv"); ;
                                Utilities.DataInterpolator(killsFile[0], extraTicksFile[0], players[0], args[2]);
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Folder not found");
                            break;
                        }

                    case "--unlabeledFeatures":
                        if(args[2] != "")
                        {
                            string[] demoDirs = Directory.GetDirectories(args[1], "*.demo");
                            foreach (var dir in demoDirs)
                            {
                                Console.WriteLine("Dumping: " + dir);
                                string[] killsFile = Directory.GetFiles(dir, "kills.csv");
                                string[] ticksFile = Directory.GetFiles(dir, "ticks.csv");
                                string[] players = Directory.GetFiles(dir, "players.csv");
                                Utilities.UnlabeledFeatureExtractor(killsFile[0], ticksFile[0], players[0], args[2]); 
                            }
                            break;
                        }
                        else { Console.WriteLine("Output CSV File Missing"); break; }

                    case "--extractLabels":
                        if (args != null || args.Length == 3)
                            {
                            Utilities.LabeledEntriesExtractor(args[1], args[2]);
                            break;
                        }
                        else { Console.WriteLine("Input/Output CSV Files Missing"); break; }

                    case "--plot":
                        Utilities.PlotAngles(Int32.Parse(args[3]), long.Parse(args[2]), args[1]);
                        break;

                    case "--plotUnlabeled":
                        Utilities.PlotUnlabeledData(args[1]);
                        break;

                    case "--printPlayers":
                        if (File.Exists(args[1]))
                        {
                            Console.WriteLine((args[1]));
                            Utilities.PlayersExtractor((args[1]));

                            break;
                        }
                        else
                        {
                            Console.WriteLine("File not found");
                            break;
                        }

                    default:
                        foreach (var fileName in args)
                        {
                            if (File.Exists(fileName)) Utilities.DataExtractor(fileName);
                            else { Console.WriteLine("File: " + fileName + " not found"); break; }
                        }
                        break;
                }
            }

        }

    }
}
