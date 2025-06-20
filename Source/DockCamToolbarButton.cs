using KSP.Localization;
using ClickThroughFix;
using KSP.UI.Screens;
using OLDD_camera.Camera;
using OLDD_camera.Modules;
using OLDD_camera.Utils;
using System.Collections.Generic;
using ToolbarControl_NS;
using UnityEngine;

namespace OLDD_camera
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DockCamToolbarButton : MonoBehaviour
    {
        const float WINX = 132;
        const float WINY = 179;
        const float WINWIDTH = 306;
        const float WINHEIGHT = 230;

        internal static DockCamToolbarButton instance;
        //private static PluginConfiguration _config;
        private static List<Vessel> _vesselsWithCamera = new List<Vessel>();
        private static List<Vessel> _vesselsWithDockingCamera = new List<Vessel>();
        private static List<Vessel> _vesselsWithAttachedCamera = new List<Vessel>();

        private static Rect _lastWindowPosition;
        private static Rect _windowPosition = new Rect(WINX, WINY, WINWIDTH, WINHEIGHT);
        private static Rect helpWindowPosition;
        private static readonly VesselRanges DefaultRanges = PhysicsGlobals.Instance.VesselRangesDefault;
        //private readonly VesselRanges.Situation _myRanges = new VesselRanges.Situation(10000, 10000, 2500, 2500);
        private readonly VesselRanges.Situation _myRanges = new VesselRanges.Situation(10000, 12000, 3500, 2000);
        private const int WINDOW_WIDTH = 256;
        private static bool _showWindow;
        //private static bool _shadersToUse0 = true;
        //private static bool _shadersToUse1;
        //private static bool _shadersToUse2;
        //private static bool _dist2500 = true;
        //private static bool _dist9999;
        private bool mainWindowVisible;
        static private bool altOverlayHelpVisible;
        // private readonly string _modulePartCameraId = "PartCameraModule";

        //public static bool FCS;
        static public bool hideUI = false;


        public void Awake()
        {
            instance = this;
            mainWindowVisible = false;
            altOverlayHelpVisible = false;

            helpWindowPosition = new Rect((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400);
        }

        public void Start()
        {
            //LoadWindowData();
            if (WindowSettings.LoadSettings())
            {
                _windowPosition = WindowSettings.windowPosition;
            }
            OnAppLauncherReady();
            if (!HighLogic.LoadedSceneIsFlight) return;
            GameEvents.onVesselCreate.Add(NewVesselCreated);
            GameEvents.onVesselChange.Add(NewVesselCreated);
            GameEvents.onVesselLoaded.Add(NewVesselCreated);
            GameEvents.onVesselsUndocking.Add(VesselsUndocked);

            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
        }

        private void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(NewVesselCreated);
            GameEvents.onVesselChange.Remove(NewVesselCreated);
            GameEvents.onVesselLoaded.Remove(NewVesselCreated);
            GameEvents.onVesselsUndocking.Remove(VesselsUndocked);
            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);

            toolbarControl.OnDestroy();
            Destroy(toolbarControl);
            toolbarControl = null;
        }
        void onHideUI()
        {
            hideUI = true;
        }
        void onShowUI()
        {
            hideUI = false;
        }
        public void Update()
        {
            _showWindow = mainWindowVisible && HighLogic.LoadedSceneIsFlight && !FlightGlobals.ActiveVessel.isEVA; // && !MapView.MapIsEnabled;
        }

        private void OnGUI()
        {
            if (!hideUI && mainWindowVisible && HighLogic.LoadedSceneIsFlight) // && !MapView.MapIsEnabled)
            {
                if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                {
                    GUI.skin = HighLogic.Skin;
                }
                if (OLDD_camera.Utils.Styles.KspSkin != HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                {
                    OLDD_camera.Utils.Styles.InitStyles();
                }
                OnWindowOLDD();

                if (altOverlayHelpVisible)
                {
                    if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                    {
                        GUI.skin = HighLogic.Skin;
                    }
                    OnWindowAltHelp();
                }
            }
        }
        private void ShowMainWindow() { mainWindowVisible = true; }

        private void HideMainWindow() { mainWindowVisible = false; }

        private ToolbarControl toolbarControl;
        internal const string MODID = "Docking_Camera_KURS";
        internal const string MODNAME = "Docking Camera KURS";

        #region NO_LOCALIZATION
        private void OnAppLauncherReady()
        {
            if (toolbarControl != null) return;
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(ShowMainWindow, HideMainWindow,
                ApplicationLauncher.AppScenes.FLIGHT,
                MODID,
                "DockingCameraButton",
                "DockingCamKURS/Icons/DockingCamIcon32",
                "DockingCamKURS/Icons/DockingCamIcon",
                MODNAME);
        }
        #endregion

        private static void OnWindowOLDD()
        {
            if (!_showWindow) return;
            _windowPosition.width = WINDOW_WIDTH;
            var vesselsCount = _vesselsWithCamera.Count;
            var height = 20 * vesselsCount;
            _windowPosition.height = 140 + height + 10 + 60;
            _windowPosition.width += 50;

            _windowPosition = Util.ConstrainToScreen(ClickThruBlocker.GUIWindow(BaseCamera.SettingsWinID, _windowPosition, DrawOnWindowOLDD, Localizer.Format("#LOC_DockingCam_1")), 100);
        }

        private static void OnWindowAltHelp()
        {
            helpWindowPosition = ClickThruBlocker.GUIWindow(BaseCamera.HelpWinId, helpWindowPosition, DrawOnWindowAltHelp, Localizer.Format("#LOC_DockingCam_2"));

        }
        private static string vesselStr(int i)
        {
            if (i == 1)
                return Localizer.Format("#LOC_DockingCam_3");
            else
                return Localizer.Format("#LOC_DockingCam_4");
        }
        private static void DrawOnWindowAltHelp(int winId)
        {
            //GUILayout.BeginHorizontal();
            //GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#LOC_DockingCam_5"));
            GUILayout.Label(Localizer.Format("#LOC_DockingCam_6"));
            GUILayout.Label(Localizer.Format("#LOC_DockingCam_7"));

            GUILayout.Label(Localizer.Format("#LOC_DockingCam_8"));

            GUILayout.Label(Localizer.Format("#LOC_DockingCam_9"));

            GUILayout.Label(Localizer.Format("#LOC_DockingCam_10"));
            GUILayout.Label(Localizer.Format("#LOC_DockingCam_11"));
            GUILayout.Label("Roll is the difference in lining up the two docking ports rotationally");
            GUILayout.Label(Localizer.Format("#LOC_DockingCam_12"));
            if (GUILayout.Button(Localizer.Format("#LOC_DockingCam_13")))
                altOverlayHelpVisible = false;
            //GUILayout.EndHorizontal();
            //GUILayout.EndVertical();
            GUI.DragWindow();
        }
#if false
I really like the idea of DockingCam
by the amazing u/linuxgurugamer over that of Docking Port Alignment Indicator. However, the overlay provided by DockingCam doesn't really help with docking.

The xyz velocities and distances in the text overlay are in the game coordinate frame. Not in the coordinate frame of either the current docking port or the target docking port. This makes the velocities and distances displayed pretty useless. Furthermore, the crosshairs are also not very useful and the attitude crosshair leaves the window, which can make it hard to find the correct attitude without looking at the attitude text overlay. The attitude crosshair also only shows pitch and yaw, no roll. There's also not really a crosshair that helps with getting the right velocity for docking.

To solve these issues, I first transformed both the velocities and the distances to the coordinate frame of the current docking port. I then made sure the attitude crosshair is always in the window. The attitude crosshair still doesn't give any indication as to the roll of the vessel. I removed the red crosshair. I'm still not really certain what that does exactly, but I haven't really given it much thought. I added two more crosshairs. One red/green crosshair and one yellow/magenta crosshair. The red/green crosshair basically shows the desired velocity. It turns green when the target docking port is in front of you in the z axis and it turns red when it's behind you. The yellow/magenta crosshair basically shows the current velocity. It turns yellow when you're moving forward in the z dimension and it turns magenta when you're moving backward in the z dimension. Neither of the crosshairs can ever leave the window.

Basically all you have to do to dock is make sure your attitude is correct by aligning the blue crosshair with the center crosshair and making sure the pitch, yaw and roll gauges all show 0 degrees. You then have to make sure the desired velocity gauge is green by moving behind the target docking port. Once you've done that, aligning the yellow crosshair with the green crosshair can get you to dock, but it's easier to first center the green crosshair and then move toward the target docking port.

I've really found this mod mod makes docking a whole lot easier. All you have to do to install this mod is first install the DockingCam mod and then replacing the DockingCamera.dll and DockingCamera.dll.mdb in the Plugins folder with the ones I've provided. I'm not yet finished with the modifications, but I wonder what you think of these overlays
#endif

        private static void DrawOnWindowOLDD(int windowID)
        {
            var checkDist = FlightGlobals.ActiveVessel.vesselRanges.landed.load;
            var unloadDistance = Localizer.Format("#LOC_DockingCam_14") + FlightGlobals.ActiveVessel.vesselRanges.landed.load;
            GUILayout.Label(unloadDistance, Styles.Label13B);

            var vessels = _vesselsWithCamera;
            vessels.Remove(FlightGlobals.ActiveVessel);
            //DockCamToolbarButton.instance.NewVesselCreated(FlightGlobals.ActiveVessel);

            GUILayout.Label(" " + _vesselsWithAttachedCamera.Count + " " + vesselStr(_vesselsWithAttachedCamera.Count) + Localizer.Format("#LOC_DockingCam_15"), Styles.GreenLabel15B);
            GUILayout.Label(" " + _vesselsWithDockingCamera.Count + " " + vesselStr(_vesselsWithDockingCamera.Count) + Localizer.Format("#LOC_DockingCam_16"), Styles.GreenLabel15B);

            if (vessels.Count > 1)
            {
                foreach (var vessel in vessels)
                {
                    var i = vessels.IndexOf(vessel) + 1;
                    var range = Mathf.Round(Vector3.Distance(vessel.transform.position, FlightGlobals.ActiveVessel.transform.position));
                    var situation = vessel.RevealSituationString();
                    var str = $"{i}" + "." + " {v}" + "ssel.vesselName" + "}" +  "(" + "{r}" + "nge:N" + "}" +  "m) -" + " {s}" + "tuation" + "}";
                    if (range <= checkDist)
                        GUILayout.Label(str, Styles.GreenLabel13);
                    else
                        GUILayout.Label(str);
                }
            }
            HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showCross =
                GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showCross, Localizer.Format("#LOC_DockingCam_17"));
            HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showSummaryData =
                GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showSummaryData, Localizer.Format("#LOC_DockingCam_18"));
            HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showData =
                GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showData, Localizer.Format("#LOC_DockingCam_19"));
            HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showDials =
                GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().showDials, Localizer.Format("#LOC_DockingCam_20"));

            HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_2>().altOverlay =
                GUILayout.Toggle(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_2>().altOverlay, Localizer.Format("#LOC_DockingCam_21"));
            if (GUILayout.Button(Localizer.Format("#LOC_DockingCam_2")))
            {
                altOverlayHelpVisible = !altOverlayHelpVisible;
            }

            GUI.DragWindow();

            if (_windowPosition.x == _lastWindowPosition.x && _windowPosition.y == _lastWindowPosition.y) return;
            _lastWindowPosition.x = _windowPosition.x;
            _lastWindowPosition.y = _windowPosition.y;
            //SaveWindowData();
            WindowSettings.SaveWinSettings(_windowPosition);
        }

        private void VesselsUndocked(Vessel d1, Vessel d2)
        {
            NewVesselCreated(d1);
        }

        private void NewVesselCreated(Vessel data)
        {
            //return;
            var allVessels = FlightGlobals.Vessels;
            _vesselsWithCamera = GetVesselsWithCamera(allVessels);
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>()._dist2500)
            {
                foreach (var vessel in _vesselsWithCamera)
                {
                    vessel.vesselRanges.subOrbital = _myRanges;
                    vessel.vesselRanges.landed = _myRanges;
                    vessel.vesselRanges.escaping = _myRanges;
                    vessel.vesselRanges.orbit = _myRanges;
                    vessel.vesselRanges.prelaunch = _myRanges;
                    vessel.vesselRanges.splashed = _myRanges;
                }
            }
            else
            {
                foreach (var vessel in _vesselsWithCamera)
                {
                    #region NO_LOCALIZATION
#if false
                    LogRanges("DefaultRanges.subOrbital: " , DefaultRanges.subOrbital);
                    LogRanges("DefaultRanges.landed: " , DefaultRanges.landed);
                    LogRanges("DefaultRanges.escaping: " , DefaultRanges.escaping);
                    LogRanges("DefaultRanges.orbit: " , DefaultRanges.orbit);
                    LogRanges("DefaultRanges.prelaunch: " , DefaultRanges.prelaunch);
                    LogRanges("DefaultRanges.splashed: " , DefaultRanges.splashed);
#endif
                    #endregion

                    vessel.vesselRanges.subOrbital = DefaultRanges.subOrbital;
                    vessel.vesselRanges.landed = DefaultRanges.landed;
                    vessel.vesselRanges.escaping = DefaultRanges.escaping;
                    vessel.vesselRanges.orbit = DefaultRanges.orbit;
                    vessel.vesselRanges.prelaunch = DefaultRanges.prelaunch;
                    vessel.vesselRanges.splashed = DefaultRanges.splashed;
                }
            }
        }
