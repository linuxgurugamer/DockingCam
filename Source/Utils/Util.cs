using KSP.Localization;
using System;
using System.IO;
using System.Text;
using OLDD_camera.Camera;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;


namespace OLDD_camera.Utils
{
    public delegate void UpdateGUIFunction(); //ddd

    public class UpdateGUIObject : MonoBehaviour
    {
        //public event Action UpdateGUIFunction; //ddd
        public UpdateGUIFunction updateGUIFunction = null; //lll

        private void OnGUI()
        {
            if  (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().hideOnF2 && DockCamToolbarButton.hideUI)
                return;
            //UpdateGUIFunction?.Invoke();
            //if (UpdateGUIFunction != null) //ddd
            if (updateGUIFunction != null)
            {
                //UpdateGUIFunction(); //ddd

                if (HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                {
                    GUI.skin = HighLogic.Skin;
                }
                if (OLDD_camera.Utils.Styles.KspSkin != HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                {
                    OLDD_camera.Utils.Styles.InitStyles();
                }
                updateGUIFunction();
            }
        }
    }

    public static class Util
    {
        private const string dataTexturePath = "DockingCamKURS/";
        private const string PhotoDirectory = "Screenshots";

        public static Texture2D LoadTexture(string name)
        {
            return GameDatabase.Instance.GetTexture(dataTexturePath + name, false);
        }

        public static Texture2D MonoColorRectTexture(Color color)
        {
            return MonoColorTexture(color, 4, 4);
        }

        public static Texture2D MonoColorVerticalLineTexture(Color color, int size)
        {
            return MonoColorTexture(color, 1, size);
        }

        public static Texture2D MonoColorHorizontalLineTexture(Color color, int size)
        {
            return MonoColorTexture(color, size, 1);
        }

        public static Texture2D MonoColorTexture(Color color, int width, int height)
        {
            var texture2D = new Texture2D(width, height);
            for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                    texture2D.SetPixel(i, j, color);
            texture2D.Apply();
            return texture2D;
        }

        public static Texture2D WhiteNoiseTexture(int width, int height, float alpha = .16f)
        {
            var black = new Color(0, 0, 0, alpha);
            var white = new Color(1, 1, 1, alpha);
            width *= 2;
            height *= 2;
            var texture2D = new Texture2D(width, height);
            var colors = new Color[width * height];
            for (int i = 0; i < width * height; i++)
            {
                colors[i] = UnityEngine.Random.Range(0, 100) % 2 == 1 ? black : white;
            }
            texture2D.SetPixels(colors);
            texture2D.Apply();
            return texture2D;
        }

        /// <summary>
        /// This class converts time strings like "1d 2m 2s" into a double value as seconds and also vice versa, based on kerbin time.
        /// </summary>

        public static void SavePng(this RenderTexture renderTexture, string dir, string photoFrom)
        {
            RenderTexture.active = renderTexture;
            if (Event.current.type.Equals(EventType.Repaint))
                Graphics.Blit(renderTexture, BaseCamera.CurrentShader);
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            WritePng(texture, dir, photoFrom);
        }
        public static void WritePng(Texture2D texture, string dir, string photoFrom)
        { 
            var time = Planetarium.fetch.time;
            var photoTime = GetTimeMark(time);
            var bytes = texture.EncodeToPNG();
            var name = string.Concat(Localizer.Format("#LOC_DockingCam_109"), photoFrom, Localizer.Format("#LOC_DockingCam_110"), photoTime,
            #region NO_LOCALIZATION
                ".png");
            #endregion

            var folder = Path.Combine(PhotoDirectory, HighLogic.SaveFolder, dir);
            Directory.CreateDirectory(folder);
            name = Path.Combine(folder,  name);
            File.WriteAllBytes(name, bytes);
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_DockingCam_111"), 3f, ScreenMessageStyle.UPPER_CENTER);
        }
        public static string GetTimeMark(double universalTime)
        {
            var time = universalTime;
            var timeMark = new StringBuilder();
            if (time >= 9201600)
                time = Converter(time, timeMark, 9201600, Localizer.Format("#LOC_DockingCam_112"));
            if (time >= 21600)
                time = Converter(time, timeMark, 21600, "d");
            if (time >= 3600)
                time = Converter(time, timeMark, 3600, Localizer.Format("#LOC_DockingCam_113"));
            if (time >= 60)
                time = Converter(time, timeMark, 60, Localizer.Format("#LOC_DockingCam_114"));
            timeMark.Append(time.ToString("F0"));
            timeMark.Append(Localizer.Format("#LOC_DockingCam_115"));
            return timeMark.ToString();
        }

        private static double Converter(double time, StringBuilder timeMark, uint seconds, string suffix)
        {
            timeMark.Append(Math.Floor(time / seconds));
            timeMark.Append(suffix);
            return time % seconds;
        }
        public static Rect ConstrainToScreen(Rect r, int limit)
        {
            r.x = Mathf.Clamp(r.x, limit - r.width, Screen.width - limit);
            r.y = Mathf.Clamp(r.y, limit - r.height, Screen.height - limit);
            return r;
        }

        public static int randomSeed = new Random().Next();
        private static int nextRandomInt = randomSeed;

        public static int GetRandomInt()
        {
            nextRandomInt++;
            return nextRandomInt;
        }

    }
}
