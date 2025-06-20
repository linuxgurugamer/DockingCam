using KSP.Localization;
using OLDD_camera.Camera;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace OLDD_camera.Modules
{
    public class DockingCameraModule : PartModule //, ICamPart
    {

        /// <summary>
        /// Module adds an external camera and gives control over it
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Docking Camera", isPersistant = true),
        UI_Toggle(controlEnabled = true, enabledText = "#LOC_DockingCam_90", disabledText = "#LOC_DockingCam_91", scene = UI_Scene.Flight)]
        public bool IsEnabled;

        [KSPField(isPersistant = true)]
        public bool noise;

        [KSPField(isPersistant = true)]
        public double electricchargeCost = 0.02d;

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

        [KSPField]
        public string restrictShaderTo = "";     // a comma delimiated list of shader names, the part after the slash

        [KSPField(isPersistant = true)]
        public Vector3 cameraPosition = Vector3.zero;

        [KSPField(isPersistant = true)]
        public Vector3 cameraForward = Vector3.up;

        [KSPField(isPersistant = true)]
        public Vector3 cameraUp = Vector3.up;

        [KSPField(isPersistant = true)]
        public bool transformModification = true;

#if DEBUG
        [KSPField]
        public bool devMode = true;
#else
        [KSPField]
        public bool devMode = false;
#endif

        //////////////////////////////////////////////////////////////////////////////

        [KSPField]
        public bool canTakePicture = false;				// Can this camera take a picture

        [KSPField(isPersistant = false)]
        public int cameraHorizontalResolution = 256;	// Horizontal res for picture taking

        [KSPField(isPersistant = false)]
        public int cameraVerticalResolution = 256;		// Vertical res for picture taking

        [KSPField]
        public string deployAnimationName = null;       // If there is a deploy animation needed to be open before the camera is available

        [KSPField]
        public bool openBeforePic = false;				// Does the deploy animation need to be open to take a picture

        [KSPField]
        public bool openIsOne = true;					// relative to the deploy animation, indicates which way is open

        [KSPField(isPersistant = false)]
        public string cameraTransformName = "";

        [KSPField]
        public float cameraCustomNearClipPlane = 0f;	// Used for clipping the near camera.  

        [KSPField]
        public bool isDockingNode = true;				// Is this a docking node

        [KSPField]
        #region NO_LOCALIZATION
        public string photoDir = "DockingCam";
        #endregion

        //////////////////////////////////////////////////////////////////////////////



        [KSPAction("Toggle Docking Camera")]
        public void EnableAction(KSPActionParam param)
        {
            if (IsEnabled)
                IsEnabled = false;
            else
                IsEnabled = true;
        }

#if false
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
#endif

        //static int cameraCnt = 0;
        //int thisCamera = -1;
        //public override void OnStart(StartState state)
        //{
        //    if (state == StartState.Editor || _camera != null) return;

        Animation deployAnim = null;
                #region NO_LOCALIZATION
        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (cameraName != "")
                {
                    _camera = new DockingCamera(this, part,
                        noise, electricchargeCost, targetCrossStockOnAtStartup, crossDPAIonAtStartup, crossOLDDonAtStartup, transformModification,
                        _windowSize, restrictShaderTo,
                        windowLabel, cameraName, slidingOptionWindow, allowZoom,
                        cameraTransformName);
                }
                else
                {
                    _camera = new DockingCamera(this, part,
                        noise, electricchargeCost, targetCrossStockOnAtStartup, crossDPAIonAtStartup, crossOLDDonAtStartup, transformModification,
                        _windowSize, restrictShaderTo, allowZoom: allowZoom);
                }

                if (cameraLabel != "")
                    Fields["IsEnabled"].guiName = cameraLabel;

                _crossDPAI = crossDPAIonAtStartup;
                _crossOLDD = crossOLDDonAtStartup;
                _targetCrossStock = targetCrossStockOnAtStartup;
#if false
            if (!devMode)
            {
                Debug.Log("OLDD DockingCameraModule 5");
                Events["StartCameraAdjuster"].guiActive = false;
            }
#endif
                _camera.setECusageCost(electricchargeCost);
                //}

                //_camera.DoStart();
                if (deployAnimationName != null)
                {
                    deployAnim = part.FindModelAnimators(deployAnimationName).FirstOrDefault();

                    if (deployAnim != null)
                        InvokeRepeating("CheckIfAvailable", 0, 1f);
                }
            }
        }

        void OnDestroy()
        {
            if (_camera != null)
                _camera.DoOnDestroy();
        }
        void CheckIfAvailable()
        {
            //Log.Info("CheckIfAvailable, deployAnimationName: " + deployAnimationName + ", openBeforePic: " + openBeforePic +
            //    ", openIsOne: " + openIsOne + ", time: " + deployAnim[deployAnimationName].time +
            //    ", length: " + deployAnim[deployAnimationName].length + ", normalizedSpeed: " + deployAnim[deployAnimationName].normalizedSpeed);
            if (openBeforePic)
            {
                if (openIsOne)
                {
                    if (deployAnim[deployAnimationName].normalizedSpeed <= 0)
                        Fields["IsEnabled"].guiActive = false;
                    else
                        Fields["IsEnabled"].guiActive = true;
                }
                else
                {
                    if (deployAnim[deployAnimationName].normalizedSpeed > 0)
                        Fields["IsEnabled"].guiActive = true;
                    else
                        Fields["IsEnabled"].guiActive = false;

                }
            }
        }

                #endregion

        public override void OnUpdate()
        {
            if (_camera == null) return;

            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().FCS && part.vessel != FlightGlobals.ActiveVessel && IsEnabled)
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

        #region NO_LOCALIZATION
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
        #endregion

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
