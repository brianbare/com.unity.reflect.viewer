using System;

namespace Unity.Reflect.Viewer.UI
{
    [Serializable]
    public struct CameraViewsData : IEquatable<CameraViewsData>
    {
        /// <summary>
        /// Distance to move the camera up or down from the center (in cm)
        /// </summary>
        public int heightAdjustment;
        /// <summary>
        /// The camera to move to designated location
        /// </summary>
        public UnityEngine.Camera cameraToMove;

        public bool Equals(CameraViewsData other)
        {
            return heightAdjustment == other.heightAdjustment && cameraToMove == other.cameraToMove;
        }

        public override bool Equals(object obj)
        {
            return obj is CameraViewsData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (heightAdjustment.GetHashCode() * 397) ^ cameraToMove.GetHashCode(); ;
        }

        public static bool operator ==(CameraViewsData a, CameraViewsData b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(CameraViewsData a, CameraViewsData b)
        {
            return !(a == b);
        }
    }
}
