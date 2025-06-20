using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace OLDD_camera
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings
    // HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings>().
    public class KURSSettings_1 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#LOC_DockingCam_22"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return Localizer.Format("#LOC_DockingCam_23"); } }
        public override string DisplaySection { get { return Localizer.Format("#LOC_DockingCam_23"); } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("2250",
            toolTip = "#LOC_DockingCam_24")]
        public bool _dist2500 = true;

        [GameParameters.CustomParameterUI("9999",
            toolTip = "#LOC_DockingCam_24")]
        public bool _dist9999 = false;


        [GameParameters.CustomParameterUI("Scale camera capabilites to career mode",
            toolTip = "#LOC_DockingCam_25")]
        public bool scaleToCareer = false;


        [GameParameters.CustomParameterUI("Cam shutdown if out of range",
            toolTip = "#LOC_DockingCam_24")]
        public bool FCS = false;

        [GameParameters.CustomParameterUI("Show cross",
            toolTip = "#LOC_DockingCam_26")]
        public bool showCross = true;

        [GameParameters.CustomParameterUI("Show summary data",
            toolTip = "#LOC_DockingCam_27")]
        public bool showSummaryData = true;

        [GameParameters.CustomParameterUI("Show data",
            toolTip = "#LOC_DockingCam_28")]
        public bool showData = true;

        [GameParameters.CustomParameterUI("Show rotator dials",
            toolTip = "#LOC_DockingCam_29")]
        public bool showDials = true;

        [GameParameters.CustomIntParameterUI("Default camera window size", minValue = 1, maxValue = 10,
            toolTip = "#LOC_DockingCam_30")]
        public int defaultCamWindowSize = 2;


        [GameParameters.CustomParameterUI("Use KSP skin")]
        public bool useKSPskin = false;

        [GameParameters.CustomParameterUI("Hide window(s) on F2")]
        public bool hideOnF2 = true;

        [GameParameters.CustomParameterUI("Use camera object for adjustments",
            toolTip = "#LOC_DockingCam_31")]
        public bool useCamObj = true;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {        }

        bool oDist2500;
        bool oDist9999;
        bool distInitted = false;

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (distInitted)
            {
                if (oDist2500 != _dist2500)
                {
                    _dist9999 = !_dist2500;
                }
                else if (oDist9999 != _dist9999)
                {
                    _dist2500 = !_dist9999;
                }
            }
            distInitted = true;
            oDist2500 = _dist2500;
            oDist9999 = _dist9999;

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            // if (member.Name == "DefaultSettings" && DefaultSettings)
            //SetDifficultyPreset(parameters.preset);

            if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Parameters != null)
            {
                if (OLDD_camera.Utils.Styles.KspSkin != HighLogic.CurrentGame.Parameters.CustomParams<KURSSettings_1>().useKSPskin)
                {
                    OLDD_camera.Utils.Styles.InitStyles();
                }
            }
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }



    public class KURSSettings_2 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#LOC_DockingCam_32"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return Localizer.Format("#LOC_DockingCam_23"); } }
        public override string DisplaySection { get { return Localizer.Format("#LOC_DockingCam_23"); } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Use Dehim's changes",
            toolTip = "#LOC_DockingCam_33")]
        public bool altOverlay = false;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        { }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        { return true; }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        { return true; }

        public override IList ValidValues(MemberInfo member)
        { return null; }
    }

}
