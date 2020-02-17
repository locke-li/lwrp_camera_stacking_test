using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline {
    public class CopyToRenderTexturePass : ScriptableRenderPass {
        const string k_Tag = "Copy To RenderTexture";

        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }
        private RenderTargetHandle intermediate { get; set; }
        private RenderTextureDescriptor descriptor;
        private SampleCount sampleCount;
        private bool hdrEnabled;

        private int UVTransformID;

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public CopyToRenderTexturePass() {
            UVTransformID = Shader.PropertyToID("_UVTransform");
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTextureDescriptor baseDescriptor, SampleCount sampleCount, bool hdrEnabled, RenderTargetHandle source, RenderTargetHandle intermediate, RenderTargetHandle destination) {
            this.source = source;
            this.destination = destination;
            this.intermediate = intermediate;
            descriptor = baseDescriptor;
            this.sampleCount = sampleCount;
            this.hdrEnabled = hdrEnabled;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData) {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            var cmd = CommandBufferPool.Get(k_Tag);

            var colorRT = source.Identifier();
            var opaqueColorRT = destination.Identifier();

            var inter = intermediate.Identifier();
            if (SystemInfo.graphicsUVStartsAtTop && false) {
                var colorDescriptor = descriptor;
                colorDescriptor.depthBufferBits = 0;
                colorDescriptor.msaaSamples = (int)sampleCount;
                cmd.GetTemporaryRT(intermediate.id, colorDescriptor, FilterMode.Point);
                cmd.SetGlobalVector(UVTransformID, new Vector4(1.0f, -1.0f, 0.0f, 1.0f));
                var mat = renderer.GetMaterial(MaterialHandle.BlitFlip);
                cmd.Blit(colorRT, inter);
                cmd.SetGlobalTexture("_MainTex", inter);
                cmd.Blit(inter, opaqueColorRT, mat);

                if (Status.Valid) {
                    Status.CodePath = "Flip";
                    cmd.Blit(colorRT, Status.Blit0.texture);
                    cmd.Blit(inter, Status.Blit1.texture);
                    cmd.Blit(opaqueColorRT, Status.Blit2.texture);
                }
            }
            else {
                if (sampleCount == SampleCount.One && !hdrEnabled) {
                    cmd.Blit(colorRT, opaqueColorRT);

                    if (Status.Valid) {
                        Status.CodePath = "Direct";
                        cmd.Blit(colorRT, Status.Blit0.texture);
                        cmd.Blit(opaqueColorRT, Status.Blit2.texture);
                    }
                }
                else {
                    var colorDescriptor = descriptor;
                    colorDescriptor.depthBufferBits = 0;
                    colorDescriptor.msaaSamples = 1;
                    cmd.GetTemporaryRT(intermediate.id, colorDescriptor, FilterMode.Point);
                    cmd.Blit(colorRT, inter);
                    cmd.SetGlobalTexture("_MainTex", inter);
                    cmd.Blit(inter, opaqueColorRT);

                    if (Status.Valid) {
                        Status.CodePath = "Intermediate";
                        cmd.Blit(colorRT, Status.Blit0.texture);
                        cmd.Blit(inter, Status.Blit1.texture);
                        cmd.Blit(opaqueColorRT, Status.Blit2.texture);
                    }
                }
            }
            cmd.ReleaseTemporaryRT(intermediate.id);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd) {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            destination = RenderTargetHandle.CameraTarget;
        }
    }
}
