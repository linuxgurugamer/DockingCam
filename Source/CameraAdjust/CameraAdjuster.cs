using System;
using System.IO;
using UnityEngine;


namespace OLDD_camera.CameraAdjust
{
    

    public class CameraAdjuster : MonoBehaviour
    {
        // Define the column widths here
        // 
        const int LABELWIDTH = 120;
        const int BUTTONWIDTH = 30;

        Modules.DockingCameraModule dcm;        // link to parent module
        internal bool active = false;
        string newConfig = "";
        string path = "";

        // Window information
        Rect winPos = new Rect(20, 20, 550, 300);
        static int caWinCnt = 0;                    // used to offset successive windows if multiple are displayed
        int CamAdjId = Utils.Util.GetRandomInt();   // window id

        public GameObject cameraMarker = null;
        bool MarkerVisible = true;

        float deltaXYZ = 0.1f;
        float deltaFwd = 1f;
        float deltaUp = 1f;

        void CreateCameraObject()
        {
            // Try to create a game object using our marker mesh
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings>().useNodeObj)
            {
                cameraMarker = Instantiate(GameDatabase.Instance.GetModel("DockingCamKURS/Assets/PWBComMarker/PWBTargetComMarker"));
                // Make it a bit smaller - we need to fix the model for this
                cameraMarker.transform.localScale = Vector3.one / 5f;
            }
            else
            {
                cameraMarker = Instantiate(GameDatabase.Instance.GetModel("DockingCamKURS/Assets/hullcam_hubbazoot/model2"));
                cameraMarker.transform.localScale = Vector3.one * 2f;
            }
            Collider m_Collider = cameraMarker.GetComponent<Collider>();
            if (m_Collider != null)
                m_Collider.enabled = false;
        }

        private void CreateSavedCameraMarker()
        {
            if (cameraMarker == null)
                Utils.Log.Error("cameraMarker is null");

            // Do not try to create the marker if it already exisits
            if (null != cameraMarker) return;
            // First try to find the camera that will be used to display the marker - it needs a special camera to make it "float"
            UnityEngine.Camera markerCam = InFlightMarkerCam.GetMarkerCam();

            if (markerCam == null)
                Utils.Log.Error("markerCam is null");
            // Did we find the camera? If we did then set up the marker object, and display it via tha camera we found
            if (null == markerCam) return;
            CreateCameraObject();


            // Add a behaviour to it to allow us to control it and link it to the part that is marks the saved CoM position for
            cameraMarker.AddComponent<CameraMarker>();
            // Tell the marker which instance of the PWBFueldBalancingModule it is displaying the set CoM location for (we could have more than one per vessel)
            cameraMarker.GetComponent<CameraMarker>().LinkPart(dcm);

            // Start the marker visible if it has been set to be visible, or hidden if it is set to be hidden
            cameraMarker.SetActive(MarkerVisible);
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                markerCam.enabled = MarkerVisible;

            int layer = (int)(Math.Log(markerCam.cullingMask) / Math.Log(2));
            // print("MarkerCam has cullingMask: " + markerCam.cullingMask + " setting marker to be in layer: " + layer);
            cameraMarker.layer = layer;

        }

        public void SetDcm(Modules.DockingCameraModule d)
        {
            dcm = d;
            CreateSavedCameraMarker();
        }

        public void OnDestroy()
        {
            cameraMarker.DestroyGameObject();
            Destroy(this);           
        }

        public void Start()
        {
            active = true;
            caWinCnt = (caWinCnt++) % 5;

            winPos = new Rect(20 + 25 * caWinCnt, 20 + 25 * caWinCnt, 450, 300);
           
        }

        public void OnGUI()
        {
            if (!active)
                return;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings>().useKSPskin)
                GUI.skin = HighLogic.Skin;

