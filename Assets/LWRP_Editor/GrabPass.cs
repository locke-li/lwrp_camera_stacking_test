using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace Babeltime.Public {
    public class GrabPass : MonoBehaviour, IAfterTransparentPass {
        public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle) {
            return new RenderGrabPass();
        }
    }

    public class RenderGrabPass : ScriptableRenderPass {
        public RenderGrabPass() {
            RegisterShaderPassName("GrabPass");
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData) {
            var filterSetting = new FilterRenderersSettings(true) {
                renderQueueRange = RenderQueueRange.transparent,
            };
            var cam = renderingData.cameraData.camera;
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSetting = CreateDrawRendererSettings(cam, sortFlags, RendererConfiguration.None, false);
            drawSetting.SetShaderPassName(0, new ShaderPassName("GrabPass"));
            //drawSetting.SetShaderPassName(1, new ShaderPassName("shadow map"));
            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSetting, filterSetting);
        }
    }
}