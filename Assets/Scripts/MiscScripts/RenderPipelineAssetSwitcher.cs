using UnityEngine;
using UnityEngine.Rendering;

public class RenderPipelineAssetSwitcher : MonoBehaviour
{
    public RenderPipelineAsset specificRenderPipelineAsset;

    void Awake()
    {
        //Switch to the scene-specific asset
        if(QualitySettings.renderPipeline != specificRenderPipelineAsset)
        {
            QualitySettings.renderPipeline = specificRenderPipelineAsset;
        }
    }
}
