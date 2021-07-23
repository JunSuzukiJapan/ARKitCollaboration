using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.ARFoundation;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.iOS.Multipeer;
using UnityEngine.XR.ARKit;
#endif

namespace ARKitCollaborator {

public interface IAnchorCreatedHandler {
    void AnchorCreatedHandler(ARAnchor anchor, ARRaycastHit hit);
}
 
}
