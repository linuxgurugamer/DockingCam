
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OLDD_camera.CameraAdjust
{
    // Behaviour to make the MarkerCam move with the main camera - I suspect that I do not need this TODO try to remove
    public class MarkerCamBehaviour : MonoBehaviour
    {
        private void LateUpdate()
        {
            gameObject.transform.position = FlightCamera.fetch.cameras[0].gameObject.transform.position;
            gameObject.transform.rotation = FlightCamera.fetch.cameras[0].gameObject.transform.rotation;
            // print("Setting markercam to be at: " + this.gameObject.transform.position + " and rotation " + Camera.main.transform.position);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class InFlightMarkerCam : MonoBehaviour
    {
        private GameObject _markerCamObject;
        internal static UnityEngine.Camera MarkerCam;

        private void Awake()
        {
            //print("InFlightMarkerCam::Awake");
            _markerCamObject = null;
        }

        public void Start()
        {
            print("InFlightMarkerCam::Start");
            CreateMarkerCam();
            GameEvents.onVesselChange.Add(OnVesselChange);
        }

        public bool MarkerCamEnabled
        {
            get { return MarkerCam?.enabled ?? false; }
            set
            {
                if (MarkerCam == null) CreateMarkerCam();
                if (MarkerCam != null) MarkerCam.enabled = value;
            }
        }
        private void DestroyMarkerCam()
        {
            // print("InFlightMarkerCam::DestroyMarkerCam");
            if (null == _markerCamObject) return;
            // print("Shutting down the inflight MarkerCamObject");
            // There should be only one, but lets do all of them just in case.
            IEnumerator mcbs = _markerCamObject.GetComponents<MarkerCamBehaviour>().GetEnumerator();
            while (mcbs.MoveNext())
            {
                if (mcbs.Current == null) continue;
                Destroy((MarkerCamBehaviour)mcbs.Current);
            }

            Destroy(_markerCamObject);

            _markerCamObject = null;
        }

        private void CreateMarkerCam()
        {
            if (null != _markerCamObject) return;

            // print("Setting up the inflight MarkerCamObject");
            _markerCamObject = new GameObject("MarkerCamObject");
            _markerCamObject.transform.parent = FlightCamera.fetch.cameras[0].gameObject.transform;//UnityEngine.Camera.mainCamera.gameObject.transform; // Set the new camera to be a child of the main camera  
            MarkerCam = _markerCamObject.AddComponent<UnityEngine.Camera>();

            // Change a few things - the depth needs to be higher so it renders over the top
            MarkerCam.name = "markerCam";
            MarkerCam.depth = UnityEngine.Camera.main.depth + 10;
            MarkerCam.clearFlags = CameraClearFlags.Depth;
            MarkerCam.allowHDR = false;
            //MarkerCam.hdr = false;
            // Add a behaviour so we can get the MarkerCam to come around and change itself when the main camera changes
            _markerCamObject.AddComponent<MarkerCamBehaviour>(); // TODO can this be removed?

            // Set the culling mask. 
            MarkerCam.cullingMask = 1 << 17;
        }

        internal static UnityEngine.Camera GetMarkerCam()
        {
            IEnumerator cams = UnityEngine.Camera.allCameras.GetEnumerator();
            while (cams.MoveNext())
            {
                if (cams.Current == null) continue;
                if (((UnityEngine.Camera)cams.Current).name == "markerCam")
                {
                    return ((UnityEngine.Camera)cams.Current);
                }
            }
            return null;
        }

        private void OnVesselChange(Vessel data)
        {
            //Debug.Log("Setting MarkerCam.enabled from OnVesselChange");
            MarkerCamEnabled = !data.isEVA;
        }


        private void OnDestroy()
        {
            DestroyMarkerCam();
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }
    }

}
