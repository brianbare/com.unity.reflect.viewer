using System;
using System.Collections.Generic;
using UnityEngine.Reflect.Pipeline;

namespace UnityEngine.Reflect.Viewer.Pipeline
{
    /// <summary>
    /// General metadata search filter node
    /// </summary>
    [SerializeField]
    public class MetadataSearchFilterNode : ReflectNode<MetadataGeneralSearchFilter>
    {
        public GameObjectInput gameObjectInput = new GameObjectInput();

        protected override MetadataGeneralSearchFilter Create(ReflectBootstrapper hook, ISyncModelProvider provider, IExposedPropertyTable resolver)
        {
            var filter = new MetadataGeneralSearchFilter();

            gameObjectInput.streamEvent = filter.OnGameObjectEvent;
            gameObjectInput.streamEnd = filter.OnGameObjectStreamEnd;
            gameObjectInput.streamBegin = filter.OnGameObjectStreamBegin;
            return filter;
        }
    }

    /// <summary>
    /// Processor for the general metadata search filter
    /// </summary>
    public class MetadataGeneralSearchFilter : IReflectNodeProcessor
    {
        string thisRootParameter;

        public void OnPipelineInitialized()
        {
            // OnPipelineInitialized is called the first time the pipeline is run.
        }

        public void OnGameObjectStreamBegin()
        {
            foreach (KeyValuePair<IFilterMetadata, Dictionary<string, string>> kvp in MetadataManager.Instance.FilterLookup)
            {
                // Notify each observer just once search is beginning
                kvp.Key.NotifyBeforeSearch();
            }
        }

        public void OnGameObjectEvent(SyncedData<GameObject> gameObjectData, StreamEvent streamEvent)
        {
            Metadata metadata = gameObjectData.data.GetComponent<Metadata>();
            foreach (KeyValuePair<IFilterMetadata, Dictionary<string, string>> _kvp in MetadataManager.Instance.FilterLookup)
            {
                foreach (KeyValuePair<string, string> kvp in _kvp.Value)
                {
                    if (metadata != null)
                    {
                        // If the listener is looking for any value including empty or null parameters and the Metadata is empty(e.g. curtain walls)
                        if (metadata.GetParameters().Count == 0)
                        {
                            if (kvp.Value == MetadataManager.Instance.AnyValue)
                            {
                                _kvp.Key.NotifyObservers(gameObjectData.data, streamEvent, kvp.Key);
                            }
                            continue;
                        }

                        thisRootParameter = metadata.GetParameter(kvp.Key);
                        if (!string.IsNullOrEmpty(thisRootParameter))
                        {
                            if (kvp.Value == MetadataManager.Instance.AnyValue)
                            {
                                _kvp.Key.NotifyObservers(gameObjectData.data, streamEvent, kvp.Key, thisRootParameter);
                            }
                            else if (thisRootParameter == kvp.Value)
                            {
                                _kvp.Key.NotifyObservers(gameObjectData.data, streamEvent, kvp.Key, thisRootParameter);
                            }
                        }
                    }
                    // If the listener is looking for any value including empty or null parameters
                    else if (kvp.Value == MetadataManager.Instance.AnyValue)
                    {
                        _kvp.Key.NotifyObservers(gameObjectData.data, streamEvent, kvp.Key);
                    }
                }
            }
        }

        public void OnGameObjectStreamEnd()
        {
            foreach (KeyValuePair<IFilterMetadata, Dictionary<string, string>> kvp in MetadataManager.Instance.FilterLookup)
            {
                // Notify each observer just once search is complete
                kvp.Key.NotifyAfterSearch();
            }
        }

        public void OnPipelineShutdown()
        {
            // OnPipelineShutdown is called before the pipeline graph is destroyed.
        }
    }
}
