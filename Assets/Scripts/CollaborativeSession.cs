using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARKit;
#endif

namespace ARKitCollaborator {
    [RequireComponent(typeof(ARSession))]
    public class CollaborativeSession : MonoBehaviour {
        [SerializeField]
        [Tooltip("The name for this network service. It should be 15 characters or less and can contain ASCII, lowercase letters, numbers, and hyphens.")]
        string m_ServiceType;

        [SerializeField]
        GameObject m_deserializer;

        ITryDeserializable m_tryDeserializable;

        /// <summary>
        /// The name for this network service.
        /// See <a href="https://developer.apple.com/documentation/multipeerconnectivity/mcnearbyserviceadvertiser">MCNearbyServiceAdvertiser</a>
        /// for the purpose of and restrictions on this name.
        /// </summary>
        public string serviceType {
            get => m_ServiceType;
            set => m_ServiceType = value;
        }

        public delegate void HasCollaborationDataHandler();
        public delegate void OutgoingDataSentHandler();
        public delegate void IncomingDataReceivedHandler();

        public HasCollaborationDataHandler OnHasCollaborationData;
        public OutgoingDataSentHandler OnOutgoingDataSent;
        public IncomingDataReceivedHandler OnIncomingDataReceived;

        ARSession m_ARSession;

        void DisableNotSupported(string reason) {
            enabled = false;
            Debug.Log(reason);
        }

        void OnEnable() {
    #if UNITY_IOS && !UNITY_EDITOR
            var subsystem = GetSubsystem();
            if (!ARKitSessionSubsystem.supportsCollaboration || subsystem == null) {
                DisableNotSupported("Collaborative sessions require iOS 13.");
                return;
            }

            subsystem.collaborationRequested = true;
            m_MCSession.Enabled = true;
    #else
            DisableNotSupported("Collaborative sessions are an ARKit 3 feature; This platform does not support them.");
    #endif
        }

    #if UNITY_IOS && !UNITY_EDITOR
        MCSession m_MCSession;

        public MCSession Session { get {return m_MCSession; } }

        ARKitSessionSubsystem GetSubsystem() {
            if (m_ARSession == null)
                return null;

            return m_ARSession.subsystem as ARKitSessionSubsystem;
        }

        void Awake() {
            m_ARSession = GetComponent<ARSession>();
            m_MCSession = new MCSession(SystemInfo.deviceName, m_ServiceType);

            m_tryDeserializable = m_deserializer.GetComponent<ITryDeserializable>();
            Debug.LogFormat("m_tryDeserializable: {0}", m_tryDeserializable);
        }

        void OnDisable() {
            m_MCSession.Enabled = false;

            var subsystem = GetSubsystem();
            if (subsystem != null)
                subsystem.collaborationRequested = false;
        }

        void Update() {
            var subsystem = GetSubsystem();
            if (subsystem == null)
                return;

            // Check for new collaboration data
            while (subsystem.collaborationDataCount > 0) {
                using (var collaborationData = subsystem.DequeueCollaborationData()) {
                    if(OnHasCollaborationData != null) {
                        OnHasCollaborationData();
                    }

                    if (m_MCSession.ConnectedPeerCount == 0)
                        continue;

                    using (var serializedData = collaborationData.ToSerialized())
                    using (var data = NSData.CreateWithBytesNoCopy(serializedData.bytes)) {
                        m_MCSession.SendToAllPeers(data, collaborationData.priority == ARCollaborationDataPriority.Critical
                            ? MCSessionSendDataMode.Reliable
                            : MCSessionSendDataMode.Unreliable);

                        if(OnOutgoingDataSent != null) {
                            OnOutgoingDataSent();
                        }

                        // Only log 'critical' data as 'optional' data tends to come every frame
                        if (collaborationData.priority == ARCollaborationDataPriority.Critical) {
                            Debug.Log($"Sent {data.Length} bytes of collaboration data.");
                        }
                    }
                }
            }

            // Check for incoming data
            while (m_MCSession.ReceivedDataQueueSize > 0) {
                if(OnIncomingDataReceived != null){
                    OnIncomingDataReceived();
                }

                using (var data = m_MCSession.DequeueReceivedData()){
                    if(m_tryDeserializable == null || (! m_tryDeserializable.TryDeserialize(data.Bytes))){
                        using (var collaborationData = new ARCollaborationData(data.Bytes)) {
                            if (collaborationData.valid) {
                                subsystem.UpdateWithCollaborationData(collaborationData);
                                if (collaborationData.priority == ARCollaborationDataPriority.Critical) {
                                    Debug.Log($"Received {data.Bytes.Length} bytes of collaboration data.");
                                }
                            } else {
                                Debug.Log($"Received {data.Bytes.Length} bytes from remote, but the collaboration data was not valid.");
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy() {
            m_MCSession.Dispose();
        }
    #endif
    }
}
