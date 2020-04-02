//code exclusion define, by liyingbo
//vr/xr related code
//#define VR_SUPPORT
//features unused
//#define UNUSED_FEATURE

using System;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    internal class DefaultRendererSetup : IRendererSetup
    {
        private DepthOnlyPass m_DepthOnlyPass;
        private MainLightShadowCasterPass m_MainLightShadowCasterPass;
        private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;
        private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
        private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
        private BeginXRRenderingPass m_BeginXrRenderingPass;
        private SetupLightweightConstanstPass m_SetupLightweightConstants;
        private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
        private OpaquePostProcessPass m_OpaquePostProcessPass;
        private DrawSkyboxPass m_DrawSkyboxPass;
        private CopyDepthPass m_CopyDepthPass;
        private CopyColorPass m_CopyColorPass;
        private CopyRenderedPass m_CopyRenderedPass;
        private CopyToRenderTexturePass m_CopyToRenderTexturePass;
        private RenderTransparentForwardPass m_RenderTransparentForwardPass;
        private TransparentPostProcessPass m_TransparentPostProcessPass;
        private FinalBlitPass m_FinalBlitPass;
        private EndXRRenderingPass m_EndXrRenderingPass;

#if UNITY_EDITOR
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif


        private RenderTargetHandle ColorAttachment;
        private RenderTargetHandle DepthAttachment;
        private RenderTargetHandle ColorCopyIntermediateHandle;
        private RenderTargetHandle DepthTexture;
        private RenderTargetHandle OpaqueColor;
        private RenderTargetHandle RenderedColor;
        private RenderTargetHandle MainLightShadowmap;
        private RenderTargetHandle AdditionalLightsShadowmap;
        private RenderTargetHandle ScreenSpaceShadowmap;

        private List<IBeforeRender> m_BeforeRenderPasses = new List<IBeforeRender>(10);

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init()
        {
            if (m_Initialized)
                return;

            m_DepthOnlyPass = new DepthOnlyPass();
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
            m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass();
            m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
            m_BeginXrRenderingPass = new BeginXRRenderingPass();
            m_SetupLightweightConstants = new SetupLightweightConstanstPass();
            m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
            m_OpaquePostProcessPass = new OpaquePostProcessPass();
            m_DrawSkyboxPass = new DrawSkyboxPass();
            m_CopyDepthPass = new CopyDepthPass();
            m_CopyColorPass = new CopyColorPass();
            m_CopyRenderedPass = new CopyRenderedPass();
            m_CopyToRenderTexturePass = new CopyToRenderTexturePass();
            m_RenderTransparentForwardPass = new RenderTransparentForwardPass();
            m_TransparentPostProcessPass = new TransparentPostProcessPass();
            m_FinalBlitPass = new FinalBlitPass();
            m_EndXrRenderingPass = new EndXRRenderingPass();

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass();
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            ColorAttachment.Init("_CameraColorTexture");
            DepthAttachment.Init("_CameraDepthAttachment");
            ColorCopyIntermediateHandle.Init("_ColorCopyIntermediate");
            DepthTexture.Init("_CameraDepthTexture");
            OpaqueColor.Init("_CameraOpaqueTexture");
            RenderedColor.Init("_CameraRenderedTexture");
            MainLightShadowmap.Init("_MainLightShadowmapTexture");
            AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");
            ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");

            m_Initialized = true;
        }

        public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            Camera camera = renderingData.cameraData.camera;
            camera.GetComponents(m_BeforeRenderPasses);

            renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            ClearFlag clearFlag = ScriptableRenderer.GetCameraClearFlag(renderingData.cameraData.camera);
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool requiresRenderToTexture = ScriptableRenderer.RequiresIntermediateColorTexture(ref renderingData.cameraData, baseDescriptor)
                                           || m_BeforeRenderPasses.Count != 0;

            RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
            RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;

            if (requiresRenderToTexture)
            {
                colorHandle = ColorAttachment;
                depthHandle = DepthAttachment;

                var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
                m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
                renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);
                //FIX camera stacking with RT by liyingbo. Copy current screen content to the newly created RT.  
                var hdrEnabled = renderingData.cameraData.isHdrEnabled;
                if (clearFlag == ClearFlag.Depth) {
                    m_CopyToRenderTexturePass.Setup(baseDescriptor, sampleCount, hdrEnabled, RenderTargetHandle.CameraTarget, ColorCopyIntermediateHandle, colorHandle);
                    renderer.EnqueuePass(m_CopyToRenderTexturePass);
                }
            }

#if UNUSED_FEATURE
            foreach (var pass in m_BeforeRenderPasses)
            {
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle, clearFlag));
            }
