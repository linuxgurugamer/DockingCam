//#define KSP170  // Better defined in the project file
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using OLDD_camera.Utils;
using UnityEngine;
using ClickThroughFix;

namespace OLDD_camera.Camera
{
    public abstract class BaseCamera
    {

        protected static int WindowCount;
        protected static double ElectricChargeAmount;
        internal double electricchargeCost = 0.02d;
        public static Material CurrentShader;
        protected UpdateGUIObject UpdateGUIObject;

        internal Rect WindowPosition = new Rect(0, 400, 100, 100);
        static Rect _lastWindowPosition;
        internal Rect TexturePosition;
        internal float sidebarWidthOffset = 0; // used for scaling stock KSP skin

        protected string WindowLabel;
        protected string SubWindowLabel;
        protected GameObject PartGameObject;
        protected Part ThisPart;

        //private Texture _textureBackGroundCamera;
        private Texture _textureSeparator;
        private Texture _textureTargetMark;
        internal Texture[] TextureNoSignal;
        internal int TextureNoSignalId;
        protected RenderTexture RenderTexture;

        //private ShaderType _shaderType;
        private int _shaderIndex = 0;

        private static string _currentShaderName;
        //internal static int ShadersToUse = 0;

        protected float WindowSize;
        private const float WindowAddition = 92f;
        protected float RotateX = 0;
        protected float RotateY = 0;
        protected float RotateZ = 0;

        protected int MinZoomMultiplier = 4;
        internal float MinZoom = 1f;
        internal float MaxZoom = 32f;
        internal float CurrentZoom = 32f;
        internal int CalculatedZoom;
        internal bool ZoomMultiplier = false;

        private bool _isTargetPoint;

        protected int WindowSizeCoef = 2;
        protected int MaxWindowSizeCoef = 5;

        protected int WindowId = Util.GetRandomInt();
        public static int SettingsWinID = Util.GetRandomInt();
        public static int HelpWinId = Util.GetRandomInt();

        internal bool IsActive;
        internal bool IsButtonOff;
        internal bool IsOrbital;
        internal bool IsAuxiliaryWindowOpen;
        internal bool AuxWindowAllowed = true;
        internal bool IsAuxiliaryWindowButtonPres;
        internal bool IsZoomAllowed = true;

        protected List<UnityEngine.Camera> AllCameras = new List<UnityEngine.Camera>();
        protected List<GameObject> AllCamerasGameObject = new List<GameObject>();
        protected List<string> CameraNames = new List<string> { "GalaxyCamera", "Camera ScaledSpace", "Camera 00" };

        internal ShaderInfo availableShaders = null;

        Modules.DockingCameraModule dcm;
        public string photoDir = "DockingCam";

        public void SetDCM(Modules.DockingCameraModule dcm)
        {
            this.dcm = dcm;
        }

        public void SetPhotoDir(string dir)
        {
            Log.Info("SetPhotoDir: " + dir);
            photoDir = dir;
        }
        protected BaseCamera(Part thisPart, float windowSizeInit, string windowLabel = "Camera")
        {
            InitBaseCamera(thisPart, windowSizeInit, windowLabel);
        }
        float oWindowSizeInit;

        void InitBaseCamera(Part thisPart, float windowSizeInit, string windowLabel = "Camera", bool firstTime = true)
        {
            oWindowSizeInit = windowSizeInit;
            WindowSize = GameSettings.UI_SCALE * windowSizeInit / 2;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
            {
                sidebarWidthOffset = 40 * GameSettings.UI_SCALE;
                WindowSize += sidebarWidthOffset;
            }
            else
                sidebarWidthOffset = 0;

            MaxWindowSizeCoef = 1;
            while ((MaxWindowSizeCoef + 2) * WindowSize < Screen.height && MaxWindowSizeCoef < 10)
                MaxWindowSizeCoef++;
            WindowSizeCoef = Math.Min(HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().defaultCamWindowSize, MaxWindowSizeCoef);
            ThisPart = thisPart;
            SubWindowLabel = windowLabel;
            WindowLabel = windowLabel;
            PartGameObject = thisPart.gameObject;

            InitWindow();
            InitTextures();
            if (firstTime)
            {
                GameEvents.OnFlightUIModeChanged.Add(FlightUIModeChanged);
                GameEvents.onUIScaleChange.Add(onUIScaleChange);

                GameObject updateGUIHolder = new GameObject();
                UpdateGUIObject = updateGUIHolder.AddComponent<UpdateGUIObject>();
                LoadWinSettings();
            }
        }

