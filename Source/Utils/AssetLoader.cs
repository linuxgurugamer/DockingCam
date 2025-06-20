using KSP.Localization;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace OLDD_camera.Utils
{
    [KSPAddon(KSPAddon.Startup.Flight, true)]

    public class AssetLoader : MonoBehaviour
    {
        public static Texture2D texSelfRot = null;
        public static Texture2D texTargetRot = null;
        public static Texture2D texTargetPoint = null;
        public static Texture2D texLampOn = null;
        public static Texture2D texLampOff = null;
        public static Texture2D texDockingCam = null;
        public static List<Material> materials = null;

        IEnumerator Start()
        {
            string url = "file://" + Path.GetFullPath(KSPUtil.ApplicationRootPath) + "GameData/DockingCamKURS/Resources/DockingCam.ksp";
            //            string url = "file://" + KSPUtil.ApplicationRootPath + "GameData/DockingCamKURS/Resources/dockingcamshaders.shaders";

            while (!Caching.ready)
                yield return null;

            Debug.Log("OLDD_AssetLoader: Start caching ready, url: " + url);

            // Start a download of the given URL
            WWW www = new WWW(url);
            // Wait for download to complete
            yield return www;

            // Load and retrieve the AssetBundle
            Debug.Log("OLDD_AssetLoader: finished");
            AssetBundle bundle = www.assetBundle;
            var shaderAssets = bundle.LoadAllAssets<Shader>();
            if (shaderAssets == null)
                Log.Error("shaderAssets is null");
            //var materials = new List<Material>();
            materials = new List<Material>();
#if DEBUG
            foreach (var i in bundle.GetAllAssetNames())
            {
                Log.Info("assetName: " + i);
            }
#endif
            foreach (var shader in shaderAssets)
            {
                Log.Info("shader: " + shader.name);

                materials.Add(new Material(shader));

            }

            Log.Info("materials.Count: " + materials.Count);
            var index = materials.FindIndex(x => x.name.Contains(Localizer.Format("#LOC_DockingCam_108")));
            var item = materials[index];
            materials[index] = materials[0];
            materials[0] = item;


            texSelfRot = (Texture2D)bundle.LoadAsset("selfrot");
            texTargetRot = (Texture2D)bundle.LoadAsset("targetrot");
            texTargetPoint = (Texture2D)bundle.LoadAsset("targetpoint");
            texLampOn = (Texture2D)bundle.LoadAsset("lampon");
            texLampOff = (Texture2D)bundle.LoadAsset("lampoff");
            texDockingCam = (Texture2D)bundle.LoadAsset("dockingcam");

            www.Dispose();
        }
    }
}
