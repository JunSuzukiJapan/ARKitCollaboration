using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace ARKitCollaborator {

public class DeserializableObject : MonoBehaviour, ITryDeserializable {
    public virtual bool TryDeserialize(NativeSlice<byte> bytes){
        return false;
    }
}

}