        ~BaseCamera()
        {
            GameEvents.OnFlightUIModeChanged.Remove(FlightUIModeChanged);
            GameEvents.onUIScaleChange.Remove(onUIScaleChange);
        }

        void onUIScaleChange()
        {
            InitBaseCamera(ThisPart, oWindowSizeInit, WindowLabel, false);
        }

        private void FlightUIModeChanged(FlightUIMode mode)
        {
            if (ThisPart != null && ThisPart.vessel != null)
                IsOrbital = ThisPart.vessel.situation == Vessel.Situations.ORBITING;
            else
                IsOrbital = false;
#if false // This was pre 1.7.0

            IsOrbital = mode == FlightUIMode.ORBITAL;
#endif
        }

        protected virtual void InitWindow()
        {
            WindowPosition.width = WindowSize * WindowSizeCoef;
            WindowPosition.height = WindowSize * WindowSizeCoef + 34f;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                sidebarWidthOffset = 40 * GameSettings.UI_SCALE;
            else
                sidebarWidthOffset = 0;

        }

        protected virtual void InitTextures()
        {
            float lineHeight;
            float titleHeight;

            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
            {
                lineHeight = 14 * GameSettings.UI_SCALE;
                titleHeight = 34;
            }
            else
            {
                lineHeight = 12 * GameSettings.UI_SCALE;
                titleHeight = 22;
            }

            // TexturePosition = new Rect(6, 34, WindowPosition.width - 12f, WindowPosition.height - 40f); //42f);

            TexturePosition = new Rect(6, lineHeight + titleHeight, WindowPosition.width - lineHeight, WindowPosition.height - (lineHeight + /*28f +*/ titleHeight + 6f)); //42f);


            RenderTexture = new RenderTexture((int)WindowSize * 4, (int)WindowSize * 4, 24);//, RenderTextureFormat.RGB565);  
            RenderTexture.active = RenderTexture;
            RenderTexture.Create();
            //_textureBackGroundCamera = Util.MonoColorRectTexture(new Color(0.45f, 0.45f, 0.45f, 1));
            try
            {
                _textureSeparator = Util.MonoColorVerticalLineTexture(Color.white, (int)TexturePosition.height);
            }
            catch (Exception e)
            {
                Debug.Log("OLDD.BaseCamera.InitTextures, Exception: : " + e.Message);
                Debug.Log("OLDD.BaseCamera.InitTextures, TexturePosition.height has invalid value: " + (int)TexturePosition.height);
                Debug.Log("OLDD.BaseCamera.InitTextures, TexturePosition value: " + TexturePosition);
                Debug.Log("OLDD.BaseCamera.InitTextures, lineHeight value: " + lineHeight);
                Debug.Log("OLDD.BaseCamera.InitTextures, titleHeight value: " + titleHeight);
                TexturePosition.height = 1f;
            }

            _textureTargetMark = AssetLoader.texTargetPoint;
            TextureNoSignal = new Texture[8];
            for (int i = 0; i < TextureNoSignal.Length; i++)
            {
                TextureNoSignal[i] = Util.WhiteNoiseTexture((int)TexturePosition.width, (int)TexturePosition.height, 1f);
            }
        }

