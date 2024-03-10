using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace OLDD_camera
{
    internal  static class WindowSettings
    {
        static bool loaded = false;

        //static internal bool shadersToUse0 ;
        //static internal bool shadersToUse1;
        //static internal bool shadersToUse2;
        static internal Rect windowPosition;
        static internal Rect cameraWindowPosition;

        static internal int WindowSizeCoef;

        internal static String _AssemblyLocation
        { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        internal static String _AssemblyFolder
        { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }

        /// <summary>
        /// Location of file for saving and loading methods
        ///
        /// This can be an absolute path (eg c:\test.cfg) or a relative path from the location of the assembly dll (eg. ../config/test)
        /// </summary>
        public static String FilePath
        {
            get
            {
                return System.IO.Path.Combine(_AssemblyFolder + "/../PluginData/", "DockingCamera.cfg").Replace("\\", "/");
            }
        }

        static void SaveSettings()
        {
            if (!loaded)
                LoadSettings();
            ConfigNode n = new ConfigNode();
            ConfigNode node = new ConfigNode("DockingCamera");
            //node.AddValue("shadersToUse0", shadersToUse0);
            //node.AddValue("shadersToUse1", shadersToUse1);
            //node.AddValue("shadersToUse2", shadersToUse2);
            node.AddValue("WindowSizeCoef", WindowSizeCoef);

            node.AddValue("winX", windowPosition.x);
            node.AddValue("winY", windowPosition.y);
            node.AddValue("winWidth", windowPosition.width);
            node.AddValue("winHeight", windowPosition.height);

            node.AddValue("camWinX", cameraWindowPosition.x);
            node.AddValue("camWinY", cameraWindowPosition.y);
            node.AddValue("camWinWidth", cameraWindowPosition.width);
            node.AddValue("camWinHeight", cameraWindowPosition.height);

            n.AddNode(node);
            n.Save(FilePath);
        }

        internal static void SaveWinSettings(/* bool s0, bool s1, bool s2, */ Rect win)
        {
            //shadersToUse0 = s0;
            //shadersToUse1 = s1;
            //shadersToUse2 = s2;
            windowPosition = win;
            SaveSettings();
        }


        internal static void SaveCameraWinSettings(/* bool s0, bool s1, bool s2, */ Rect win)
        {
            //shadersToUse0 = s0;
            //shadersToUse1 = s1;
            //shadersToUse2 = s2;
            cameraWindowPosition = win;
            SaveSettings();
        }

        internal static void SaveSettings(int wsc)
        {
            WindowSizeCoef = wsc;
            SaveSettings();
        }
        internal static bool LoadSettings()
        {
            loaded = true;
            if (File.Exists(FilePath))
            {
                ConfigNode n = ConfigNode.Load(FilePath);
                if (n.HasNode("DockingCamera"))
                {
                    ConfigNode node = n.GetNode("DockingCamera");

                    WindowSizeCoef = int.Parse(node.GetValue("WindowSizeCoef"));
                    WindowSizeCoef = Math.Min(Math.Max(WindowSizeCoef, 2),5);  // Make sure it is between 2 and 5
                    if (node.HasValue("winX"))
                    {
                        windowPosition.x = float.Parse(node.GetValue("winX"));
                        windowPosition.y = float.Parse(node.GetValue("winY"));
                        windowPosition.width = float.Parse(node.GetValue("winWidth"));
                        windowPosition.height = float.Parse(node.GetValue("winHeight"));
                    }
                    if (node.HasValue("camWinX"))
                    {
                        cameraWindowPosition.x = float.Parse(node.GetValue("camWinX"));
                        cameraWindowPosition.y = float.Parse(node.GetValue("camWinY"));
                        cameraWindowPosition.width = float.Parse(node.GetValue("camWinWidth"));
                        cameraWindowPosition.height = float.Parse(node.GetValue("camWinHeight"));

                        Utils.Log.Info("LoadSettings, cameraWindowPosition: " + cameraWindowPosition);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
