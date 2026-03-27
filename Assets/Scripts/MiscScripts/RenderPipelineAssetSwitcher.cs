using UnityEngine;
using UnityEngine.Rendering;

public class RenderPipelineAssetSwitcher : MonoBehaviour
{
    public RenderPipelineAsset specificRenderPipelineAsset;

    void OnEnable()
    {
        //Switch to the scene-specific asset
        if (specificRenderPipelineAsset != null)
        {
            QualitySettings.renderPipeline = specificRenderPipelineAsset;
        }
    }
}