        protected virtual void InitCameras()
        {
            IsAuxiliaryWindowOpen = false;
            IsAuxiliaryWindowButtonPres = false;

            AllCamerasGameObject = CameraNames.Select(a => new GameObject()).ToList();
            AllCameras = AllCamerasGameObject.Select((go, i) =>
                {
                    var camera = go.AddComponent<UnityEngine.Camera>();
                    UnityEngine.Camera cameraExample = UnityEngine.Camera.allCameras.FirstOrDefault(cam => cam.name == CameraNames[i]);
                    if (cameraExample != null)
                    {
                        camera.CopyFrom(cameraExample);
                        camera.name = string.Format("{1} copy of {0}", CameraNames[i], WindowCount);
                        camera.targetTexture = RenderTexture;
                        if (i == 2 && dcm != null && dcm.cameraCustomNearClipPlane > 0)
                            camera.nearClipPlane = dcm.cameraCustomNearClipPlane;
                    }
                    return camera;
                }).ToList();
        }

        protected virtual void DestroyCameras()
        {
            AllCameras.ForEach(UnityEngine.Object.Destroy);
            AllCameras.Clear();
        }


        /// <summary>
        /// Create and activate cameras
        /// </summary>
        public virtual void Activate()
        {
            if (IsActive) return;
            WindowCount++;
            InitCameras();
            IsActive = true;
            //UpdateGUIObject.UpdateGUIFunction += Begin; //ddd
            if (UpdateGUIObject != null)
                UpdateGUIObject.updateGUIFunction += Begin; //lll
            else
                Log.Error("UpdateGUIObject is null");
        }

        /// <summary>
        /// Destroy  cameras
        /// </summary>
        public virtual void Deactivate()
        {
            if (!IsActive) return;
            WindowCount--;
            DestroyCameras();
            IsActive = false;
            //UpdateGUIObject.UpdateGUIFunction -= Begin; //ddd
            if (UpdateGUIObject != null)
                UpdateGUIObject.updateGUIFunction -= Begin; //lll
        }

        public void setECusageCost(double ECAmount)
        {
            electricchargeCost = ECAmount;
        }

        private void Begin() //draw main window
        {
            if (!IsActive) return;
            //if (MapView.MapIsEnabled) return;
            WindowPosition = ClickThruBlocker.GUIWindow(WindowId, KSPUtil.ClampRectToScreen(WindowPosition), DrawWindow, WindowLabel);
            int electricityId = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
            double electricChargeAmount;
            double electricChargeMaxAmount;
            ThisPart.GetConnectedResourceTotals(electricityId, out electricChargeAmount, out electricChargeMaxAmount);
            if (HighLogic.LoadedSceneIsFlight && !FlightDriver.Pause)
                ThisPart.RequestResource(electricityId, electricchargeCost * TimeWarp.fixedDeltaTime);
        }

        #region DRAW LAYERS 

        private void DrawWindow(int id)
        {
            ExtendedDrawWindowL1();
            ExtendedDrawWindowL2();
            ExtendedDrawWindowL3();
            GUI.DragWindow();


            if (WindowPosition.x == _lastWindowPosition.x && WindowPosition.y == _lastWindowPosition.y) return;
            _lastWindowPosition.x = WindowPosition.x;
            _lastWindowPosition.y = WindowPosition.y;

            WindowSettings.SaveCameraWinSettings(WindowPosition);
        }



        internal bool takePic = false;

        RenderTexture _renderTextureColor;
        RenderTexture _renderTextureDepth;

        public Texture2D GetPic()
        {
            RenderTexture CurrentRT = RenderTexture.active;

            Texture2D imageTexture = new Texture2D(dcm.cameraHorizontalResolution, dcm.cameraVerticalResolution, TextureFormat.RGB24, false);

            if (_renderTextureColor == null)
            {
                _renderTextureColor = new RenderTexture(dcm.cameraHorizontalResolution, dcm.cameraVerticalResolution, 0);
                _renderTextureDepth = new RenderTexture(dcm.cameraHorizontalResolution, dcm.cameraVerticalResolution, 24);
                _renderTextureColor.Create();
                _renderTextureDepth.Create();
            }
            RenderTexture.active = _renderTextureColor;


            foreach (var a in AllCameras)
            {
                a.SetTargetBuffers(_renderTextureColor.colorBuffer, _renderTextureDepth.depthBuffer);
                a.Render();
            }

            imageTexture.ReadPixels(new Rect(0, 0, dcm.cameraHorizontalResolution, dcm.cameraVerticalResolution), 0, 0);
            imageTexture.Apply();

            _renderTextureColor.Release();
            _renderTextureDepth.Release();
            _renderTextureColor = null;
            _renderTextureDepth = null;

            foreach (var a in AllCameras)
                a.targetTexture = RenderTexture;

            RenderTexture.active = CurrentRT;


            return imageTexture;
        }

