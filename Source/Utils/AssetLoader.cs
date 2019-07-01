using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace OLDD_camera.Utils 
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]

    public class AssetLoader:MonoBehaviour
    {
        /* dynamicload
        public static Material matNightVisionClear = null;
        public static Material matNightVision = null;

#if SHADERTEST
        public static Material matTest = null;
#endif
        public static Material matNone = null;
        public static Material matCRT = null;
        public static Material matGrayscale = null;
        public static Material matThermal = null;
        */

        public static Texture2D texSelfRot = null;
        public static Texture2D texTargetRot = null;
        public static Texture2D texTargetPoint = null;
        public static Texture2D texLampOn = null;
        public static Texture2D texLampOff = null;
        public static Texture2D texDockingCam = null;
        public static List<Material> materials;

        IEnumerator Start()
        {
            string url = "file://" + KSPUtil.ApplicationRootPath + "GameData/DockingCamKURS/Resources/DockingCam.ksp";
            //            string url = "file://" + KSPUtil.ApplicationRootPath + "GameData/DockingCamKURS/Resources/dockingcamshaders.shaders";
            while (!Caching.ready)
                yield return null;
            Debug.Log("OLDD_AssetLoader: Start caching ready");

            // Start a download of the given URL
            WWW www = new WWW(url);
            // Wait for download to complete
            yield return www;

            // Load and retrieve the AssetBundle
            Debug.Log("OLDD_AssetLoader: finished");
            AssetBundle bundle = www.assetBundle;
            var shaderAssets = bundle.LoadAllAssets<Shader>();
            //var materials = new List<Material>();
            materials = new List<Material>();
#if DEBUG
            foreach (var i in bundle.GetAllAssetNames())
            {
                Debug.Log("assetName: " + i);
            }
#endif
            foreach (var shader in shaderAssets)
            {
                Log.Info("shader: " + shader.name);

                materials.Add(new Material(shader));

                /* dynamicload
                if (shader.name == "Custom/CRT")                
                    matCRT = new Material(shader);

                if (shader.name == "Hidden/NightVision")
                    matNightVision =  new Material(shader);
                if (shader.name == "NightVisionClear")
                    matNightVisionClear = new Material(shader);

                if (shader.name == "Custom/UI/Grayscale")
                    matGrayscale = new Material(shader);

                if (shader.name == "CameraFilterPack/CameraFilterPack_Classic_ThermalVision")
                    matThermal = new Material(shader);
    
                if (shader.name == "Custom/None")
                    matNone = new Material(shader);
    
#if SHADERTEST
                if (shader.name == "Custom/None")
                    {
                        matTest = new Material(shader);
                }
#endif
*/
            }

            var index = materials.FindIndex(x => x.name.Contains("None"));
            var item = materials[index];
            materials[index] = materials[0];
            materials[0] = item;


            texSelfRot = (Texture2D)bundle.LoadAsset("selfrot");
            texTargetRot = (Texture2D)bundle.LoadAsset("targetrot");
            texTargetPoint = (Texture2D)bundle.LoadAsset("targetPoint");     
            texLampOn = (Texture2D)bundle.LoadAsset("lampon");
            texLampOff = (Texture2D)bundle.LoadAsset("lampoff"); 
            texDockingCam = (Texture2D)bundle.LoadAsset("dockingcam");

            www.Dispose();
        }
    }
}
