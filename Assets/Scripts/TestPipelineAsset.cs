using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class TestPipelineAsset : RenderPipelineAsset
{
#if UNITY_EDITOR
    [MenuItem("Graphics/Create Test Pipeline")]
    private static void CreateBasicAssetPipeline()
    {
        var instance = CreateInstance<TestPipelineAsset>();
        AssetDatabase.CreateAsset(instance, "Assets/TestPipelineAsset.asset");
    }
#endif

    protected override RenderPipeline CreatePipeline()
    {
        return new TestPipeline();
    }
}