        /* ************************************************************************************************
         * Function Name: WriteTextureToDrive
         * Input: The texture object that will be written to the hard drive.
         * Output: None
         * Purpose: This function will take an input texture and then convert it to a png file in the 
         * DockingCam subfolder of the Screenshot folder.
         * This currently has some bugs.
         * If Linux users complain about screenshots not saving to the disk, then this is the first place
         * to look.
         * ************************************************************************************************/
        private void WriteTextureToDrive(Texture2D Input)
        {
            Util.WritePng(Input, photoDir, ThisPart.vessel.vesselName);
        }




        /// <summary>
        /// drawing method, first layer, for cameras
        /// </summary>
        protected virtual void ExtendedDrawWindowL1()
        {
            if (AssetLoader.materials == null)
                return;
            var widthOffset = WindowPosition.width - 90;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
            {
                widthOffset -= sidebarWidthOffset;
            }

            var calculateZoom = (int)(MaxZoom - CurrentZoom + MinZoom);
            CalculatedZoom = !ZoomMultiplier ? calculateZoom : calculateZoom * MinZoomMultiplier * 6;
            if (IsAuxiliaryWindowOpen)
            {
                GUI.Label(new Rect(widthOffset, 22, 80, 20), "Zoom: " + CalculatedZoom, Styles.Label13B);

                if (dcm != null && dcm.isDockingNode && FlightGlobals.ActiveVessel == ThisPart.vessel)
                    _isTargetPoint = GUI.Toggle(new Rect(widthOffset - 2, 233, 88, 20), _isTargetPoint, "Target Mark");
            }
            //GUI.DrawTexture(TexturePosition, _textureBackGroundCamera);
            //Log.Info("ExtendedDrawWindowL1 4, _shaderIndex: " + _shaderIndex);
            //Log.Info("ExtendedDrawWindowL1 4.1 AssetLoader.materials.Count:" + AssetLoader.materials.Count);

            CurrentShader = AssetLoader.materials[_shaderIndex];
            //Log.Info("ExtendedDrawWindowL1 5");

            _currentShaderName = CurrentShader == null ? "none" : CurrentShader.name;
            //Log.Info("ExtendedDrawWindowL1 6");

            if (Event.current.type.Equals(EventType.Repaint))
            {
                //Log.Info("ExtendedDrawWindowL1 7");
                Graphics.DrawTexture(TexturePosition, Render(), CurrentShader);
                if (takePic)
                {
                    //RenderTexture.SavePng(ThisPart.vessel.vesselName);
                    //takePic = false;
                    //return;
                    WriteTextureToDrive(GetPic());

                    takePic = false;
                }
            }

        }

        /// <summary>
        /// drawing method, second layer (draw any texture between cam and interface)
        /// </summary>
        protected virtual void ExtendedDrawWindowL2()
        {

            if (TargetHelper.IsTargetSelect)
            {
                var camera = AllCameras.Last();
                var vessel = TargetHelper.Target as Vessel;

                if (vessel == null)
                {
                    var targetedDockingNode = TargetHelper.Target as ModuleDockingNode;
                    vessel = targetedDockingNode.vessel;
                }

                var point = camera.WorldToViewportPoint(vessel.transform.position); //get current targeted vessel 
                var x = point.x; //(0;1)
                var y = point.y;
                var z = point.z;

                x = GetX(x, z);
                y = GetY(y, z);

                var offsetX = TexturePosition.width * x;
                var offsetY = TexturePosition.height * y;

                if (_isTargetPoint)
                    GUI.DrawTexture(new Rect(TexturePosition.xMin + offsetX - 10, TexturePosition.yMax - offsetY - 10, 20, 20), _textureTargetMark);
            }
#if false
            else
            {
                if (IsOrbital)
                    GUI.DrawTexture(TexturePosition, TextureNoSignal[TextureNoSignalId]);
            }
#endif
        }

