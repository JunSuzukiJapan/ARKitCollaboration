using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARAnchorManager))]
    [RequireComponent(typeof(ARRaycastManager))]
    public class AnchorCreator : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Prefab;

        [SerializeField]
        Camera m_Camera;

        ARAnchor m_Anchor;

        public GameObject prefab
        {
            get => m_Prefab;
            set => m_Prefab = value;
        }

        public ARAnchor MainAnchor {
            get => m_Anchor;
        }

        // public void RemoveAllAnchors()
        // {
        //     Logger.Log($"Removing all anchors ({m_Anchors.Count})");
        //     foreach (var anchor in m_Anchors)
        //     {
        //         Destroy(anchor.gameObject);
        //     }
        //     m_Anchors.Clear();
        // }

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
        }

        void SetAnchorText(ARAnchor anchor, string text)
        {
            var canvasTextManager = anchor.GetComponent<CanvasTextManager>();
            if (canvasTextManager)
            {
                canvasTextManager.text = text;
                canvasTextManager.textID = anchor.trackableId.ToString();
            }
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
                    Logger.Log("Creating anchor attachment.");
                    var oldPrefab = m_AnchorManager.anchorPrefab;
                    m_AnchorManager.anchorPrefab = prefab;
                    anchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
                    m_AnchorManager.anchorPrefab = oldPrefab;
                    SetAnchorText(anchor, $"Attached to plane {plane.trackableId}");
                    return anchor;
                }
            }

            // Otherwise, just create a regular anchor at the hit pose
            Logger.Log("Creating regular anchor.");

            // Note: the anchor can be anywhere in the scene hierarchy
            var gameObject = Instantiate(prefab, hit.pose.position, hit.pose.rotation);

            // Make sure the new GameObject has an ARAnchor component
            anchor = gameObject.GetComponent<ARAnchor>();
            if (anchor == null)
            {
                anchor = gameObject.AddComponent<ARAnchor>();
            }

            SetAnchorText(anchor, $"Anchor (from {hit.hitType})");

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

                // Create a new anchor
                m_Anchor = CreateAnchor(hit);
                if (m_Anchor)
                {
                    // Remember the anchor so we can remove it later.
                    m_Anchors.Add(m_Anchor);
                }
            }
        }

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        List<ARAnchor> m_Anchors = new List<ARAnchor>();

        ARRaycastManager m_RaycastManager;

        ARAnchorManager m_AnchorManager;
    }
}
