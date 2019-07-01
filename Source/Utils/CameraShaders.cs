using UnityEngine;
using UnityEngine.UI;

namespace OLDD_camera.Utils
{
//    public enum ShaderType
//    {
//        None = 0,
//        CRT = 1,              // Custom/CRT
//        NightVision = 2,  // Hidden/NightVision
//        NightVisionClear = 3,   // NightVisionClear
//        Grayscale = 4,          // Custom/MovieTime
//        Thermal = 5,            // CameraFilterPack/CameraFilterPack_Classic_ThermalVision
// #if SHADERTEST
//        Test = 5,          // none
//#endif
       
//    }


    internal class CameraShaders
    {
        /* dynamicload
        public static Material GetShader(ShaderType type)
        {

            switch (type)
            {
                case ShaderType.CRT: return CRT;
                case ShaderType.NightVision: return NightVision;                    
                case ShaderType.NightVisionClear: return NightVisionClear;
                case ShaderType.Grayscale: return Grayscale;
                case ShaderType.Thermal: return ThermalVision;
                case ShaderType.None: return NoneMaterial;
#if SHADERTEST
                case ShaderType.Test: return TestMaterial;
#endif
                default: return null;
            }
        }

        
        static Material crt = null;
        static Material grayscale = null;
        static Material nightvisionclear = null;
        static Material nightvision = null;
        static Material thermalvision = null;
        static Material noneMaterial = null;
#if SHADERTEST
        static Material testMaterial = null;
#endif
        public static Material CRT
        {
            get
            {
                if (crt == null)
                    crt = AssetLoader.matCRT;
                return crt;
            }
        }

        public static Material Grayscale
        {
            get
            {
                if (grayscale == null)
                    grayscale = AssetLoader.matGrayscale; //new Material(Shader.Find("Hidden/Grayscale Effect"));
                return grayscale;
            }
        }

        public static Material NightVisionClear
        {
            get
            {
                if (nightvisionclear == null)
                    nightvisionclear = AssetLoader.matNightVisionClear;
                return nightvisionclear;
            }
        }

        public static Material NightVision
        {
            get
            {
                if (nightvision == null)
                    nightvision = AssetLoader.matNightVision;
                return nightvision;
            }
        }

        public static Material ThermalVision
        {
            get
            {
                if (thermalvision == null)
                    thermalvision = AssetLoader.matThermal;
                return thermalvision;
            }
        }
        public static Material NoneMaterial
        {
            get
            {
                if (noneMaterial == null)
                    noneMaterial = AssetLoader.matNone;
                return noneMaterial;
            }
        }
#if SHADERTEST
        public static Material TestMaterial
        {
            get
            {
                if (testMaterial == null)
                    testMaterial = AssetLoader.matTest;
                return testMaterial;
            }
        }
#endif
*/
    }
}