        /// <summary>
        /// drawing method, third layer, interface
        /// </summary>
        protected virtual void ExtendedDrawWindowL3()
        {
            if (AssetLoader.materials == null)
                return;

            if (!ThisPart.vessel.Equals(FlightGlobals.ActiveVessel))
                GUI.Label(new Rect(8, 34, 222, 20), "Broadcast from: " + ThisPart.vessel.vesselName, Styles.GreenLabel11);

            if (IsAuxiliaryWindowOpen)
                GUI.DrawTexture(new Rect(TexturePosition.width + 8, 34, GameSettings.UI_SCALE * 1, TexturePosition.height), _textureSeparator); //Separator
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
            {
                if (GUI.Button(new Rect(WindowPosition.width - 20, 3, GameSettings.UI_SCALE * 15, GameSettings.UI_SCALE * 15), "x"))
                    IsButtonOff = true;
            }
            else
            {
                if (GUI.Button(new Rect(WindowPosition.width - 20, 3, GameSettings.UI_SCALE * 15 + 4, GameSettings.UI_SCALE * 15), "x"))
                    IsButtonOff = true;
            }
            if (AuxWindowAllowed)
            {
                if (GUI.Button(new Rect(WindowPosition.width - 29, 20, GameSettings.UI_SCALE * 24, GameSettings.UI_SCALE * 15), IsAuxiliaryWindowOpen ? "◄" : "►")) //extend window
                {
                    IsAuxiliaryWindowOpen = !IsAuxiliaryWindowOpen;
                    IsAuxiliaryWindowButtonPres = true;
                }
            }

            // This assumes that the shader name has a slash in it
            var tooltip = new GUIContent("☼", _currentShaderName.Split('/')[1]);

            GUI.Box(new Rect(8, TexturePosition.yMax - 22, GameSettings.UI_SCALE * 20, GameSettings.UI_SCALE * 20), tooltip);
            GUI.Label(new Rect(64, 128, GameSettings.UI_SCALE * 200, GameSettings.UI_SCALE * 40), GUI.tooltip, Styles.YellowLabel13);
            if (GUI.Button(new Rect(8, TexturePosition.yMax - 22, 20, 20), "☼"))
            {
                if (_shaderIndex == AssetLoader.materials.Count - 1)
                    _shaderIndex = 0;
                else
                    _shaderIndex++;
            }

            if (GUI.RepeatButton(new Rect(TexturePosition.xMax - 42, TexturePosition.yMax - 22, 20, 20), "-") &&
                UnityEngine.Camera.allCameras.FirstOrDefault(x => x.name == "Camera 00") != null) //Size of main window
            {
                WindowSizeCoef--;
                if (WindowSizeCoef < 2)
                    WindowSizeCoef = MaxWindowSizeCoef;

                DoResizeWindow(WindowSizeCoef);
                //IsAuxiliaryWindowOpen = false;

                IsAuxiliaryWindowButtonPres = IsAuxiliaryWindowOpen;
            }
            if (GUI.RepeatButton(new Rect(TexturePosition.xMax - 22, TexturePosition.yMax - 22, 20, 20), "+") &&
                UnityEngine.Camera.allCameras.FirstOrDefault(x => x.name == "Camera 00") != null) //Size of main window
            {
                WindowSizeCoef = ((WindowSizeCoef - 1) % (MaxWindowSizeCoef - 1)) + 2;

                DoResizeWindow(WindowSizeCoef);

                IsAuxiliaryWindowButtonPres = IsAuxiliaryWindowOpen;
            }
            if (IsZoomAllowed)
                CurrentZoom = GUI.HorizontalSlider(new Rect(TexturePosition.width / 2 - 80, GUI.skin.font.lineHeight + 10, 160, 10), CurrentZoom, MaxZoom, MinZoom);
        }