#if false
        void LogRanges(string n, VesselRanges.Situation vr)
        {
            Log.Info("LogRanges: " + n + ", load: " + vr.load + ", unload: " + vr.unload + ", pack: " + vr.pack + ", unpack: " + vr.unpack);
        }
#endif
        private List<Vessel> GetVesselsWithCamera(List<Vessel> allVessels)
        {
            List<Vessel> vesselsWithCamera = new List<Vessel>();
            //List<Vessel> _vesselsWithDockingCamera = new List<Vessel>();
            //List<Vessel> _vesselsWithAttachedCamera = new List<Vessel>();

            foreach (var vessel in allVessels)
            {
                if (vessel.Parts.Count == 0) continue;
                if (vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.Flag || vessel.vesselType == VesselType.Unknown) continue;

                foreach (var part in vessel.Parts)
                {
                    if (!part.Modules.Contains<PartCameraModule>() && !part.Modules.Contains<DockingCameraModule>()) continue;
                    if (vesselsWithCamera.Contains(vessel)) continue;
                    if (part.Modules.Contains<PartCameraModule>() && !_vesselsWithAttachedCamera.Contains(vessel))
                        _vesselsWithAttachedCamera.Add(vessel);
                    if (part.Modules.Contains<DockingCameraModule>() && !_vesselsWithDockingCamera.Contains(vessel))
                        _vesselsWithDockingCamera.Add(vessel);
                    vesselsWithCamera.Add(vessel);
                    break;
                }
                if (_vesselsWithAttachedCamera.Contains(FlightGlobals.ActiveVessel))
                    _vesselsWithAttachedCamera.Remove(FlightGlobals.ActiveVessel);
                if (_vesselsWithDockingCamera.Contains(FlightGlobals.ActiveVessel))
                    _vesselsWithDockingCamera.Remove(FlightGlobals.ActiveVessel);

            }
            return vesselsWithCamera;
        }


#if false
        internal static void SaveWindowData(int WindowSizeCoef = 1)
        {
            _config.SetValue("toolbarWindowPosition", _windowPosition);
            //_config.SetValue("shadersToUse0", _shadersToUse0);
            //_config.SetValue("shadersToUse1", _shadersToUse1);
            //_config.SetValue("shadersToUse2", _shadersToUse2);

            
            //_config.SetValue("FCS", FCS);
            _config.save();
        }

        internal static void LoadWindowData()
        {
            _config = PluginConfiguration.CreateForType<DockCamToolbarButton>();
            _config.load();
            var defaultWindow = new Rect();
            _windowPosition = _config.GetValue("toolbarWindowPosition", defaultWindow);
            //_shadersToUse0 = _config.GetValue("shadersToUse0", _shadersToUse0);
            //_shadersToUse1 = _config.GetValue("shadersToUse1", _shadersToUse1);
            //_shadersToUse2 = _config.GetValue("shadersToUse2", _shadersToUse2);
            //FCS = _config.GetValue("FCS", FCS);
        }
#endif
    }
}
