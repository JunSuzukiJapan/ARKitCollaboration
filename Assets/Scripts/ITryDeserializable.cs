using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace ARKitCollaborator {

public interface ITryDeserializable {
    bool TryDeserialize(NativeSlice<byte> bytes);
}

}