using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARKitCollaborator {
    public interface IDataNotifier {
        void NotifyIncomingDataReceived();
        void NotifyOutgoingDataSent();
        void NotifyHasCollaborationData();
    }
}