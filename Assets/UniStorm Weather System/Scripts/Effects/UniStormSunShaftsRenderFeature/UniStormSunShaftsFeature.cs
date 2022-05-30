using UnityEngine;
using UnityEngine.Rendering.Universal;


public class UniStormSunShaftsFeature : ScriptableRendererFeature
{
    public enum BufferType
    {
        CameraColor,
        Custom
    }

    public enum SunShaftsResolution
    {
        Low = 0,
        Normal = 1,
        High = 2,
    }

    public enum ShaftsScreenBlendMode
    {
        Screen = 0,
        Add = 1,
    }

    [System.Serializable]
    public class Settings
    {
        public bool isEnabled = true;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        //public Shader sunShaftsShader;
        public SunShaftsResolution resolution = SunShaftsResolution.Normal;
        public ShaftsScreenBlendMode screenBlendMode = ShaftsScreenBlendMode.Screen;

        public Transform sunTransform;
        public int radialBlurIterations = 2;
        public Color sunColor = Color.white;
        public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);
        public float sunShaftBlurRadius = 2.5f;
        public float sunShaftIntensity = 1.15f;

        public float maxRadius = 0.75f;

        //public float leftSunVectorAdjustment = 0;
        //public float rightSunVectorAdjustment = 0;

        public bool useDepthTexture = true;
    }

    public Settings settings = new Settings();

    UniStormSunShaftsPass sunShaftsPass;

    public override void Create()
    {
        sunShaftsPass = new UniStormSunShaftsPass(name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.isEnabled)
            return;

        //if (settings.sunShaftsShader == null)
        //{
        //    Debug.LogWarningFormat("Missing Sun Shaft shader. {0} Sun Shafts shader will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
        //    return;
        //}

        sunShaftsPass.settings = settings;
        sunShaftsPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(sunShaftsPass);
    }
}