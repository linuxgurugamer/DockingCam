using System.Collections;
using OLDD_camera.Camera;
using OLDD_camera.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Modules
{
    public  class DockingCameraModule : PartModule, ICamPart
    {
        /// <summary>
        /// Module adds an external camera and gives control over it
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Docking Camera", isPersistant = true),
        UI_Toggle(controlEnabled = true, enabledText = "ON", disabledText = "OFF", scene = UI_Scene.Flight)]
        public bool IsEnabled;

        [KSPField(isPersistant = true)]
        public bool noise;

        [KSPField(isPersistant = true)]
        private bool _crossDPAI;

        [KSPField(isPersistant = true)]
        private bool _crossOLDD;

        [KSPField(isPersistant = true)]
        private bool _targetCrossStock = true;

        private int _windowSize = 256;
        internal DockingCamera _camera;

        // cameraName is a transform where the camera should be.
        [KSPField]
        public string cameraName = "";

        [KSPField]
        public string cameraLabel = "";

        [KSPField]
        public string windowLabel = "";

        [KSPField]
        public bool crossDPAIonAtStartup = true;
        [KSPField]
        public bool crossOLDDonAtStartup = true;
        [KSPField]
        public bool targetCrossStockOnAtStartup = true;
        [KSPField]
        public bool slidingOptionWindow = true;
        [KSPField]
        public bool allowZoom = true;


        [KSPField( isPersistant = true )]
        public Vector3 cameraPosition = Vector3.zero;

        [KSPField(isPersistant = true)]
        public Vector3 cameraForward = Vector3.up;

        [KSPField(isPersistant = true)]
        public Vector3 cameraUp = Vector3.up;

        [KSPField(isPersistant = true)]
        public bool transformModification = true;

        [KSPField]
        public bool devMode = false;

        CameraAdjust.CameraAdjuster ca = null;
        [KSPEvent(guiActive = true, guiName = "Camera Adjuster")]
        public void StartCameraAdjuster()
        {
            if (ca == null)
            {
                ca = this.gameObject.AddComponent<CameraAdjust.CameraAdjuster>();
            }
            ca.SetDcm(this);
            
            ca.active = !ca.active;
            if (!ca.active)
            {
                ca.OnDestroy();
                ca = null;
            }
        }

        //static int cameraCnt = 0;
        //int thisCamera = -1;
        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor || _camera != null) return;
 
            if (_camera == null)
            {
                if(cameraName != "")
                    _camera = new DockingCamera(this, part, 
                        noise, targetCrossStockOnAtStartup, crossDPAIonAtStartup, crossOLDDonAtStartup, transformModification,
                        _windowSize, windowLabel, cameraName, 
                        slidingOptionWindow, allowZoom);
                else
                    _camera = new DockingCamera(this, part, 
                        noise, targetCrossStockOnAtStartup, crossDPAIonAtStartup, crossOLDDonAtStartup, transformModification,
                        _windowSize);
            }
            if (cameraLabel != "")
                Fields["IsEnabled"].guiName = cameraLabel;
            _crossDPAI = crossDPAIonAtStartup;
            _crossOLDD = crossOLDDonAtStartup;
            _targetCrossStock = targetCrossStockOnAtStartup;
            if (!devMode)
            {
                Events["StartCameraAdjuster"].guiActive = false;
            }
        }

        public override void OnUpdate()
        {
            if (_camera == null) return;

            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings>().FCS && part.vessel != FlightGlobals.ActiveVessel && IsEnabled)
            {
                var dist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, part.vessel.transform.position);
                var treshhold = vessel.vesselRanges.orbit.load;
                if (dist > treshhold * 0.99)
                    _camera.IsButtonOff = true; 
            }

            if (_camera.IsButtonOff)
            {
                IsEnabled = false;
                _camera.IsButtonOff = false;
    
            }

            if (IsEnabled)
                Activate();
            else
                Deactivate();

            noise = _camera.Noise;
            _crossDPAI = _camera.TargetCrossDPAI;
            _crossOLDD = _camera.TargetCrossOLDD;
            _targetCrossStock = _camera.TargetCrossStock;

            if (_camera.IsAuxiliaryWindowButtonPres)
                StartCoroutine(_camera.ResizeWindow());
            if (_camera.IsActive)
                _camera.Update();
        }

        public void Activate()
        {
            if (_camera.IsActive) return;
            
            _camera.Activate();
            StartCoroutine("WhiteNoise"); 
        }

        public void Deactivate()
        {
            if (!_camera.IsActive) return;
            StopCoroutine("WhiteNoise");
            _camera.Deactivate();
        }

        private IEnumerator WhiteNoise() 
        {
            while (_camera.IsActive)
            {
                _camera.UpdateNoise();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
