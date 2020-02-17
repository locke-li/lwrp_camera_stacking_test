using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace Babeltime.Public {
    public class CharacterAddPass : MonoBehaviour, IAfterOpaquePass {
        public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle) {
            return new RenderCharacterAddPass();
        }
    }

    public class RenderCharacterAddPass : ScriptableRenderPass {
        public RenderCharacterAddPass() {
            RegisterShaderPassName("outline");
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData) {
            var filterSetting = new FilterRenderersSettings(true) {
                renderQueueRange = RenderQueueRange.opaque,
            };
            var cam = renderingData.cameraData.camera;
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSetting = CreateDrawRendererSettings(cam, sortFlags, RendererConfiguration.None, false);
            drawSetting.SetShaderPassName(0, new ShaderPassName("outline"));
            drawSetting.SetShaderPassName(1, new ShaderPassName("shadow map"));
            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSetting, filterSetting);
        }
    }
}