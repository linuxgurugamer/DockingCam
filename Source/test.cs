using KSP.Localization;
using OLDD_camera.Utils;
using UnityEngine;

namespace OLDD_camera
{
    internal class test
    {
        void test1()
        {
            string a = Localizer.Format("#LOC_DockingCam_34");
            float y = 1.23f;



            GUILayout.Label(Localizer.Format("#LOC_DockingCam_35") + $"{y}" + "f2" + "}", Styles.RedLabel13);
            GUILayout.Label($ "vY:" + " {y}" + "f2" + "}", Styles.RedLabel13);
            GUILayout.Label($" {y}" + "f2" + "}", Styles.RedLabel13);
            GUILayout.Label($"{y}" + "f2" + "}", Styles.RedLabel13);

        }
    }
}
