using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

using UnityEngine.XR.ARFoundation;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARKit;
#endif

namespace ARKitCollaborator
{
    [RequireComponent(typeof(ARAnchorManager))]
    [RequireComponent(typeof(ARRaycastManager))]
    public class AnchorCreator : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Prefab;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        float m_minDistance = 0.2f;

        public delegate void AnchorCreatedHandler(ARAnchor anchor, ARRaycastHit hit);
        public event AnchorCreatedHandler OnAnchorCreated;

        ARAnchor m_Anchor;

        public GameObject prefab
        {
            get => m_Prefab;
            set => m_Prefab = value;
        }

        public ARAnchor MainAnchor {
            get => m_Anchor;
        }

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
        }

        ARAnchor CreateAnchor(in ARRaycastHit hit)
        {
            ARAnchor anchor = null;

            // If we hit a plane, try to "attach" the anchor to the plane
            if (hit.trackable is ARPlane plane)
            {
                var planeManager = GetComponent<ARPlaneManager>();
                if (planeManager)
                {
                    Debug.Log("Creating anchor attachment.");
                    var oldPrefab = m_AnchorManager.anchorPrefab;
                    m_AnchorManager.anchorPrefab = prefab;
                    anchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
                    m_AnchorManager.anchorPrefab = oldPrefab;
                    if(OnAnchorCreated != null){
                        OnAnchorCreated(anchor, hit);
                    }
                    return anchor;
                }
            }

            // Otherwise, just create a regular anchor at the hit pose
            Debug.Log("Creating regular anchor.");

            // Note: the anchor can be anywhere in the scene hierarchy
            var gameObject = Instantiate(prefab, hit.pose.position, hit.pose.rotation);

            // Make sure the new GameObject has an ARAnchor component
            anchor = gameObject.GetComponent<ARAnchor>();
            if (anchor == null)
            {
                anchor = gameObject.AddComponent<ARAnchor>();
            }

            if(OnAnchorCreated != null){
                OnAnchorCreated(anchor, hit);
            }

            return anchor;
        }

        void Update()
        {
            if(m_Anchor != null) return;

            // Raycast against planes and feature points
            const TrackableType trackableTypes =
                TrackableType.FeaturePoint |
                TrackableType.PlaneWithinPolygon;

            // Perform the raycast
            var centerPosition = new Vector3(m_Camera.transform.position.x, m_Camera.transform.position.y, 0);
            if (m_RaycastManager.Raycast(centerPosition, s_Hits, trackableTypes))
            {
                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hit = s_Hits[0];

                Debug.LogFormat("hit.distance: {0}", hit.distance);
                if(hit.distance <= m_minDistance) return;  // 端末からの距離が近すぎるアンカーは生成しない。

                // Create a new anchor
                m_Anchor = CreateAnchor(hit);
            }
        }

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        ARRaycastManager m_RaycastManager;

        ARAnchorManager m_AnchorManager;
    }
}