#endif

            bool mainLightShadows = false;
            if (renderingData.shadowData.supportsMainLightShadows)
            {
                mainLightShadows = m_MainLightShadowCasterPass.Setup(MainLightShadowmap, ref renderingData);
                if (mainLightShadows)
                    renderer.EnqueuePass(m_MainLightShadowCasterPass);
            }

            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(AdditionalLightsShadowmap, ref renderingData, renderer.maxVisibleAdditionalLights);
                if (additionalLightShadows)
                    renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);
            }

            bool resolveShadowsInScreenSpace = mainLightShadows && renderingData.shadowData.requiresScreenSpaceShadowResolve;
            bool requiresDepthPrepass = resolveShadowsInScreenSpace || renderingData.cameraData.isSceneViewCamera ||
                                        (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData) || renderingData.cameraData.isOffscreenRender));

#if VR_SUPPORT
            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;
#endif

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            if (requiresDepthPrepass)
            {
                m_DepthOnlyPass.Setup(baseDescriptor, DepthTexture, SampleCount.One);
                renderer.EnqueuePass(m_DepthOnlyPass);

#if UNUSED_FEATURE
                foreach (var pass in camera.GetComponents<IAfterDepthPrePass>())
                    renderer.EnqueuePass(pass.GetPassToEnqueue(m_DepthOnlyPass.descriptor, DepthTexture));
#endif
            }

            if (resolveShadowsInScreenSpace)
            {
                m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, ScreenSpaceShadowmap);
                renderer.EnqueuePass(m_ScreenSpaceShadowResolvePass);
            }

#if VR_SUPPORT
            if (renderingData.cameraData.isStereoEnabled)
                renderer.EnqueuePass(m_BeginXrRenderingPass);
#endif

            RendererConfiguration rendererConfiguration = ScriptableRenderer.GetRendererConfiguration(renderingData.lightData.additionalLightsCount);

            m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
            renderer.EnqueuePass(m_SetupLightweightConstants);

#if UNUSED_FEATURE
            // If a before all render pass executed we expect it to clear the color render target
            if (m_BeforeRenderPasses.Count != 0)
                clearFlag = ClearFlag.None;
#endif

            m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor, rendererConfiguration);
            renderer.EnqueuePass(m_RenderOpaqueForwardPass);

            //used by CharacterAddPass
            foreach (var pass in camera.GetComponents<IAfterOpaquePass>())
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessingContext))
            {
                m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_OpaquePostProcessPass);

#if UNUSED_FEATURE
                foreach (var pass in camera.GetComponents<IAfterOpaquePostProcess>())
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
#endif
            }

            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                m_DrawSkyboxPass.Setup(colorHandle, depthHandle);
                renderer.EnqueuePass(m_DrawSkyboxPass);
            }

#if UNUSED_FEATURE
            foreach (var pass in camera.GetComponents<IAfterSkyboxPass>())
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
#endif

            if (renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass)
            {
                m_CopyDepthPass.Setup(depthHandle, DepthTexture);
                renderer.EnqueuePass(m_CopyDepthPass);
            }

            if (renderingData.cameraData.requiresOpaqueTexture)
            {
                m_CopyColorPass.Setup(colorHandle, OpaqueColor);
                renderer.EnqueuePass(m_CopyColorPass);
            }

            m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, rendererConfiguration);
            renderer.EnqueuePass(m_RenderTransparentForwardPass);

            if (renderingData.cameraData.requiresRenderedTexture) {
                m_CopyRenderedPass.Setup(baseDescriptor, colorHandle, depthHandle, RenderedColor);
                renderer.EnqueuePass(m_CopyRenderedPass);

                foreach (var pass in camera.GetComponents<IAfterTransparentPass>())
                    renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
            }

            //copied to above, within rendered texture condition
#if UNUSED_FEATURE
            foreach (var pass in camera.GetComponents<IAfterTransparentPass>())
                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
#endif

            if (renderingData.cameraData.postProcessEnabled)
            {
                m_TransparentPostProcessPass.Setup(baseDescriptor, colorHandle, BuiltinRenderTextureType.CameraTarget);
                renderer.EnqueuePass(m_TransparentPostProcessPass);
            }
            else if (!renderingData.cameraData.isOffscreenRender && colorHandle != RenderTargetHandle.CameraTarget)
            {
                m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
                renderer.EnqueuePass(m_FinalBlitPass);
            }

#if UNUSED_FEATURE
            foreach (var pass in camera.GetComponents<IAfterRender>())
                renderer.EnqueuePass(pass.GetPassToEnqueue());
#endif

#if VR_SUPPORT
            if (renderingData.cameraData.isStereoEnabled)
            {
                renderer.EnqueuePass(m_EndXrRenderingPass);
            }
#endif

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_SceneViewDepthCopyPass.Setup(DepthTexture);
                renderer.EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = (int)cameraData.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
