namespace UnityEngine.Reflect.Viewer.Pipeline
{
    /// <summary>
    /// Interface for what to pass and how to register with the Notifier of Reflect Metadata requests
    /// </summary>
    /// <remarks>Send the observer, the parameter to search for, and a matching value if desired</remarks>
    public interface INotifyMetadataObservers
    {
        void Attach(IFilterMetadata observer, string parameter, string value);
        void Detach(IFilterMetadata observer, string parameter);
    }
}
