using UnityEngine;
using UnityEngine.Rendering;

    public class TestPipeline : RenderPipeline
    {
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            var cmd = new CommandBuffer();
            cmd.ClearRenderTarget(true, true, Color.blue);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
            
            foreach (Camera camera in cameras)
            {
                if (!camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
                    continue;
                context.Cull(ref parameters);
            }
            
            context.Submit();
        }
    }
