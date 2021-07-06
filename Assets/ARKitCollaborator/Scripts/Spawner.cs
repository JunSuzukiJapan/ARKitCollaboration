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
                var localRot = anchor.transform.InverseTransformDirection(rot.eulerAngles);

                SerializedObjectData serializedObjectData = ObjectSerializer.Serialize(anchor.trackableId, ObjectType.Cube, localPos, Quaternion.Euler(localRot));
                Debug.LogFormat("serializedData: {0}", serializedObjectData);
                NativeArray<byte> ary = serializedObjectData.GetNativeArray();
                var data = NSData.CreateWithBytesNoCopy(ary);
                Debug.LogFormat("data: {0}", data);

                session.SendToAllPeers(data, MCSessionSendDataMode.Reliable);

                Debug.Log($"Sent {data.Length} bytes of collaboration data.");
            }
        }
#endif
    }

    public void SpawnReceivedData(NativeSlice<byte> bytes){
        Debug.LogFormat("received bytes length: {0}", bytes.Length);

        ObjectData data = ObjectSerializer.Deserialize(bytes);
        Debug.LogFormat("received data Parent: {0}", data.Id.ToString());
        Debug.LogFormat("received data Type: {0}, pos: {1}, rot: {2}", data.Type, data.Position, data.Rotation);

        switch(data.Type){
        case ObjectType.Cube:
        case ObjectType.Sphere:
            Debug.Log("Instantiate!!!");
            Instantiate(m_RemoteCubePrefab, data.Position, data.Rotation);
            break;
        }
    }
}
