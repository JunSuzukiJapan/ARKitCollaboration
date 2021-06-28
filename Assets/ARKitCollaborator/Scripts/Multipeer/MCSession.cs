using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.iOS.Multipeer
{
    public enum MCSessionState: int {
        NotConnected,
        Connecting,
        Connected,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MCSession : IDisposable, IEquatable<MCSession>
    // public class MCSession : IDisposable, IEquatable<MCSession>
    {
        IntPtr m_Ptr;
        private static GCHandle m_GCHandleForOnChangePeerState;

        public bool Created => m_Ptr != IntPtr.Zero;

        public delegate void DidReceiveDataHandler();
        public delegate void DidChangePeerStateHandler(MCSessionState state);
        public delegate void DidReceiveInvitationFromPeerHandler();
        public delegate void FoundPeerHandler();
        public delegate void LostPeerHandler();
        public delegate void DidStartReceivingResourceWithNameHandler();
        public delegate void DidFinishReceivingResourceWithNameHandler();
        public delegate void DidReceiveStreamHandler();

        public delegate void DidChangePeerStateHandlerCaller(MCSessionState state);
        // [DllImport("__Internal")]
        // private static extern void _Call_DidChangePeerStateHandlerCaller (MCSessionState state, IntPtr methodHandle, DidChangePeerStateHandlerCaller caller);

        [AOT.MonoPInvokeCallbackAttribute(typeof(DidChangePeerStateHandlerCaller))]
        public static void S_DidChangePeerState(MCSessionState state){
            Debug.Log("static DidChangePeerState");
            if( ! m_GCHandleForOnChangePeerState.IsAllocated) return;

            // methodHandleからコールバック関数を取り出す。
            GCHandle handle = (GCHandle)m_GCHandleForOnChangePeerState;
            DidChangePeerStateHandler callback = handle.Target as DidChangePeerStateHandler;
            // // 不要になったハンドルを解放する。
            // handle.Free();

            Debug.Log("  call callback in DidChangePeerState");
            callback(state);
            Debug.Log("  end call callback in DidChangePeerState");
        }

        public bool Enabled
        {
            get
            {
                if (!Created)
                    return false;
                return GetEnabled(this);
            }

            set
            {
                if (!Created && value)
                    throw new InvalidOperationException();
                SetEnabled(this, value);
            }
        }

        public MCSession(string peerName, string serviceType, DidChangePeerStateHandler peerStateHandler)
        {
            if (peerName == null)
                throw new ArgumentNullException(nameof(peerName));

            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            using (var peerName_NSString = new NSString(peerName))
            using (var serviceType_NSString = new NSString(serviceType))
            {
                if(m_GCHandleForOnChangePeerState.IsAllocated){
                    m_GCHandleForOnChangePeerState.Free();
                }
                // コールバック関数をGCされないようにAllocしてハンドルを取得する。
                m_GCHandleForOnChangePeerState = GCHandle.Alloc(peerStateHandler, GCHandleType.Normal);

                // Debug.LogFormat("S_DidChangePeerState: {0}", S_DidChangePeerState);
                m_Ptr = InitWithName(peerName_NSString, serviceType_NSString, S_DidChangePeerState);
            }
        }

        public void Dispose(){
            Debug.LogFormat("MCSession disposed.");
            if(m_GCHandleForOnChangePeerState.IsAllocated){
                m_GCHandleForOnChangePeerState.Free();
            }
            NativeApi.CFRelease(ref m_Ptr);
        }

        public void SendToAllPeers(NSData data, MCSessionSendDataMode mode)
        {
            if (!Created)
                throw new InvalidOperationException($"The {typeof(MCSession).Name} has not been created.");

            if (!data.Created)
                throw new ArgumentException($"'{nameof(data)}' is not valid.", nameof(data));

            using (var error = SendToAllPeers(this, data, mode))
            {
                if (error.Valid)
                    throw error.ToException();
            }
        }

        public int ReceivedDataQueueSize => GetReceivedDataQueueSize(this);

        public NSData DequeueReceivedData()
        {
            if (!Created)
                throw new InvalidOperationException($"The {typeof(MCSession).Name} has not been created.");

            return DequeueReceivedData(this);
        }

        public int ConnectedPeerCount => GetConnectedPeerCount(this);

        public override int GetHashCode() => m_Ptr.GetHashCode();
        public override bool Equals(object obj) => (obj is MCSession) && Equals((MCSession)obj);
        public bool Equals(MCSession other) => m_Ptr == other.m_Ptr;
        public static bool operator==(MCSession lhs, MCSession rhs) => lhs.Equals(rhs);
        public static bool operator!=(MCSession lhs, MCSession rhs) => !lhs.Equals(rhs);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_sendToAllPeers")]
        static extern NSError SendToAllPeers(MCSession self, NSData data, MCSessionSendDataMode mode);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_initWithName")]
        static extern IntPtr InitWithName(NSString name, NSString serviceType,
                                          DidChangePeerStateHandlerCaller didChangePeerStateHandlerCaller
        );

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_receivedDataQueueSize")]
        static extern int GetReceivedDataQueueSize(MCSession self);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_dequeueReceivedData")]
        static extern NSData DequeueReceivedData(MCSession self);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_connectedPeerCount")]
        static extern int GetConnectedPeerCount(MCSession self);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_setEnabled")]
        static extern void SetEnabled(MCSession self, bool enabled);

        [DllImport("__Internal", EntryPoint="UnityMC_Delegate_getEnabled")]
        static extern bool GetEnabled(MCSession self);
    }
}
