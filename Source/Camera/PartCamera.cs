using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OLDD_camera.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Camera
{
    class PartCamera : BaseCamera
    {
        private static HashSet<int> _usedId = new HashSet<int>();
        private const int ButtonSize = 25;

        private LineRenderer _scanningRay;
        private LineRenderer _visibilityRay;

        private readonly GameObject _rotatorZ;
        private readonly GameObject _rotatorY;
        private readonly GameObject _zoommer;
        private readonly GameObject _camObject;
        private readonly float _stepper;
        private readonly string _bulletName;
        public int Hits = 4;

        private float _rotateZbuffer;
        private float _rotateYbuffer;
        private float _zoomBuffer;
        private float _lastZoom;
        private float _simplifiedRotateZBuffer;
        private float _rotateStep;
        private int _id;
        private readonly int _allowedScanDistance;
        private readonly int _resourceUsage;
        private readonly string _resourceName;
        private bool _isRayEnabled;
        
        enum Alignment { up, right, down, left};
        private Alignment _alignment = Alignment.up;
        private bool _isUpsideDown;
        private bool _isScienceActivate;
        private bool _isVisibilityRay;
        private string _cameraMode;

        public bool IsWaitForRay;
        public bool IsToZero;

        internal bool IsOnboard;
        internal bool IsOnboardEnabled;
        internal bool IsLookAtMe;
        internal bool IsLookAtMeEnabled;
        internal bool IsLookAtMeAutoZoom;
        internal bool IsFollowMe;
        internal bool IsFollowEnabled;
        internal bool IsTargetCam;
        internal bool IsTargetCamEnabled;
        internal bool IsOutOfRange;

        internal float IsFollowMeOffsetX;
        internal float IsFollowMeOffsetY;
        internal float IsFollowMeOffsetZ;
        internal float TargetOffset;

        internal Transform CurrentCamTarget;
        internal Transform CurrentCam;

        internal Quaternion InitialCamRotation;
        internal Vector3 InitialCamPosition;
        internal Quaternion InitialCamLocalRotation;
        internal Vector3 InitialCamLocalPosition;

        internal Quaternion CurrentCamRotation;
        internal Vector3 CurrentCamPosition;
        internal Quaternion CurrentCamLocalRotation;
        internal Vector3 CurrentCamLocalPosition;

        public float RealZoom
        {
            get { return ZoomMultiplier ? CurrentZoom / MinZoomMultiplier : CurrentZoom; }
            set { CurrentZoom = value; }
        }

        public PartCamera(Part thisPart, string resourceScanning, string bulletName, int hits,
                string rotatorZ, string rotatorY, string zoommer, float stepper, string cameraName, int allowedScanDistance,
                int windowSize, bool isOnboard, bool isLookAtMe, bool isLookAtMeAutoZoom, bool isFollowMe, bool isTargetCam,
                float isFollowMeOffsetX, float isFollowMeOffsetY, float isFollowMeOffsetZ, float targetOffset,
                string windowLabel = "Camera") : base(thisPart, windowSize, windowLabel)
        {
            var splresource = resourceScanning.Split('.').ToList();
            _resourceName = splresource[0];
            _resourceUsage = int.Parse(splresource[1]);
            _bulletName = bulletName;
            _rotatorZ = PartGameObject.gameObject.GetChild(rotatorZ);
            _rotatorY = PartGameObject.gameObject.GetChild(rotatorY);
            _zoommer = PartGameObject.gameObject.GetChild(zoommer);
            _camObject = PartGameObject.gameObject.GetChild(cameraName);
            _stepper = stepper;
            _allowedScanDistance = allowedScanDistance;
            _lastZoom = CurrentZoom;

            IsOnboard = isOnboard;
            IsLookAtMe = isLookAtMe;
            IsFollowMe = isFollowMe;
            IsLookAtMeAutoZoom = isLookAtMeAutoZoom;
            IsTargetCam = isTargetCam;
            IsFollowMeOffsetX = isFollowMeOffsetX;
            IsFollowMeOffsetY = isFollowMeOffsetY;
            IsFollowMeOffsetZ = isFollowMeOffsetZ;
            TargetOffset = targetOffset;

            GameEvents.onGameSceneLoadRequested.Add(LevelWasLoaded);

            GetCurrentBullets(bulletName, hits);
        }

        private void LevelWasLoaded(GameScenes data)
        {
            _usedId = new HashSet<int>();
        }

        ~PartCamera()
        {
            GameEvents.onGameSceneLoadRequested.Remove(LevelWasLoaded);
        }

        private void GetCurrentBullets(string bulletName, int hits)
        {
            if (hits == -1)
            {
                Hits = 0;
                while (true)
                {
                    var hit = PartGameObject.GetChild($"{bulletName}{Hits + 1:000}");
                    if (hit == null)
                        break;
                    Hits++;
                }
            }
            else
            {
                Hits = hits;
                var i = Hits + 1;
                while (true)
                {
                    var hit = PartGameObject.GetChild($"{bulletName}{i:000}");
                    if (hit == null)
                        break;
                    Object.Destroy(hit);
                    i++;
                }
            }
        }

        public override void Activate()
        {
            base.Activate();
            SetFreeId();
            WindowPosition.x = WindowPosition.width * (_id - 1);
            WindowPosition.y = 64;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            if (_usedId != null) //lll
                _usedId.Remove(_id);
        }

        private void SetFreeId()
        {
            for (int i = 1; i < 8; i++)
            {
                if (_usedId.Contains(i)) continue;
                _id = i;
                WindowLabel = SubWindowLabel + " " + _id;
                _usedId.Add(i);
                return;
            }
        }

        #region DRAW LAYERS
        protected override void ExtendedDrawWindowL1()
        {
            if (IsOrbital) return;

            SetRotationStep();
            var widthOffset = WindowPosition.width - 90;

            if (IsAuxiliaryWindowOpen)
            {
                if (IsOnboard)
                    DrawButtonsBlock(widthOffset);
                else
                    DrawModeDataBlock(widthOffset);

                DrawModeSelector(widthOffset);
                ZoomMultiplier = GUI.Toggle(new Rect(widthOffset, 112, 80, 20), ZoomMultiplier, " x 24");

                if (IsOnboard)
                {
                    GUI.Label(new Rect(widthOffset, 148, 80, 20), $"rotateZ: {_simplifiedRotateZBuffer:F0}°");
                    GUI.Label(new Rect(widthOffset, 164, 80, 20), $"rotateY: {_rotateYbuffer:F0}°");
                }

                if (GUI.Button(new Rect(widthOffset, 186, 80, 25), "PHOTO"))
                    RenderTexture.SavePng(ThisPart.vessel.vesselName);
            }

            if ((IsOnboard || IsLookAtMe) && (FlightGlobals.ActiveVessel == ThisPart.vessel))
                _isVisibilityRay = GUI.Toggle(new Rect(widthOffset - 2, 215, 80, 20), _isVisibilityRay, "Target Ray");

            GUI.Label(new Rect(widthOffset, 312, 80, 20), $"Bullets: {Hits:F0}", Styles.Label13B);
            base.ExtendedDrawWindowL1();
        }

        protected override void ExtendedDrawWindowL2()
        {
            if (IsTargetCam && IsOutOfRange)
            {
                if (Event.current.type.Equals(EventType.Repaint))
                {
                    Graphics.DrawTexture(TexturePosition, TextureNoSignal[TextureNoSignalId]);
                }
                GUI.Label(new Rect(TexturePosition.xMin + 32 * WindowSizeCoef, TexturePosition.yMin + 32 * WindowSizeCoef, 160, 160),
                "TARGET \n IS \n OUT OF RANGE", Styles.RedLabel25B);
            }
            base.ExtendedDrawWindowL2();
        }

        protected override void ExtendedDrawWindowL3()
        {
            var str = "Mode: " + _cameraMode + " ( x " + CalculatedZoom + " )";
            GUI.Label(new Rect(TexturePosition.xMin + 44 * WindowSizeCoef, TexturePosition.yMax - 12, 160, 20),
                str, Styles.GreenLabel11);
            base.ExtendedDrawWindowL3();
        }

        private void DrawButtonsBlock(float widthOffset)
        {
            if (GUI.Button(new Rect(widthOffset, 36, ButtonSize, ButtonSize), "↻"))
            {
                //_camObject.transform.Rotate(new Vector3(0, 0, 180f));
                _camObject.transform.Rotate(new Vector3(0, 0, 90f));
                _isUpsideDown = !_isUpsideDown;
                _alignment++;
                if ((int)_alignment > 3)
                    _alignment = Alignment.up;
            }
            if (GUI.RepeatButton(new Rect(widthOffset + ButtonSize, 36, ButtonSize, ButtonSize), "↑"))
            {
                switch (_alignment)
                {
                    case Alignment.up: RotateY += _rotateStep; break;
                    case Alignment.right: RotateZ -= _rotateStep; break;
                    case Alignment.down: RotateY -= _rotateStep; break;
                    case Alignment.left: RotateZ += _rotateStep;break;
                }
            }
            if (GUI.Button(new Rect(widthOffset + ButtonSize * 2, 36, ButtonSize, ButtonSize), "⦿"))
            {
                if (ThisPart.vessel.Equals(FlightGlobals.ActiveVessel))
                {
                    if (!TargetHelper.IsTargetSelect)
                        ScreenMessages.PostScreenMessage("NO TARGET FOR SCANNING", 3f, ScreenMessageStyle.UPPER_CENTER);
                    else
                    {
                        if (Hits <= 0)
                            ScreenMessages.PostScreenMessage("BULLETS DEPLETED", 3f, ScreenMessageStyle.UPPER_CENTER);
                        else
                        {
                            var id = PartResourceLibrary.Instance.GetDefinition(_resourceName).id;
                            double amount;
                            double maxAmount;
                            ThisPart.GetConnectedResourceTotals(id, out amount, out maxAmount);
                            if (amount > _resourceUsage)
                            {
                                ThisPart.RequestResource(id, (double)_resourceUsage);
                                var hit = PartGameObject.GetChild($"{_bulletName}{Hits:000}");
                                Object.Destroy(hit);
                                Hits--;
                                _isRayEnabled = true;
                                IsWaitForRay = true;
                                _isScienceActivate = false;
                            }
                            else
                                ScreenMessages.PostScreenMessage("NOT ENOUGH ELECTRICITY FOR SCAN", 3f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        //if (HitCounter() && UseResourceForScanning())
                        //{
                        //    _isRayEnabled = true;
                        //    IsWaitForRay = true;
                        //    _isScienceActivate = false;
                        //}
                    }
                }
                else
                    ScreenMessages.PostScreenMessage("Camera not on active vessel", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
            if (GUI.RepeatButton(new Rect(widthOffset, 36 + ButtonSize, ButtonSize, ButtonSize), "←"))
            {
                switch (_alignment)
                {
                    case Alignment.up: RotateZ -= _rotateStep; break;
                    case Alignment.right: RotateY -= _rotateStep; break;
                    case Alignment.down: RotateZ += _rotateStep; break;
                    case Alignment.left: RotateY += _rotateStep; break;
                }
            }
            if (GUI.Button(new Rect(widthOffset + ButtonSize, 36 + ButtonSize, ButtonSize, ButtonSize), "o"))
            {
                IsToZero = true;
            }
            if (GUI.RepeatButton(new Rect(widthOffset + ButtonSize * 2, 36 + ButtonSize, ButtonSize, ButtonSize), "→"))
            {
#if false
                if (!_isUpsideDown)
                    RotateZ += _rotateStep;
                else
                    RotateZ -= _rotateStep;

#endif
                switch (_alignment)
                {
                    case Alignment.up: RotateZ += _rotateStep; break;
                    case Alignment.right: RotateY += _rotateStep; break;
                    case Alignment.down: RotateZ -= _rotateStep; break;
                    case Alignment.left: RotateY -= _rotateStep; break;
                }
            }
            if (GUI.Button(new Rect(widthOffset, 36 + ButtonSize * 2, ButtonSize, ButtonSize), "-"))
            {
                CurrentZoom += 0.5f;
                if (CurrentZoom > MaxZoom)
                    CurrentZoom = MaxZoom;
            }
            if (GUI.RepeatButton(new Rect(widthOffset + ButtonSize, 36 + ButtonSize * 2, ButtonSize, ButtonSize), "↓"))
            {
#if false
                if (_rotateYbuffer > 0)
                {

                    if (!_isUpsideDown)
                        RotateY -= _rotateStep;
                    else
                        RotateY += _rotateStep;
                }
#endif
                switch (_alignment)
                {
                    case Alignment.up: RotateY -= _rotateStep; break;
                    case Alignment.right: RotateZ += _rotateStep; break;
                    case Alignment.down: RotateY += _rotateStep; break;
                    case Alignment.left: RotateZ -= _rotateStep; break;
                }
            }
            if (GUI.Button(new Rect(widthOffset + ButtonSize * 2, 36 + ButtonSize * 2, ButtonSize, ButtonSize), "+"))
            {
                CurrentZoom -= 0.5f;
                if (CurrentZoom < MinZoom)
                    CurrentZoom = MinZoom;
            }
        }

        private void DrawModeDataBlock(float widthOffset)
        {
            if (IsLookAtMe)
            {
                GUI.Box(new Rect(widthOffset - 2, 36, 86, 76), "Look At Me");
                GUI.Label(new Rect(widthOffset + 2, 55, 84, 44), "Focus on:\n" + FlightGlobals.ActiveVessel.vesselName, Styles.GreenLabel13);
                IsLookAtMeAutoZoom = GUI.Toggle(new Rect(widthOffset, 90, 86, 20), IsLookAtMeAutoZoom, "AutoZoom");
            }

            if (IsFollowMe)
            {
                GUI.Box(new Rect(widthOffset - 2, 36, 86, 76), "Offset X,Y,Z");
                IsFollowMeOffsetX = GUI.HorizontalSlider(new Rect(widthOffset + 1, 58, 80, 10), IsFollowMeOffsetX, -80, 80);
                IsFollowMeOffsetY = GUI.HorizontalSlider(new Rect(widthOffset + 1, 74, 80, 10), IsFollowMeOffsetY, -80, 80);
                IsFollowMeOffsetZ = GUI.HorizontalSlider(new Rect(widthOffset + 1, 90, 80, 10), IsFollowMeOffsetZ, -80, 80);
            }

            if (IsTargetCam)
            {
                GUI.Box(new Rect(widthOffset - 2, 36, 86, 76), "Target Data");
                var target = TargetHelper.Target;// as Vessel;
                if (target != null)
                {
                    GUI.Label(new Rect(widthOffset + 2, 55, 84, 44), "Focus on:\n" + target.GetVessel().GetName(), Styles.GreenLabel13);
                    GUI.Label(new Rect(widthOffset + 4, 152, 80, 22), "Target Offset", Styles.Label13B);
                    TargetOffset = GUI.HorizontalSlider(new Rect(widthOffset + 2, 170, 80, 25), TargetOffset, 8, 128);
                }
                else
                    GUI.Label(new Rect(widthOffset + 4, 60, 84, 44), "NO TARGET\nSELECTED", Styles.RedLabel13B);
            }
        }

        private void DrawModeSelector(float widthOffset)
        {
            GUI.Box(new Rect(widthOffset - 2, 256, 86, 56), "");

            if (ThisPart.vessel != FlightGlobals.ActiveVessel)
            {
                if (IsLookAtMe = GUI.Toggle(new Rect(widthOffset - 2, 256, 84, 20), IsLookAtMe, "Look at Me"))
                {
                    IsOnboard = IsFollowMe = IsTargetCam = false;
                    var lastCameraMode = _cameraMode;
                    _cameraMode = "Look at Me";
                    IsFollowMeOffsetX = IsFollowMeOffsetY = IsFollowMeOffsetZ = 0;
                    if (!IsLookAtMeEnabled)
                        CameraPositioning(lastCameraMode);
                }
            }

            if (IsFollowMe = GUI.Toggle(new Rect(widthOffset - 2, 274, 84, 20), IsFollowMe, "Follow Me"))
            {
                IsOnboard = IsLookAtMe = IsTargetCam = false;
                var lastCameraMode = _cameraMode;
                _cameraMode = "Follow Me";
                if (!IsFollowEnabled)
                    CameraPositioning(lastCameraMode);
            }

            if (ThisPart.vessel == FlightGlobals.ActiveVessel)
            {
                if (IsTargetCam = GUI.Toggle(new Rect(widthOffset - 2, 292, 84, 20), IsTargetCam, "Target Cam"))
                {
                    IsOnboard = IsLookAtMe = IsFollowMe = false;
                    var lastCameraMode = _cameraMode;
                    _cameraMode = "Target Cam";
                    if (IsTargetCamEnabled)
                    {
                        var target = TargetHelper.Target;// as Vessel;
                        if (target != null)
                        {
                            var target1 = target.GetVessel();
                            var range = Mathf.Round(Vector3.Distance(target1.transform.position, FlightGlobals.ActiveVessel.transform.position));
                            if (range > FlightGlobals.ActiveVessel.vesselRanges.landed.load)
                                IsOutOfRange = true;
                            else
                                IsOutOfRange = false;
                        }
                    }
                    else
                    {
                        CameraPositioning(lastCameraMode);
                        CurrentZoom = 32;
                        ZoomMultiplier = false;
                    }
                }
            }

            if (!IsLookAtMe && !IsFollowMe && !IsTargetCam)
            {
                IsOnboard = true;
                _cameraMode = "Onboard";
                IsFollowMeOffsetX = IsFollowMeOffsetY = IsFollowMeOffsetZ = 0;
                if (!IsOnboardEnabled)
                {
                    _camObject.transform.rotation = CurrentCamRotation;
                    _camObject.transform.position = CurrentCamPosition;
                    _camObject.transform.localRotation = CurrentCamLocalRotation;
                    _camObject.transform.localPosition = CurrentCamLocalPosition;
                    CurrentZoom = 32;
                    ZoomMultiplier = false;
                    IsOnboardEnabled = true;
                }
            }
        }
#endregion DRAW LAYERS

        private void CameraPositioning(string lastCameraMode)
        {
            if (lastCameraMode == "Onboard")
            {
                CurrentCamRotation = _camObject.transform.rotation;
                CurrentCamPosition = _camObject.transform.position;
                CurrentCamLocalRotation = _camObject.transform.localRotation;
                CurrentCamLocalPosition = _camObject.transform.localPosition;
            }
            _camObject.transform.rotation = InitialCamRotation;
            _camObject.transform.position = InitialCamPosition;
            _camObject.transform.localRotation = InitialCamLocalRotation;
            _camObject.transform.localPosition = InitialCamLocalPosition;
        }

        private void SetRotationStep()
        {
            _simplifiedRotateZBuffer = _rotateZbuffer;
            if (Mathf.Abs(_simplifiedRotateZBuffer) > 360)
                _simplifiedRotateZBuffer = _simplifiedRotateZBuffer % 360;

            _rotateStep = 1.0f;
            if (CalculatedZoom >= 512) { _rotateStep = 0.02f; return; }
            if (CalculatedZoom >= 256) {_rotateStep = 0.04f; return;}
            if (CalculatedZoom >= 128) {_rotateStep = 0.06f; return;}
            if (CalculatedZoom >= 64) { _rotateStep = 0.07f; return; }
            if (CalculatedZoom >= 32) {_rotateStep = 0.08f; return;}
            if (CalculatedZoom >= 16) {_rotateStep = 0.32f; return;}
            if (CalculatedZoom >= 8) _rotateStep = 0.64f;
        }

        public override void Update()
        {
            if (IsOrbital || IsOutOfRange)
                UpdateWhiteNoise();
            DrawScanningRay();
            DrawVisibilityRay();

            AllCamerasGameObject.Last().transform.position = _camObject.gameObject.transform.position;
            AllCamerasGameObject.Last().transform.rotation = _camObject.gameObject.transform.rotation;

            var step = -(_lastZoom - CurrentZoom) / _stepper;
            _lastZoom = CurrentZoom;
            _zoommer.transform.Translate(new Vector3(step, 0, 0));
            _rotatorZ.transform.Rotate(new Vector3(0, 0, 1), RotateZ);
            _rotatorY.transform.Rotate(new Vector3(0, 1, 0), RotateY);
            _rotateZbuffer += RotateZ;
            _rotateYbuffer += RotateY;
            _zoomBuffer += step;

            AllCamerasGameObject[0].transform.rotation = AllCamerasGameObject.Last().transform.rotation;
            AllCamerasGameObject[1].transform.rotation = AllCamerasGameObject.Last().transform.rotation;
            AllCamerasGameObject[2].transform.rotation = AllCamerasGameObject.Last().transform.rotation;
            AllCamerasGameObject[2].transform.position = AllCamerasGameObject.Last().transform.position;
            AllCameras.ForEach(cam => cam.fieldOfView = RealZoom); //currentZoom); 
            //AllCameras.ForEach(delegate (UnityEngine.Camera cam) //lll
            //{
            //    cam.fieldOfView = RealZoom;
            //});
            RotateZ = 0;
            RotateY = 0;
        }

        private void DrawScanningRay()
        {
            Object.Destroy(_scanningRay);
            if (!_isRayEnabled || !TargetHelper.IsTargetSelect) return;
            Vector3 endPoint;
            if (!IsInsight(out endPoint)) return;
            _scanningRay = new GameObject("scanningRay").AddComponent<LineRenderer>();
            _scanningRay.material = new Material(Shader.Find("Particles/Additive"));
            // The following commented lines were obsolete, replaced with the methods immediately following.
            // The old lines were left as historical documentation

            //_scanningRay.SetColors(Color.red, Color.red);
            _scanningRay.startColor = Color.red;
            _scanningRay.endColor = Color.red;

            //_scanningRay.SetVertexCount(2);
            //_scanningRay.numPositions = 2;
            _scanningRay.positionCount = 2;

            //_scanningRay.SetWidth(0.02f, 0.02f);
            _scanningRay.startWidth = 0.02f;
            _scanningRay.endWidth = 0.02f;

            _scanningRay.useWorldSpace = true;
            _scanningRay.SetPosition(0, ThisPart.transform.position);
            _scanningRay.SetPosition(1, endPoint);
        }

        private void DrawVisibilityRay()
        {
            Object.Destroy(_visibilityRay);
            if (!_isVisibilityRay) return;
            if (!TargetHelper.IsTargetSelect || !IsTargetVisiable()) return;
            _visibilityRay = new GameObject("visibilityRay").AddComponent<LineRenderer>();
            var color = Color.white;
            _visibilityRay.material = new Material(Shader.Find("Particles/Additive"));

            // The following commented lines were obsolete, replaced with the methods immediately following.
            // The old lines were left as historical documentation

            //_visibilityRay.SetColors(color, color);
            _visibilityRay.startColor = color;
            _visibilityRay.endColor = color;

            //_visibilityRay.SetVertexCount(2);
            //_visibilityRay.numPositions = 2;
            _visibilityRay.positionCount = 2;

            //_visibilityRay.SetWidth(0.02f, 0.02f);
            _visibilityRay.startWidth = 0.02f;
            _visibilityRay.endWidth = 0.02f;

            _visibilityRay.useWorldSpace = true;
            _visibilityRay.SetPosition(0, _camObject.transform.position);
            _visibilityRay.SetPosition(1, TargetHelper.Target.GetTransform().position);
        }

        private bool IsInsight(out Vector3 endPoint)
        {
            var camera = AllCameras.Last();
            endPoint = TargetHelper.Target.GetTransform().position;
            var point = camera.WorldToViewportPoint(endPoint); //get current targeted vessel 
            var x = point.x; // (0;1)
            var y = point.y; // (0;1)
            var z = point.z;
            return z > 0 && 0 <= x && x <= 1 && 0 <= y && y <= 1;
        }

        private bool IsTargetVisiable()
        {
            foreach (var body in FlightGlobals.Bodies)
            {
                var r = body.Radius;
                var self = ThisPart.vessel.GetWorldPos3D();
                var target = TargetHelper.Target.GetTransform().position;
                var shift = target - self;
                var coef = r / Vector3.Distance(self, target);
                coef *= .5f;
                shift *= coef;
                var point = target - shift;
                var distanceFromPoint = Vector3.Distance(body.position, point);
                if (distanceFromPoint < r)
                    return false;
            }
            return true;
        }

        public IEnumerator WaitForRay()
        {
            yield return new WaitForSeconds(1);
            _isRayEnabled = false;
            var target = new TargetHelper(ThisPart);
            target.Update();
            Vector3 endPoint;
            if (target.Destination <= _allowedScanDistance && IsInsight(out endPoint) && IsTargetVisiable())
            {
                ScreenMessages.PostScreenMessage(FlightGlobals.activeTarget.vessel.vesselName + " HAS BEEN SCANNED", 3f, ScreenMessageStyle.UPPER_CENTER);
                if (!_isScienceActivate)
                {
                    var spyScience = ThisPart.GetComponent<ModuleSpyExperiment>();
                    if (spyScience != null)
                        spyScience.DeployExperiment();
                    _isScienceActivate = true;
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage("NO DATA, TARGET " + FlightGlobals.activeTarget.vessel.vesselName + " IS OUT OF RANGE  OR VISIBILITY", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public IEnumerator ReturnCamToZero()
        {
            var coef = 20;
            var stepRotZ = -_simplifiedRotateZBuffer / coef;
            var stepRotY = -_rotateYbuffer / coef;
            var stepZoom = -_zoomBuffer / coef;
            for (int i = 0; i < coef; i++)
            {
                _zoommer.transform.Translate(new Vector3(stepZoom, 0, 0));
                _rotatorZ.transform.Rotate(new Vector3(0, 0, 1), stepRotZ);
                _rotatorY.transform.Rotate(new Vector3(0, 1, 0), stepRotY);
                yield return new WaitForSeconds(.05f);
            }
            _rotateZbuffer = _rotateYbuffer = _zoomBuffer = 0;
            CurrentZoom = MaxZoom;
            _lastZoom = CurrentZoom;
        }
    }
}
