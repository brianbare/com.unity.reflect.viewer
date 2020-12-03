using UnityEngine.Reflect.Pipeline;

namespace UnityEngine.Reflect.Viewer.Pipeline
{
    /// <summary>
    /// General interface for which to use for various metadata observers.
    /// Receives a matching GameObject and metadata string.
    /// </summary>
    /// <remarks>Any Controller can implement this interface and then Attach and Detach to the Metadata Manager to receive search results.
    /// Attach with this observer (the controller implementing), the parameter to search for, and the matching result.</remarks>
    public interface IFilterMetadata
    {
        void NotifyBeforeSearch();
        void NotifyObservers(GameObject reflectObject, StreamEvent updateType, string searchParameter, string result = null);
        void NotifyAfterSearch();
    }
}
