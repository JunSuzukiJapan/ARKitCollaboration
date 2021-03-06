using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ARKitCollaborator.Samples
{
    public class CollaborationNetworkingIndicator : MonoBehaviour {
        [SerializeField]
        Image m_IncomingDataImage;

        [SerializeField]
        CollaborativeSession m_session;

        public Image incomingDataImage
        {
            get { return m_IncomingDataImage; }
            set { m_IncomingDataImage = value; }
        }

        [SerializeField]
        Image m_OutgoingDataImage;

        public Image outgoingDataImage
        {
            get { return m_OutgoingDataImage; }
            set { m_OutgoingDataImage = value; }
        }

        [SerializeField]
        Image m_HasCollaborationDataImage;

        public Image hasCollaborationDataImage
        {
            get { return m_HasCollaborationDataImage; }
            set { m_HasCollaborationDataImage = value; }
        }

        bool m_IncomingDataReceived;

        bool m_OutgoingDataSent;

        bool m_HasCollaborationData;

        void OnEnable(){
            if(m_session != null){
                m_session.OnHasCollaborationData += NotifyHasCollaborationData;
                m_session.OnOutgoingDataSent += NotifyOutgoingDataSent;
                m_session.OnIncomingDataReceived += NotifyIncomingDataReceived;
            }
        }

        void OnDisable(){
            if(m_session != null){
                m_session.OnHasCollaborationData -= NotifyHasCollaborationData;
                m_session.OnOutgoingDataSent -= NotifyOutgoingDataSent;
                m_session.OnIncomingDataReceived -= NotifyIncomingDataReceived;
            }
        }

        void Update() {
            m_IncomingDataImage.color = m_IncomingDataReceived ? Color.green : Color.red;
            m_OutgoingDataImage.color = m_OutgoingDataSent ? Color.green : Color.red;
            m_HasCollaborationDataImage.color = m_HasCollaborationData ? Color.green : Color.red;

            m_IncomingDataReceived = false;
            m_OutgoingDataSent = false;
            m_HasCollaborationData = false;
        }

        void NotifyIncomingDataReceived() {
            m_IncomingDataReceived = true;
        }

        void NotifyOutgoingDataSent() {
            m_OutgoingDataSent = true;
        }

        void NotifyHasCollaborationData() {
            m_HasCollaborationData = true;
        }
    }
}