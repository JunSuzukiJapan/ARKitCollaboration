using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARKitCollaborator;
using UnityEngine.XR.ARFoundation;

using Unity.Collections;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARKit;
#endif

namespace ARKitCollaborator.Samples {

public class Spawner : MonoBehaviour, IAnchorCreatedHandler, ITryDeserializable {
    [SerializeField]
    private GameObject m_camera;

    [SerializeField]
    private GameObject m_LocalCubePrefab;
    [SerializeField]
    private GameObject m_RemoteCubePrefab;

    [SerializeField]
    private CollaborativeSession m_CollaborativeSession;

    [SerializeField]
    private ARAnchorManager m_anchorManager;

    [SerializeField]
    private AnchorCreator m_anchorCreator;

    public void SpawnCube(){
#if UNITY_IOS && !UNITY_EDITOR
        Debug.Log("Spawn object!!!");
        if(m_LocalCubePrefab != null && m_camera != null && m_anchorCreator != null && m_anchorCreator.MainAnchor != null){
            Vector3 pos = m_camera.transform.position;
            Quaternion rot = m_camera.transform.rotation;
            ARAnchor anchor = m_anchorCreator.MainAnchor;

            Instantiate(m_LocalCubePrefab, pos, rot);

            //
            // Send to Peers
            //
            MCSession session = m_CollaborativeSession.Session;
            if (session != null && session.ConnectedPeerCount > 0){
                // ARAnchor からの相対座標を求める。
                var localPos = anchor.transform.InverseTransformPoint(pos);
                var localRot = rot * Quaternion.Inverse(anchor.transform.rotation);

                SerializedObjectData serializedObjectData = ObjectDataSerializer.Serialize(anchor.trackableId, ObjectType.Cube, localPos, localRot);
                NativeArray<byte> ary = serializedObjectData.GetNativeArray();
                var data = NSData.CreateWithBytesNoCopy(ary);

                session.SendToAllPeers(data, MCSessionSendDataMode.Reliable);
            }
        }
#endif
    }

    public bool TryDeserialize(NativeSlice<byte> bytes){
        ObjectData data = ObjectDataSerializer.TryDeserialize(bytes);
        if(data == null){
            return false;
        }

        ARAnchor anchor = m_anchorManager.GetAnchor(data.Id);
        if(anchor == null) return true;

        // ARAnchorからの相対座標をワールド座標に変換する。
        var globalPos = anchor.transform.TransformPoint(data.Position);
        var globalRot = anchor.transform.rotation * data.Rotation;

        switch(data.Type){
        case ObjectType.Cube:
            Instantiate(m_RemoteCubePrefab, globalPos, globalRot);
            break;
        }

        return true;
    }

    void OnEnable(){
        if(m_anchorCreator != null){
            m_anchorCreator.OnAnchorCreated += AnchorCreatedHandler;
        }
    }

    void OnDisable(){
        if(m_anchorCreator != null){
            m_anchorCreator.OnAnchorCreated -= AnchorCreatedHandler;
        }
    }

    void SetAnchorText(ARAnchor anchor, string text){
        var canvasTextManager = anchor.GetComponent<CanvasTextManager>();
        if (canvasTextManager)
        {
            canvasTextManager.text = text;
            canvasTextManager.textID = anchor.trackableId.ToString();
        }
    }

    public void AnchorCreatedHandler(ARAnchor anchor, ARRaycastHit hit){
        if(hit.trackable is ARPlane plane){
            SetAnchorText(anchor, $"Attached to plane {plane.trackableId}");
        }else{
            SetAnchorText(anchor, $"Anchor (from {hit.hitType})");
        }
    }
}

}