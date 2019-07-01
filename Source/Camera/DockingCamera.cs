using System;
using System.Collections.Generic;
using System.Linq;
using OLDD_camera.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Camera
{
    class DockingCamera : BaseCamera
    {
        private static HashSet<int> _usedId = new HashSet<int>();
        private static List<Texture2D>[] _textureWhiteNoise;

        private int _id;
        private int _idTextureNoise;

        private Texture2D _textureVLineOLDD;
        private Texture2D _textureHLineOLDD;
        private Texture2D _textureVLine;
        private Texture2D _textureHLine;
        private Texture2D _textureVLineBack;
        private Texture2D _textureHLineBack;

        internal readonly GameObject _moduleDockingNodeGameObject;
        private TargetHelper _target;

        internal bool Noise;
        internal bool TargetCrossOLDD;
        internal bool TargetCrossDPAI;
        internal bool TargetCrossStock;

        private bool _cameraData = true;
        private bool _rotatorState = true;
        private readonly float _maxSpeed = 2;

        private Color _targetCrossColorOLDD = new Color(0, 0, 0.9f, 1);
        private Color _targetCrossColorDPAI = new Color(0.5f, .0f, 0, 1);
        private Color _targetCrossColorBack = new Color(.9f, 0, 0, 1);

        private string _lastVesselName;
        private string _windowLabelSuffix;

        Modules.DockingCameraModule _dcm;

        public Color TargetCrossColorOLDD
        {

            get { return _targetCrossColorOLDD; }
            set
            {
                _targetCrossColorOLDD = value;
                _textureVLineOLDD = Util.MonoColorVerticalLineTexture(_targetCrossColorOLDD, (int)WindowSize * WindowSizeCoef);
                _textureHLineOLDD = Util.MonoColorHorizontalLineTexture(_targetCrossColorOLDD, (int)WindowSize * WindowSizeCoef);
            }
        }

        public Color TargetCrossColorDPAI
        {
            get { return _targetCrossColorDPAI; }
            set
            {
                _targetCrossColorDPAI = value;
                _textureVLine = Util.MonoColorVerticalLineTexture(TargetCrossColorDPAI, (int)WindowSize * WindowSizeCoef);
                _textureHLine = Util.MonoColorHorizontalLineTexture(TargetCrossColorDPAI, (int)WindowSize * WindowSizeCoef);
            }
        }
        public Color TargetCrossColorBack
        {
            get { return _targetCrossColorBack; }
            set
            {
                _targetCrossColorBack = value;
                _textureVLineBack = Util.MonoColorVerticalLineTexture(TargetCrossColorBack, (int)WindowSize * WindowSizeCoef);
                _textureHLineBack = Util.MonoColorHorizontalLineTexture(TargetCrossColorBack, (int)WindowSize * WindowSizeCoef);
            }
        }
        

        public void UpdateLocalPosition(Modules.DockingCameraModule dcm)
        {
            _dcm = dcm;

            TargetCrossDPAI = dcm.crossDPAIonAtStartup;
            TargetCrossOLDD = dcm.crossOLDDonAtStartup;
            TargetCrossStock = dcm.targetCrossStockOnAtStartup;
            AuxWindowAllowed = dcm.slidingOptionWindow;

            Noise = dcm.noise;
        }

        public DockingCamera(OLDD_camera.Modules.DockingCameraModule dcm, Part thisPart,
            bool noise, bool crossStock, bool crossDPAI, bool crossOLDD, bool transformModification,
            int windowSize, string windowLabel = "DockCam", string cameraName = "dockingNode", 
            bool slidingOptionWindow = false, bool allowZoom = false, bool noTransformMod = false)
            : base(thisPart, windowSize, windowLabel)
        {
            GameEvents.onGameSceneLoadRequested.Add(LevelWasLoaded);
            Noise = noise;
            TargetCrossDPAI = crossDPAI;
            TargetCrossOLDD = crossOLDD;
            TargetCrossStock = crossStock;
            AuxWindowAllowed = slidingOptionWindow;
            IsZoomAllowed = allowZoom;
            
            _target = new TargetHelper(thisPart);
            _moduleDockingNodeGameObject = PartGameObject.GetChild(cameraName) ?? PartGameObject;  //GET orientation from dockingnode

            if (cameraName != "dockingNode" && transformModification)
            {
                Vector3 v3 = dcm.cameraPosition;

                _moduleDockingNodeGameObject.transform.position += _moduleDockingNodeGameObject.transform.rotation * v3;
                _moduleDockingNodeGameObject.transform.rotation = dcm.part.transform.rotation;
                _moduleDockingNodeGameObject.transform.rotation *= Quaternion.LookRotation(dcm.cameraForward, dcm.cameraUp);
            }

            if (_textureWhiteNoise == null)
            {
                _textureWhiteNoise = new List<Texture2D>[4];
                for (int j = 0; j < 3; j++)
                {
                    _textureWhiteNoise[j] = new List<Texture2D>();
                    for (int i = 0; i < 4; i++)
                        _textureWhiteNoise[j].Add(Util.WhiteNoiseTexture((int)TexturePosition.width, (int)TexturePosition.height));
                }
            }
        }

        private void LevelWasLoaded(GameScenes data)
        {
            _usedId = new HashSet<int>();
        }

        ~DockingCamera()
        {
            GameEvents.onGameSceneLoadRequested.Remove(LevelWasLoaded);
        }

        protected override void InitTextures()
        {
            base.InitTextures();
            _textureVLineOLDD = Util.MonoColorVerticalLineTexture(TargetCrossColorOLDD, (int)WindowSize * WindowSizeCoef);
            _textureHLineOLDD = Util.MonoColorHorizontalLineTexture(TargetCrossColorOLDD, (int)WindowSize * WindowSizeCoef);
            _textureVLine = Util.MonoColorVerticalLineTexture(TargetCrossColorDPAI, (int)WindowSize * WindowSizeCoef);
            _textureHLine = Util.MonoColorHorizontalLineTexture(TargetCrossColorDPAI, (int)WindowSize * WindowSizeCoef);
            _textureVLineBack = Util.MonoColorVerticalLineTexture(_targetCrossColorBack, (int)WindowSize * WindowSizeCoef);
            _textureHLineBack = Util.MonoColorHorizontalLineTexture(_targetCrossColorBack, (int)WindowSize * WindowSizeCoef);
        }

        protected override void ExtendedDrawWindowL1()
        {
            var widthOffset = WindowPosition.width - 92;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings>().useKSPskin)
            {
                widthOffset -= sidebarWidthOffset;
            }
            if (IsAuxiliaryWindowOpen)
            {
                TargetCrossStock = GUI.Toggle(new Rect(widthOffset, 124, 88, 20), TargetCrossStock, "Cross: stock");
                //if (TargetCrossStock)
                //    TargetCrossDPAI = TargetCrossOLDD = false;
                if (ThisPart.vessel.Equals(FlightGlobals.ActiveVessel) && TargetHelper.IsTargetSelect)
                {

                    if (_target != null && _target.IsDockPort)
                    {
                        TargetCrossDPAI = GUI.Toggle(new Rect(widthOffset, 64, 88, 20), TargetCrossDPAI, "Cross: DPAI");
                        //if (TargetCrossDPAI)
                        //    TargetCrossStock = TargetCrossOLDD = false;

                        TargetCrossOLDD = GUI.Toggle(new Rect(widthOffset, 84, 88, 20), TargetCrossOLDD, "Cross: KURS");
                        //if (TargetCrossOLDD)
                        //    TargetCrossStock = TargetCrossDPAI = false;

                        _cameraData = GUI.Toggle(new Rect(widthOffset, 144, 88, 20), _cameraData, "Flight data");
                        _rotatorState = GUI.Toggle(new Rect(widthOffset, 164, 88, 20), _rotatorState, "Rotator");
                    }
                    else
                        GUI.Label(new Rect(widthOffset, 174, 88, 60), " Select\n docking\n port", Styles.RedLabel13B);
                }
                Noise = GUI.Toggle(new Rect(widthOffset, 253, 88, 20), Noise, "Noise");
            }
            base.ExtendedDrawWindowL1();
        }

        protected override void ExtendedDrawWindowL2()
        {
            // This draws the standard cross
            if (TargetCrossStock)
                GUI.DrawTexture(TexturePosition, AssetLoader.texDockingCam);
            if (Noise)
            {
                GUI.DrawTexture(TexturePosition, _textureWhiteNoise[WindowSizeCoef - 2][_idTextureNoise]);  //add whitenoise
            }
            base.ExtendedDrawWindowL2();
        }

        protected override void ExtendedDrawWindowL3()
        {
            //targetted lamp & seconds Block
            if (_target.IsMoveToTarget)
            {
                GUI.DrawTexture(new Rect(TexturePosition.xMin + 20, TexturePosition.yMax - 20, 20, 20), AssetLoader.texLampOn);
                GUI.Label(new Rect(TexturePosition.xMin + 40, TexturePosition.yMax - 20, 140, 20), $"Time to dock:{_target.SecondsToDock:f0}s");
            }
            else
                GUI.DrawTexture(new Rect(TexturePosition.xMin + 20, TexturePosition.yMax - 20, 20, 20), AssetLoader.texLampOff);

            GetWindowLabel();
            GetFlightData();
            GetCross();

            if (_rotatorState && ThisPart.vessel.Equals(FlightGlobals.ActiveVessel) && TargetHelper.IsTargetSelect)
            {
                var size1 = TexturePosition.width / 7;
                var x1 = TexturePosition.xMin + TexturePosition.width / 2 - size1 / 2;
                var rect1 = new Rect(x1, TexturePosition.yMax - size1, size1, size1);
                GUI.DrawTexture(rect1, AssetLoader.texTargetRot);
                Matrix4x4 matrixBackup1 = GUI.matrix;
                GUIUtility.RotateAroundPivot(_target.AngleZ, rect1.center);
                GUI.DrawTexture(new Rect(x1, TexturePosition.yMax - size1, size1, size1), AssetLoader.texSelfRot);
                GUI.matrix = matrixBackup1;

                var size2 = TexturePosition.width / 8;
                var x2 = TexturePosition.xMin + TexturePosition.width / 2 - size2 / 2 - size1;
                var rect2 = new Rect(x2, TexturePosition.yMax - size2, size2, size2);
                GUI.DrawTexture(rect2, AssetLoader.texTargetRot);
                Matrix4x4 matrixBackup2 = GUI.matrix;
                GUIUtility.RotateAroundPivot(_target.AngleX, rect2.center);
                GUI.DrawTexture(new Rect(x2, TexturePosition.yMax - size2, size2, size2), AssetLoader.texSelfRot);
                GUI.matrix = matrixBackup2;

                var size3 = TexturePosition.width / 8;
                var x3 = TexturePosition.xMin + TexturePosition.width / 2 - size3 / 2 + size1;
                var rect3 = new Rect(x3, TexturePosition.yMax - size3, size3, size3);
                GUI.DrawTexture(rect3, AssetLoader.texTargetRot);
                Matrix4x4 matrixBackup3 = GUI.matrix;
                GUIUtility.RotateAroundPivot(_target.AngleY, rect3.center);
                GUI.DrawTexture(new Rect(x3, TexturePosition.yMax - size3, size3, size3), AssetLoader.texSelfRot);
                GUI.matrix = matrixBackup3;
            }

            base.ExtendedDrawWindowL3();
        }

        private void GetWindowLabel()
        {
            if (ThisPart.vessel.Equals(FlightGlobals.ActiveVessel))
            {
                if (TargetHelper.IsTargetSelect) // && thisPart.vessel.Equals(FlightGlobals.ActiveVessel))
                {
                    _lastVesselName = TargetHelper.Target.GetName();
                    _windowLabelSuffix = " to " + _lastVesselName;
                    WindowLabel = SubWindowLabel + " " + _id + _windowLabelSuffix;
                }
                else
                {
                    //if (ThisPart.vessel.Equals(FlightGlobals.ActiveVessel))
                    {
                        WindowLabel = SubWindowLabel + " " + _id;
                        _lastVesselName = "";
                        _windowLabelSuffix = _lastVesselName;
                    }
                }
            }
            else
            {
                WindowLabel = SubWindowLabel + " " + _id + _windowLabelSuffix;
            }
        }

        private void GetCross()
        {
            if (TargetCrossDPAI && _target.IsDockPort)
            {
                var textV = _target.LookForward ? _textureVLine : _textureVLineBack;
                var textH = _target.LookForward ? _textureHLine : _textureHLineBack;
                var tx = _target.TargetMoveHelpX;
                var ty = _target.TargetMoveHelpY;
                if (!_target.LookForward)
                {
                    tx = 1 - tx;
                    ty = 1 - ty;
                }
                GUI.DrawTexture(new Rect(TexturePosition.xMin + Math.Abs(tx * TexturePosition.width) % TexturePosition.width,
                    TexturePosition.yMin, 1, TexturePosition.height), textV);
                GUI.DrawTexture(new Rect(TexturePosition.xMin, TexturePosition.yMin
                    + Math.Abs(ty * TexturePosition.height) % TexturePosition.height, TexturePosition.width, 1), textH);
            }

            if (TargetCrossOLDD && _target.IsDockPort)
            {
                var tx = TexturePosition.width / 2;
                var ty = TexturePosition.height / 2;
                if (Mathf.Abs(_target.AngleX) > 20)
                    tx += (_target.AngleX > 0 ? -1 : 1) * (TexturePosition.width / 2 - 1);
                else
                    tx += TexturePosition.width / 40 * -_target.AngleX;
                if (Mathf.Abs(_target.AngleY) > 20)
                    ty += (_target.AngleY > 0 ? -1 : 1) * (TexturePosition.height / 2 - 1);
                else
                    ty += TexturePosition.height / 40 * -_target.AngleY;
                GUI.DrawTexture(new Rect(TexturePosition.xMin + tx, TexturePosition.yMin, 1, TexturePosition.height), _textureVLineOLDD);
                GUI.DrawTexture(new Rect(TexturePosition.xMin, TexturePosition.yMin + ty, TexturePosition.width, 1), _textureHLineOLDD);
            }
        }

        private void GetFlightData()
        {
            if (!_cameraData) return;
            if (TargetHelper.IsTargetSelect && ThisPart.vessel.Equals(FlightGlobals.ActiveVessel))
            {
                // DockPort DATA block
                float i = 0;
                _target.Update();

                if (!_target.IsDockPort)
                {
                    GUI.Label(new Rect(TexturePosition.xMin + 2, 34, 100, 40), "Target is not\n a DockPort");
                    if (_target.Destination < 200f)
                        GUI.Label(new Rect(TexturePosition.xMin + 2, 68, 100, 40), "DockPort is\n available", Styles.GreenLabel11);
                }
                else
                    GUI.Label(new Rect(TexturePosition.xMin + 2, 34, 100, 40), "DockPort captured", Styles.GreenLabel13);

                // Flight DATA
                var dataFormat = Math.Abs(_target.Destination) < 1000 ? "{0:f2}" : "{0:f0}";
                var stringOffset = 16;
                GUI.Label(new Rect(TexturePosition.xMax - 70, 34 + i++ * stringOffset, 70, 20), string.Format("Dist:" + dataFormat, _target.Destination), Styles.Label13B);
                i += .2f;

                GUI.Label(new Rect(TexturePosition.xMax - 70, 34 + i++ * stringOffset, 70, 20), string.Format("dX:" + dataFormat, _target.DX));
                GUI.Label(new Rect(TexturePosition.xMax - 70, 34 + i++ * stringOffset, 70, 20), string.Format("dY:" + dataFormat, _target.DY));
                GUI.Label(new Rect(TexturePosition.xMax - 70, 34 + i++ * stringOffset, 70, 20), string.Format("dZ:" + dataFormat, _target.DZ));
                i += .2f;

                if (Math.Abs(_target.SpeedX) > _maxSpeed && Math.Abs(_target.Destination) < 200)
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vX:{_target.SpeedX:f2}", Styles.RedLabel13);
                else
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vX:{_target.SpeedX:f2}", Styles.Label13);

                if (Math.Abs(_target.SpeedY) > _maxSpeed && Math.Abs(_target.Destination) < 200)
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vY:{_target.SpeedY:f2}", Styles.RedLabel13);
                else
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vY:{_target.SpeedY:f2}", Styles.Label13);

                if (Math.Abs(_target.SpeedZ) > _maxSpeed && Math.Abs(_target.Destination) < 200)
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vZ:{_target.SpeedZ:f2}", Styles.RedLabel13);
                else
                    GUI.Label(new Rect(TexturePosition.xMax - 70, 38 + i++ * stringOffset, 70, 20), $"vZ:{_target.SpeedZ:f2}", Styles.Label13);
                i += .2f;

                GUI.Label(new Rect(TexturePosition.xMax - 70, 40 + i++ * stringOffset, 70, 20), $"Yaw:{_target.AngleX:f0}°");
                GUI.Label(new Rect(TexturePosition.xMax - 70, 40 + i++ * stringOffset, 70, 20), $"Pitch:{_target.AngleY:f0}°");
                GUI.Label(new Rect(TexturePosition.xMax - 70, 40 + i * stringOffset, 70, 20), $"Roll:{_target.AngleZ:f0}°");
            }
        }

        public override void Activate()
        {
            if (IsActive) return;
            SetFreeId();

            WindowPosition.x = WindowPosition.width * (_id - 1);
            WindowPosition.y = 400;
            InitWindow();
            base.Activate();
        }

        public override void Deactivate()
        {
            if (!IsActive) return;
            if (_usedId != null)
                _usedId.Remove(_id);
            base.Deactivate();
        }

        private void SetFreeId()
        {
            for (int i = 1; i < int.MaxValue; i++)
            {
                if (!_usedId.Contains(i))
                {
                    _id = i;
                    _usedId.Add(i);
                    //return; //ddd
                    break; // lll
                }
            }
        }

        public void UpdateNoise() //whitenoise
        {
            _idTextureNoise++;
            if (_idTextureNoise >= 4)
                _idTextureNoise = 0;
        }

        public override void Update()
        {
            UpdateWhiteNoise();

            AllCamerasGameObject.Last().transform.position = _moduleDockingNodeGameObject.transform.position; // near&&far
            //AllCamerasGameObject.Last().transform.localPosition = _moduleDockingNodeGameObject.transform.localPosition;
            //AllCamerasGameObject.Last().transform.localRotation = _moduleDockingNodeGameObject.transform.localRotation;
            AllCamerasGameObject.Last().transform.rotation = _moduleDockingNodeGameObject.transform.rotation;

            var sCam = AllCamerasGameObject.Last();
            sCam.transform.parent = AllCamerasGameObject.Last().transform;
            //sCam.transform.localPosition = _dcm.cameraPosition;
            //sCam.transform.localRotation = Quaternion.LookRotation(_dcm.cameraForward, _dcm.cameraUp);
            

            AllCamerasGameObject[0].transform.rotation = AllCamerasGameObject.Last().transform.rotation; // skybox galaxy
            AllCamerasGameObject[1].transform.rotation = AllCamerasGameObject.Last().transform.rotation; // nature object
            AllCamerasGameObject[2].transform.rotation = AllCamerasGameObject.Last().transform.rotation; // middle 
            AllCamerasGameObject[2].transform.position = AllCamerasGameObject.Last().transform.position;
            AllCameras.ForEach(cam => cam.fieldOfView = CurrentZoom);
        }
    }
}
