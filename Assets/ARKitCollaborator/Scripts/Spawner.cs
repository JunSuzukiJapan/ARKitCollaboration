using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARFoundation.Samples;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARKit;
#endif

using ARKitCollaborator;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private GameObject m_camera;

    [SerializeField]
    private GameObject m_LocalCubePrefab;
    [SerializeField]
    private GameObject m_RemoteCubePrefab;

    [SerializeField]
    private GameObject m_spherePrefab;

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

            Debug.LogFormat("object pos: {0}, rot: {1}", pos, rot);
            Instantiate(m_LocalCubePrefab, pos, rot);

            //
            // Send to Peers
            //
            MCSession session = m_CollaborativeSession.Session;
            Debug.LogFormat("mcsession: {0}", session);
            if (session != null && session.ConnectedPeerCount > 0){
                // ARAnchor からの相対座標を求める。
                var localPos = anchor.transform.InverseTransformPoint(pos);
                var localRot = rot * Quaternion.Inverse(anchor.transform.rotation);

                SerializedObjectData serializedObjectData = ObjectSerializer.Serialize(anchor.trackableId, ObjectType.Cube, localPos, localRot);
                Debug.LogFormat("serializedData: {0}", serializedObjectData);
                NativeArray<byte> ary = serializedObjectData.GetNativeArray();
                var data = NSData.CreateWithBytesNoCopy(ary);
                Debug.LogFormat("data: {0}", data);

                session.SendToAllPeers(data, MCSessionSendDataMode.Reliable);

                Debug.Log($"Sent {data.Length} bytes of collaboration data.");
                Debug.LogFormat("  anchor pos: {0}, rot: {1}", anchor.transform.position, anchor.transform.rotation);
                Debug.LogFormat("sent. pos: {0}, rot: {1}, local pos: {2}, local rot: {3}", pos, rot, localPos, localRot);
            }
        }
#endif
    }

    public bool TrySpawnReceivedData(NativeSlice<byte> bytes){
        Debug.LogFormat("received bytes length: {0}", bytes.Length);

        ObjectData data = ObjectSerializer.TryDeserialize(bytes);
        if(data == null){
            Debug.LogFormat("Deserialized data is null.");
            return false;
        }
        Debug.LogFormat("received data Parent: {0}", data.Id.ToString());
        Debug.LogFormat("received data Type: {0}, pos: {1}, rot: {2}", data.Type, data.Position, data.Rotation);

        ARAnchor anchor = m_anchorManager.GetAnchor(data.Id);
        Debug.LogFormat("anchor: {0}", anchor);
        if(anchor != null){
            Debug.LogFormat("trackable id: {0} FOUND", anchor.trackableId);
            Debug.LogFormat("  anchor pos: {0}, rot: {1}", anchor.transform.position, anchor.transform.rotation);
        }else{
            Debug.LogFormat("no anchor. no id");
            foreach(var trackable in m_anchorManager.trackables){
                Debug.LogFormat(" - trackable id: {0}", trackable.trackableId);
            }
        }

        if(anchor == null) return true;

        var globalPos = anchor.transform.TransformPoint(data.Position);
        var globalRot = anchor.transform.rotation * data.Rotation;

        switch(data.Type){
        case ObjectType.Cube:
        case ObjectType.Sphere:
            Debug.LogFormat("Instantiate!!!   pos: {0}, rot: {1}, global pos: {2}, global rot: {3}", data.Position, data.Rotation, globalPos, globalRot);
            Instantiate(m_RemoteCubePrefab, globalPos, globalRot);
            break;
        }

        return true;
    }
}
