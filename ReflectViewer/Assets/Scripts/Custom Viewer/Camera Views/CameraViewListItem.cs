using TMPro;
using Unity.TouchFramework;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace Unity.Reflect.Viewer.UI
{
    /// <summary>
    /// Camera view menu item controller
    /// </summary>
    public class CameraViewListItem : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField, Tooltip("Camera View List Item button")]
        Button m_ItemButton;
        [SerializeField, Tooltip("Camera View List Item text")]
        TextMeshProUGUI m_Text;
        [SerializeField, Tooltip("The text background image")]
        Image m_ItemBgImage;
#pragma warning restore CS0649

        string nameText;
        Transform location;
        /// <summary>
        /// Name of the camera location
        /// </summary>
        public string NameText => nameText;
        /// <summary>
        /// Location of the camera
        /// </summary>
        public Transform CameraLocation => location;
        /// <summary>
        /// Event to fire when the menu item is clicked
        /// </summary>
        public event Action<string, Transform, CameraViewListItem> listItemClicked;

        static Color itemSelectedColor { get; } = new Color32(61, 61, 61, 255);

        void Awake()
        {
            if (m_ItemButton != null)
                m_ItemButton.onClick.AddListener(OnItemButtonClicked);
        }

        /// <summary>
        /// Called from the CameraViewsUIController when a new menu item is created
        /// </summary>
        /// <param name="nameKey">Camera location name</param>
        /// <param name="camera_location">Camera location transform</param>
        public void InitItem(string nameKey, Transform camera_location)
        {
            m_Text.text = nameText = nameKey;
            location = camera_location;
        }

        /// <summary>
        /// Turn off label and camera highlight (i.e. item is not selected)
        /// </summary>
        public void TurnOff()
        {
            SetHighlight(false);
        }

        /// <summary>
        /// Turn on label and camera highlight (i.e. item is selected)
        /// </summary>
        public void TurnOn()
        {
            SetHighlight(true);
        }

        void SetHighlight(bool highlight)
        {
            m_Text.color = highlight ? UIConfig.propertyTextSelectedColor : UIConfig.propertyTextBaseColor;
            m_Text.fontStyle = highlight ? FontStyles.Bold : FontStyles.Normal;
            m_ItemBgImage.color = highlight ? itemSelectedColor : UIConfig.projectItemBaseColor;
        }

        // Menu item selected
        void OnItemButtonClicked()
        {
            listItemClicked?.Invoke(nameText, location, this);
        }
    }
}
