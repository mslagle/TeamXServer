using System;
using System.Linq;
using System.Net;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using TeamXServer;

namespace TKServerConsole
{
    public static class Program
    {
        private static bool readyForShutdown = false;
        public static ServerConfig Config = new ServerConfig();
        public static SaveManager saveManager;
        public static Editor editor;
        public static PlayerManager playerManager;
        public static Server server;
        public static void Main(string[] args)
        {
            Graphics.ShowLogo();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            try
            {
                Logger.Log("Starting TeamX Server", LogType.Message);
                Logger.Log("Reading configuration file.", LogType.Message);
                Config.Load();

                Graphics.LogConfiguration(Config);

                PacketUtility.AutoRegisterPacketsInSameNamespace();

                editor = new Editor();
                saveManager = new SaveManager();
                playerManager = new PlayerManager();
                server = new Server();                

                RunServerLoop();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }        

        private static void RunServerLoop()
        {
            while (true)
            {
                saveManager.RunAutoSave();
                server.Run();
            }
        }

        private static void HandleException(Exception ex)
        {
            Logger.Log("The server has encountered the following error and will be stopped:", LogType.Error);
            Logger.Log($"Error: {ex.Message}", LogType.Error);

            server.Shutdown("Error");
            saveManager.Save();

            Logger.Log("Press enter to exit...", LogType.Message);
            readyForShutdown = true;
            Console.ReadLine();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            if (readyForShutdown)
            {
                Logger.Log("Exiting...", LogType.Message);
            }

            server.Shutdown("Shutdown");
            saveManager.Save();

            Logger.Log("Exiting...", LogType.Message);
            Console.ReadLine();
        }
    }
}