        internal void DoResizeWindow(int WindowSizeCf)
        {
            WindowSizeCoef = WindowSizeCf;
            bool isActive = IsActive;
            if (isActive)
                Deactivate();
            InitWindow();
            InitTextures();
            if (isActive)
                Activate();

            WindowSettings.SaveSettings(WindowSizeCoef);
        }

        void LoadWinSettings()
        {
            if (WindowSettings.LoadSettings())
            {
                WindowSizeCoef = WindowSettings.WindowSizeCoef;
                WindowPosition = WindowSettings.cameraWindowPosition;
            }
            DoResizeWindow(WindowSizeCoef);
        }
        #endregion DRAW LAYERS

        private float GetX(float x, float z)
        {
            if (x < 0 && z > 0 && x <= 0) return 0f;
            if (x > 0 && z < 0) return 0f;
            if (x < 0 && z < 0) return 1f;
            if (x > 0 && z > 0 && x >= 1) return 1f;
            return x;
        }
        private float GetY(float y, float z)
        {
            if (z > 0)
            {
                if (y <= 0f) return 0f;
                if (y >= 1f) return 1f;
            }
            if (z < 0)
            {
                if (y <= 0) return 0f;
                if (y >= 1f) return 1f;
            }
            return y;
        }

        protected virtual RenderTexture Render()
        {
            AllCameras.ForEach(a => a.Render());
            return RenderTexture;
        }

        public IEnumerator ResizeWindow()
        {
            IsAuxiliaryWindowButtonPres = false;
            while (true)
            {
                if (IsAuxiliaryWindowOpen && WindowPosition.width < WindowSize * WindowSizeCoef + WindowAddition + sidebarWidthOffset)
                {
                    WindowPosition.width += 4;
                    if (WindowPosition.width >= WindowSize * WindowSizeCoef + WindowAddition + sidebarWidthOffset)
                        break;
                }
                else if (WindowPosition.width > WindowSize * WindowSizeCoef)
                {
                    WindowPosition.width -= 4;
                    if (WindowPosition.width <= WindowSize * WindowSizeCoef)
                        break;
                }
                else
                    break;
                //yield return new WaitForSeconds(1f / 92f); //ddd
                yield return new WaitForSeconds(0.0108695654f); //lll
            }

        }

        protected void UpdateWhiteNoise()
        {
            TextureNoSignalId++;
            if (TextureNoSignalId >= TextureNoSignal.Length)
                TextureNoSignalId = 0;
        }

        /// <summary>
        /// Update position and rotation of the cameras
        /// </summary>
        public virtual void Update()
        {
            if (IsActive)
            {
                AllCamerasGameObject.Last().transform.position = PartGameObject.transform.position;
                AllCamerasGameObject.Last().transform.rotation = PartGameObject.transform.rotation;

                AllCamerasGameObject.Last().transform.Rotate(new Vector3(-1f, 0, 0), 90);
                AllCamerasGameObject.Last().transform.Rotate(new Vector3(0, 1f, 0), RotateY);
                AllCamerasGameObject.Last().transform.Rotate(new Vector3(1f, 0, 0), RotateX);
                AllCamerasGameObject.Last().transform.Rotate(new Vector3(0, 0, 1f), RotateZ);

                AllCamerasGameObject[0].transform.rotation = AllCamerasGameObject.Last().transform.rotation;
                AllCamerasGameObject[1].transform.rotation = AllCamerasGameObject.Last().transform.rotation;
                AllCameras.ForEach(cam => cam.fieldOfView = CurrentZoom);
            }
        }
    }
}
