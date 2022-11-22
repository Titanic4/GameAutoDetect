using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf.JsonConverter;
using System.Security.Permissions;
using System.Security.Principal;
using System.Diagnostics;
using System.Reflection;

namespace GameAutoDetect
{
    
    class LibraryFolder
    {

        public string path { get; set; }

        public dynamic apps;


    }
    class Settings
    {
        public string installPath;
        public Games games;
        public string theme;
        public bool closeOnLaunch;
    }
    class Games
    {
        public ETS2 ets2;
        public ATS ats;
        
    }
    class ETS2
    {
        public string game;
        public string path;
        public string[] consoleOpts;

    }
    class ATS
    {
        public string game;
        public string path;
        public string[] consoleOpts;

    }
    internal class Program
    {
        public static List<LibraryFolder> Folders = new List<LibraryFolder>();
        const int ETS2 = 227300;
        const int ATS = 270880;
        
  
        public static string SteamPath = "";
        public static bool OnlyNew = true;
        public static string ETS2Path = "";
        public static string ATSPath = "";
        public static bool FoundETS2 = false;
        public static bool FoundATS = false;
        public static VProperty AllLibs;
        static void SaveSettings()
        {
            string JSON = "";

            JSON = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options.json");
            Settings Settings = new Settings();
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(JSON);
            if (FoundATS) {
                Settings.games.ats.path = ATSPath + @"bin\win_x64\amtrucks.exe";
                    
                    }
            if (FoundETS2)
            {
                Settings.games.ets2.path = ETS2Path + @"bin\win_x64\eurotrucks2.exe";

            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options_backup.json")){
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options_backup.json"); 
               }
            File.Copy(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options.json", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options_backup.json");
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP\\launcher-options.json", Newtonsoft.Json.JsonConvert.SerializeObject(Settings));
            Console.WriteLine("\nNew launcher settings update completed succesfully.");
        }
        
        static void SaveSettingsRegistry()
        {
            try
            {
              
         
                if (FoundETS2)
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\TRUCKERSMP", "InstallLocationETS2", ETS2Path);
                }
                if (FoundATS)
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\TRUCKERSMP", "InstallLocationATS", ATSPath);
                }
                Console.WriteLine("\nLegacy launcher settings update completed succesfully.");
            }
            catch(Exception E)
            {
                Console.WriteLine($"\nLegacy launcher settings weren't updated.  E: {E}");
            }
        }
            static void Main(string[] args)
        {
            
        
            try
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                if (args.Contains("-legacy"))
                {
                    
                    WindowsIdentity id = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(id);

                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator)) {
                        ProcessStartInfo proc = new ProcessStartInfo();
                        proc.UseShellExecute = true;
                        proc.WorkingDirectory = Environment.CurrentDirectory;
                        proc.FileName = Assembly.GetEntryAssembly().CodeBase;
                        proc.Verb = "runas";
                        proc.Arguments = "-legacy";
                        try
                        {
                            Process.Start(proc);
                            Environment.Exit(0);
                        }
                        catch
                        {
                            Console.WriteLine("Legacy launcher support couldn't be enabled.");
                            OnlyNew = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Legacy launcher support enabled.");
                        OnlyNew = false;
                    }
                    
                }
                SteamPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432NODE\\VALVE\\STEAM", "InstallPath", "").ToString();
                if (SteamPath != null || SteamPath != "")
                {
                    Console.WriteLine($"Checking all Steam Libraries of the current Steam installation in {SteamPath} ...");
                    AllLibs = VdfConvert.Deserialize(File.ReadAllText(SteamPath + "\\config\\libraryfolders.vdf"));

                    

                    for (int i=0;i<AllLibs.Value.Count();i++)
                    {
                      
                        Folders.Add(AllLibs.Value[i.ToString()].ToJson().ToObject<LibraryFolder>());

                    }
                    Console.WriteLine($"Found {Folders.Count} Steam Librar{((Folders.Count > 1)?("ies") : ("y"))}. Starting the detection process...");



                    foreach (var item in Folders)
                    {
                        Console.WriteLine($"Searching for supported games in Steam Library located in {item.path} ...");
                        if (FoundETS2 && FoundATS)
                        {
                            Console.WriteLine("Detection is complete. Both supported games were successfully detected.");
                            break;
                        }
                        else
                        {
                            foreach (var app in item.apps)
                            {

                                if (app.ToString().Contains(ETS2.ToString()) && ETS2Path == "")
                                {
                                    FoundETS2 = true;
                                    ETS2Path = item.path + "\\steamapps\\common\\Euro Truck Simulator 2\\";
                                }
                                if (app.ToString().Contains(ATS.ToString()) && ATSPath == "")
                                {
                                    FoundATS = true;
                                    ATSPath = item.path + "\\steamapps\\common\\American Truck Simulator\\";
                                }

                            }
                        }

                        
                    }

             
                    
                }
                else
                {
                    Console.WriteLine("\nSteam may not be installed, or installation may be corrupt.");
                }

            }
            catch(Exception E) {

                Console.WriteLine("Something went wrong. Exception: \n"+E);
            }
            if (ETS2Path != "")
            {
                Console.WriteLine("ETS 2 path: \"" + ETS2Path+"\"");
            }
            if (ATSPath != "")
            {
                Console.WriteLine("ATS path: \"" + ATSPath+"\"");
            }
            if (FoundETS2 || FoundATS)
            {
                if (!OnlyNew)
                {
                    try
                    {


                        if (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\TRUCKERSMP", "InstallDir", "1") != null)
                        {
                            Console.WriteLine("\nLegacy launcher found.");
                        }
                        Console.WriteLine("Do you want to update the paths in launcher's registry keys? (Y/N) Invalid input is treated as No.");
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.N: break; ;
                            case ConsoleKey.Y: SaveSettingsRegistry(); break;
                            default: break;

                        };
                    }
                    catch
                    {
                    }
                }
                else
                {
                    if (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\TRUCKERSMP", "InstallDir", "1") != null)
                    {
                        Console.WriteLine("\nLegacy launcher found, updating game paths for it is disabled.\n Relaunch with -legacy parameter to override.");
                    }
                }

                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP"))
                {
                    Console.WriteLine("\nNew launcher found. Do you want to update the paths in settings file? (Y/N) Invalid input closes.");
                    switch (Console.ReadKey(true).Key)
                    {
                        case ConsoleKey.N: return; ;
                        case ConsoleKey.Y: SaveSettings(); break;
                        default: return;

                    };
                }
              
                
               
                
            }
            if (!FoundETS2 && !FoundATS)
            {
                Console.WriteLine("We were unable to detect any supported game(s).");
                Console.ReadKey();
                return;
            }
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TruckersMP"))
            {
                Console.WriteLine($"If the new file causes launcher to lock up when opening settings, head to %appdata%\\TruckersMP and swap the current config file with one labelled \"_backup\"");
                Console.WriteLine("If you had launcher already running, try restarting it. If nothing changes. relaunch this program without launcher running.");
            }
            Console.ReadKey(true);
        }
    }
}
