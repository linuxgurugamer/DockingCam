using KSP.Localization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OLDD_camera.Camera;
using OLDD_camera.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Modules
{
  
    /// <summary>
    /// Module adds an external camera and gives control over it
    /// </summary>
    class PartCameraModule : PartModule, ICamPart
    {
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Camera", isPersistant = true),
        UI_Toggle(controlEnabled = true, enabledText = "#LOC_DockingCam_90", disabledText = "#LOC_DockingCam_91", scene = UI_Scene.Flight)]
        public bool IsEnabled;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Camera powered ")]
        private string _isPowered;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Bullets ")]
        private string _aboutHits = "#LOC_DockingCam_92";

        [KSPField(isPersistant = true)]
        private int _currentHits = -1;

        [KSPField]
        public string restrictShaderTo = "";  // a comma deliminated list of shader names, the part after the slash

        [KSPField]
        public int windowSize = 300;

        [KSPField]
        public int allowedScanDistance = 1000;

        [KSPField]
        public string resourceScanning = "#LOC_DockingCam_93";
        
        [KSPField(isPersistant = true)]
        public double electricchargeCost = 0.02d;

        private readonly string _cameraName = Localizer.Format("#LOC_DockingCam_94");
        private readonly string _rotatorZ = Localizer.Format("#LOC_DockingCam_95");
        private readonly string _rotatorY = Localizer.Format("#LOC_DockingCam_96");
        private readonly string _zoommer = Localizer.Format("#LOC_DockingCam_97");
        private readonly string _cap = Localizer.Format("#LOC_DockingCam_98");
        private readonly string _bulletName = Localizer.Format("#LOC_DockingCam_99");
        private readonly float _stepper = 1000;

        private GameObject _capObject;
        private GameObject _camObject;
        private PartCamera _camera;
        private Vector3 _initialUpVector;

        [KSPField(isPersistant = true)]
        private bool _isOnboard;
        [KSPField(isPersistant = true)]
        private bool _isLookAtMe;
        [KSPField(isPersistant = true)]
        private bool _isLookAtMeAutoZoom;
        [KSPField(isPersistant = true)]
        private bool _isFollowMe;
        [KSPField(isPersistant = true)]
        private bool _isTargetCam;

        [KSPField(isPersistant = true)]
        private float _isFollowMeOffsetX;
        [KSPField(isPersistant = true)]
        private float _isFollowMeOffsetY;
        [KSPField(isPersistant = true)]
        private float _isFollowMeOffsetZ;
        [KSPField(isPersistant = true)]
        private float _targetOffset = 100;

        [KSPField]
        #region NO_LOCALIZATION
        public string photoDir = "DockingCam";
        #endregion


        [KSPAction("Toggle Camera")]
        public void ToggleCameraAction(KSPActionParam param)
        {
            if (IsEnabled)
                IsEnabled = false;
            else
            {
                GetElectricState();
                IsEnabled = true;
            }
        }

        public override void OnStart(StartState state)
        {
            if (_camera != null) return;

                _camera = new PartCamera(part, resourceScanning, electricchargeCost, _bulletName, _currentHits, _rotatorZ, _rotatorY, _zoommer, 
                    _stepper, _cameraName, allowedScanDistance, windowSize, _isOnboard, _isLookAtMe, _isLookAtMeAutoZoom,
                    _isFollowMe, _isTargetCam, _isFollowMeOffsetX, _isFollowMeOffsetY, _isFollowMeOffsetZ, _targetOffset, restrictShaderTo);

            _capObject = part.gameObject.GetChild(_cap);
            _camObject = part.gameObject.GetChild(_cameraName);
            _initialUpVector = _camObject.transform.up;
            _camera.InitialCamRotation = _camera.CurrentCamRotation = _camObject.transform.rotation;
            _camera.InitialCamPosition = _camera.CurrentCamPosition = _camObject.transform.position;
            _camera.InitialCamLocalRotation = _camera.CurrentCamLocalRotation = _camObject.transform.localRotation;
            _camera.InitialCamLocalPosition = _camera.CurrentCamLocalPosition = _camObject.transform.localPosition;
            _camera.setECusageCost(electricchargeCost);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_DockingCam_100") +
                   Localizer.Format("#LOC_DockingCam_101");
        }

        public void Update()
        {
            if (IsEnabled)
                GetElectricState();
        }
        public override void OnUpdate()
        {
            if (_camera == null) return;

            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().FCS && part.vessel != FlightGlobals.ActiveVessel && IsEnabled)
            {
                var dist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, part.vessel.transform.position);
                var treshhold = vessel.vesselRanges.orbit.load;
                if (dist > treshhold*0.99)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_DockingCam_102"), 3f, ScreenMessageStyle.UPPER_CENTER);
                    _camera.IsButtonOff = true;
                }
            }

            if (_isPowered == Localizer.Format("#LOC_DockingCam_103"))
            {
                if (IsEnabled)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_DockingCam_104"), 3f, ScreenMessageStyle.UPPER_CENTER);
                _camera.IsButtonOff = true;
            }

            if (_camera.IsButtonOff)
            {
                IsEnabled = false;
                _camera.IsButtonOff = false;
            }

            if (_camera.IsToZero)
            {
                _camera.IsToZero = false;
                StartCoroutine(_camera.ReturnCamToZero());
            }

            if (_camera.IsWaitForRay)
            {
                _camera.IsWaitForRay = false;
                StartCoroutine(_camera.WaitForRay());
            }

            _isOnboard = _camera.IsOnboard;
            _isLookAtMe = _camera.IsLookAtMe;
            _isLookAtMeAutoZoom = _camera.IsLookAtMeAutoZoom;
            _isFollowMe = _camera.IsFollowMe;
            _isTargetCam = _camera.IsTargetCam;

            if (_isFollowMe)
            {
                _isFollowMeOffsetX = _camera.IsFollowMeOffsetX;
                _isFollowMeOffsetY = _camera.IsFollowMeOffsetY;
                _isFollowMeOffsetZ = _camera.IsFollowMeOffsetZ;               
            }

            if (_isTargetCam)
                _targetOffset = _camera.TargetOffset;

            if (_isOnboard)
                Onboard();
            if (_isLookAtMe)
                LookAtMe();
            if (_isFollowMe)
                FollowMe();
            if (_isTargetCam)
                TargetCam();

            _currentHits = _camera.Hits;
            _aboutHits = _currentHits + Localizer.Format("#LOC_DockingCam_105");

            if (IsEnabled)
                Activate();
            else
                Deactivate();

            if (_camera.IsAuxiliaryWindowButtonPres)
                StartCoroutine(_camera.ResizeWindow());
            if (_camera.IsActive)
                _camera.Update();
        }

        private void SetCurrentMode(bool a, bool b, bool c, bool d)
        {
            _camera.IsOnboardEnabled = a;
            _camera.IsLookAtMeEnabled = b;
            _camera.IsFollowEnabled = c;
            _camera.IsTargetCamEnabled = d;
        }

        private void Onboard()
        {
            SetCurrentMode(true, false, false, false);
        }

        private void LookAtMe()
        {
            SetCurrentMode(false, true, false, false);
            if (_camera.IsLookAtMeAutoZoom)
            {
                float dist = Vector3.Distance(_camObject.transform.position, FlightGlobals.ActiveVessel.vesselTransform.position);
                if (dist < 50)
                    _camera.CurrentZoom = _camera.MaxZoom;
                if (dist > 50 && dist < 100)
                    _camera.CurrentZoom = 23;  //x10
                if (dist > 100 && dist < 200)
                    _camera.CurrentZoom = 13; //x20
                if (dist > 200 && dist < 400)
                    _camera.CurrentZoom = 3;  //x30 
                if (dist > 400)
                    _camera.ZoomMultiplier = true;
                if (_camera.ZoomMultiplier)
                {
                    if (dist > 400 && dist < 800) _camera.CurrentZoom = 23;  //
                    if (dist > 800 && dist < 1600) _camera.CurrentZoom = 13; //
                    if (dist > 1600 && dist < 3200) _camera.CurrentZoom = 3; //
                }                
            }
            _camObject.transform.LookAt(FlightGlobals.ActiveVessel.CoM, _initialUpVector);
        }

        private void FollowMe()
        {
            if (!_camera.IsFollowEnabled)
            {
                SetCurrentMode(false, false, true, false);
                _camera.CurrentCamTarget = FlightGlobals.ActiveVessel.vesselTransform;
                _camera.CurrentCam = _camObject.transform;
            }

            var offset = _camera.CurrentCamTarget.right * _isFollowMeOffsetX 
                + _camera.CurrentCamTarget.up * _isFollowMeOffsetY 
                + _camera.CurrentCamTarget.forward * _isFollowMeOffsetZ;
            _camera.CurrentCam.position = _camera.CurrentCamTarget.position + offset;
            _camera.CurrentCam.LookAt(FlightGlobals.ActiveVessel.CoM, _camera.CurrentCamTarget.up);
        }

        private void TargetCam()
        {
            var target = TargetHelper.Target; // as Vessel;
            if (target == null)
            {
                SetCurrentMode(true, false, false, false);
                return;
            }
            var target1 = target.GetVessel();
            SetCurrentMode(false,false,false,true);
            var direction = target1.transform.position - FlightGlobals.ActiveVessel.transform.position;
            direction.Normalize();
            _camObject.transform.position = target1.CoM - direction * _targetOffset;
            var vectorUpNormalised = vessel.vesselTransform.up.normalized;
            _camObject.transform.LookAt(target1.transform, vectorUpNormalised);                
        }

        private void GetElectricState()
        {
            double electricChargeAmount = 0;
            int electricityId = PartResourceLibrary.Instance.GetDefinition(Localizer.Format("#LOC_DockingCam_36")).id;

            if (HighLogic.LoadedSceneIsEditor)
            {
                var parts = EditorLogic.fetch.ship.parts;
                foreach (var p in parts)
                {
                    foreach (var r in p.Resources)
                    {
                        if (r.info.id == electricityId) // definition.id)
                            electricChargeAmount += r.amount;
                    }
                }
            }
            
            if (HighLogic.LoadedSceneIsFlight)
            {
                double electricChargeMaxAmount;
                part.GetConnectedResourceTotals(electricityId, out electricChargeAmount, out electricChargeMaxAmount);
            }
            
            if (electricChargeAmount > 0)
                _isPowered = IsEnabled ? Localizer.Format("#LOC_DockingCam_106") : Localizer.Format("#LOC_DockingCam_107");
            else
                _isPowered = Localizer.Format("#LOC_DockingCam_103");
        }

        #region NO_LOCALIZATION
        public void Activate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (_camera == null)
            {
                Log.Info("_camera is null, reinitializing");
                OnStart(StartState.Orbital);
                _camera.IsActive = true;
            }
            if (_camera.IsActive) return;
            _camera.SetPhotoDir(photoDir);
            _camera.Activate();
            StartCoroutine("CapRotator");
        }
        public void Deactivate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (_camera == null || !_camera.IsActive) return;
            _camera.Deactivate();

            if (!IsEnabled) StartCoroutine("CapRotator");
        }

        #endregion
        private IEnumerator CapRotator()
        {
            int step = _camera.IsActive ? 5 : -5;
            for (var i = 0; i < 54; i++)
            {
                _capObject.transform.Rotate(new Vector3(0, 1f, 0), step);
                yield return new WaitForSeconds(1f / 270);
            }
        }
    }
    interface ICamPart
    {
        void Activate();
        void Deactivate();
    }
}