            winPos = GUILayout.Window(CamAdjId, winPos, DrawGUI, "Camera Adjuster: " + dcm.windowLabel);
        }

        void UpdateCamera(Vector3 v3, bool doUpdate = false)
        {
            dcm._camera.UpdateLocalPosition(dcm);

            if (doUpdate)
                cameraMarker.GetComponent<CameraMarker>().DoUpdate(v3);
            else
                cameraMarker.GetComponent<CameraMarker>().DoUpdate(Vector3.zero);
        }

        void DrawGUI(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(BUTTONWIDTH + LABELWIDTH + 10));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current XYZ: (" + FormatVector3(dcm.cameraPosition) + ")");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("XYZ Delta (" + deltaXYZ.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaXYZ /= 10;
                    if (deltaXYZ < 0.001)
                        deltaXYZ = 0.001f;
                    deltaXYZ = (float)Math.Round(deltaXYZ, 3);
                }

                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaXYZ *= 10;
                    if (deltaXYZ > 10) deltaXYZ = 10;
                }
            }
            GUILayout.EndHorizontal();
            Vector3 v3 = Vector3.zero;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("X (" + dcm.cameraPosition.x.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.x -= deltaXYZ;
                    v3.x = -deltaXYZ;
                    UpdateCamera(v3, true);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.x += deltaXYZ;
                    v3.x = deltaXYZ;
                    UpdateCamera(v3, true);
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Y (" + dcm.cameraPosition.y.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.y -= deltaXYZ;
                    v3.y = -deltaXYZ;
                    UpdateCamera(v3, true);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.y += deltaXYZ;
                    v3.y = deltaXYZ;
                    UpdateCamera(v3, true);
                }


            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Z (" + dcm.cameraPosition.z.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.z -= deltaXYZ;
                    v3.z = -deltaXYZ;
                    UpdateCamera(v3, true);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraPosition.z += deltaXYZ;
                    v3.z = deltaXYZ;
                    UpdateCamera(v3, true);
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("===================");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Fwd: (" + FormatVector3(dcm.cameraForward) + ")");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Fwd Delta (" + deltaFwd.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaFwd /= 10;
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaFwd *= 10;
                }

            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("X (" + dcm.cameraForward.x.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.x -= deltaFwd;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.x += deltaFwd;
                    UpdateCamera(v3, false);
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Y (" + dcm.cameraForward.y.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.y -= deltaFwd;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.y += deltaFwd;
                    UpdateCamera(v3, false);
                }


            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Z (" + dcm.cameraForward.z.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.z -= deltaFwd;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraForward.z += deltaFwd;
                    UpdateCamera(v3, false);
                }

            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(20));
            GUILayout.Label(" ", GUILayout.Width(20));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Up: (" + FormatVector3(dcm.cameraUp) + ")");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Up Delta (" + deltaUp.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaUp /= 10;
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    deltaUp *= 10;
                }

            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("X (" + dcm.cameraUp.x.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.x -= deltaUp;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.x += deltaUp;
                    UpdateCamera(v3, false);
                }

            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Y (" + dcm.cameraUp.y.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.y -= deltaUp;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.y += deltaUp;
                    UpdateCamera(v3, false);
                }


            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Z (" + dcm.cameraUp.z.ToString("#0.0##") + "):", GUILayout.Width(LABELWIDTH));
                if (GUILayout.Button("-", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.z -= deltaUp;
                    UpdateCamera(v3, false);
                }
                if (GUILayout.Button("+", GUILayout.Width(BUTTONWIDTH)))
                {
                    dcm.cameraUp.z += deltaUp;
                    UpdateCamera(v3, false);
                }

            }
            GUILayout.EndHorizontal();

            dcm.slidingOptionWindow = GUILayout.Toggle(dcm.slidingOptionWindow, "Sliding Option Window");
            dcm.crossDPAIonAtStartup = GUILayout.Toggle(dcm.crossDPAIonAtStartup, "Cross DPAI on at Startup");
            dcm.crossOLDDonAtStartup = GUILayout.Toggle(dcm.crossOLDDonAtStartup, "Cross OLDD on at Startup");
            dcm.targetCrossStockOnAtStartup = GUILayout.Toggle(dcm.targetCrossStockOnAtStartup, "Target Cross Stock on at Startup");
            dcm.noise = GUILayout.Toggle(dcm.noise, "Noise");

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            UpdateCamera(v3, false);
            GUILayout.Label("Path: " + path);
            GUILayout.TextArea(newConfig);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close"))
                active = false;
            if (GUILayout.Button("Save"))
            {
                PrintConfig();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }


        static string _stripUrl(string url, string stripName)
        {
            var nameLength = stripName.Length;
            var urlLength = url.Length - nameLength - 1;

            return url.Substring(0, urlLength);
        }

        string FormatVector3(Vector3 v)
        {
            string s = Math.Round(v.x, 3).ToString("#0.0##") + ", " + Math.Round(v.y, 3).ToString("#0.0##") + ", " + Math.Round(v.z, 3).ToString("#0.0##");

            return s;
        }

        void PrintConfig()
        {
            try
            {
                ConfigNode node = new ConfigNode("MODULE");

                node.AddValue("name", "DockingCameraModule");
                node.AddValue("cameraName", dcm.cameraName);
                node.AddValue("cameraLabel", dcm.cameraLabel);
                node.AddValue("windowLabel", dcm.windowLabel);
                node.AddValue("slidingOptionWindow", dcm.slidingOptionWindow);
                node.AddValue("crossDPAIonAtStartup", dcm.crossDPAIonAtStartup);
                node.AddValue("crossOLDDonAtStartup", dcm.crossOLDDonAtStartup);
                node.AddValue("targetCrossStockOnAtStartup", dcm.targetCrossStockOnAtStartup);
                node.AddValue("noise", dcm.noise);
                node.AddValue("cameraForward", FormatVector3(dcm.cameraForward));
                node.AddValue("cameraUp", FormatVector3(dcm.cameraUp));
                node.AddValue("cameraPosition", FormatVector3(dcm.cameraPosition));

                var cfgurl = _stripUrl(dcm.part.partInfo.partUrl, dcm.part.partInfo.name);
                path = KSPUtil.ApplicationRootPath + "/GameData/" + cfgurl + "_DCM.cfg";
                newConfig = node.ToString();
                File.WriteAllText(path, node.ToString());
            }
            catch (Exception e)
            {
                Utils.Log.Error("[DCM]: Writing attachment node file threw exception: " + e.Message);
            }
        }
    }
}
