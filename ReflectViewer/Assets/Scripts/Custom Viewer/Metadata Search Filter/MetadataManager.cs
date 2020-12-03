using System.Collections.Generic;

namespace UnityEngine.Reflect.Viewer.Pipeline
{
    /// <summary>
    /// Manager for the metadata search filters. Notifies any search observers of matching results.
    /// </summary>
    public class MetadataManager : INotifyMetadataObservers
    {
        static MetadataManager instance;
        public static MetadataManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MetadataManager();
                return instance;
            }
            set => instance = value;
        }
        /// <summary>
        /// Default value to return any non-empty or non-null value for a parameter
        /// </summary>
        public readonly string AnyValue = "AnyNonNullValue";

        Dictionary<IFilterMetadata, Dictionary<string, string>> notifyFilterDictionary = new Dictionary<IFilterMetadata, Dictionary<string, string>>();
        /// <summary>
        /// Metadata filter lookup
        /// </summary> 
        public Dictionary<IFilterMetadata, Dictionary<string, string>> FilterLookup { get => notifyFilterDictionary; set => notifyFilterDictionary = value; }


        #region INotifyMetadataObservers implementation
        public void Attach(IFilterMetadata observer, string parameter, string value)
        {
            if (!string.IsNullOrEmpty(parameter) && !string.IsNullOrEmpty(value))
            {
                var newEntry = new Dictionary<string, string> { { parameter, value } };
                if (!notifyFilterDictionary.ContainsKey(observer))
                    notifyFilterDictionary.Add(observer, newEntry);
                else
                {
                    var existingEntry = notifyFilterDictionary[observer];
                    if (!existingEntry.ContainsKey(parameter))
                        notifyFilterDictionary[observer].Add(parameter, value);
                }
            }
            else
                Debug.LogWarning("Was not able to add Reflect Filter observer since the search parameters were empty or null.");
        }

        public void Detach(IFilterMetadata observer, string parameter)
        {
            if (notifyFilterDictionary.ContainsKey(observer) && !string.IsNullOrEmpty(parameter))
            {
                var entry = notifyFilterDictionary[observer];
                if (entry.ContainsKey(parameter))
                {
                    if (entry.Count == 1)
                        notifyFilterDictionary.Remove(observer);
                    else
                        notifyFilterDictionary[observer].Remove(parameter);
                }
            }
        }
        #endregion
    }
}
