using UnityEngine;
using UnityEngine.Reflect.Pipeline;

namespace UnityEngine.Reflect.Viewer.Pipeline
{
    /// <summary>
    /// General metadata filter that uses existing Reflect pipeline asset and adds the metadata search filter node.
    /// Need an instance of this in the Reflect scene.
    /// </summary>
    public class MetadataSearchFilter : MonoBehaviour
    {
        [SerializeField, Tooltip("Exisitng Reflect pipeline to use.")]
        ReflectPipeline pipeline = default;
        PipelineAsset pipelineAsset;
        InstanceConverterNode instanceConverter;
        MetadataSearchFilterNode filterNode;
        MetadataGeneralSearchFilter metadataFilterProcessor;

        void OnEnable()
        {
            // Check for pipeline and asset
            if (pipeline == null)
            {
                Debug.LogErrorFormat("There is no existing pipeline to use. Please place one in the field on {0}", this);
                return;
            }
            pipelineAsset = pipeline.pipelineAsset;

            pipeline.beforeInitialize += AddFilterNode;
            pipeline.afterInitialize += ListenToProcessor;
        }

        void OnDisable()
        {
            pipeline.beforeInitialize -= AddFilterNode;
            pipeline.afterInitialize -= ListenToProcessor;
            metadataFilterProcessor = null;
        }

        void AddFilterNode()
        {
            Debug.Log("Before Initialized.");
            if (pipelineAsset == null)
                return;

            // Reset processor
            metadataFilterProcessor = null;
            // Get the nodes required for this filter
            pipelineAsset.TryGetNode(out instanceConverter);
            // If this is the first time the node has every been created
            if (!pipelineAsset.TryGetNode(out filterNode))
            {
                filterNode = pipelineAsset.CreateNode<MetadataSearchFilterNode>();
                // Connect the filter node
                pipelineAsset.CreateConnection(instanceConverter.output, filterNode.gameObjectInput);
            }
        }

        void ListenToProcessor()
        {
            metadataFilterProcessor = filterNode.processor;
        }
    }
}
