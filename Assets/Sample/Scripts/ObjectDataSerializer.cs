using System;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

namespace ARKitCollaborator.Samples {

public enum ObjectType: Int16 {
    Cube,
    Sphere,
}

public class ObjectData {
    TrackableId m_parentAnchorId;
    ObjectType m_type;
    Vector3 m_position;
    Quaternion m_rotation;

    public TrackableId Id { get { return m_parentAnchorId; }}
    public ObjectType Type { get { return m_type; }}
    public Vector3 Position { get { return m_position; }}
    public Quaternion Rotation { get { return m_rotation; }}

    public ObjectData(TrackableId parentId, ObjectType typ, Vector3 position, Quaternion rotation){
        m_parentAnchorId = parentId;
        m_type = typ;
        m_position = position;
        m_rotation = rotation;
    }
}

//
// CAUTION!!
//    Allocator.Tempを使っているので、このオブジェクトの有効期間は１フレームのみ
// 
public class SerializedObjectData : IDisposable {
    NativeArray<byte> m_array;

    public NativeArray<byte> GetNativeArray(){
        return m_array;
    }

    internal SerializedObjectData(byte[] bytes){
        m_array = new NativeArray<byte>(bytes, Allocator.Temp);
    }

    public void Dispose(){
        m_array.Dispose();
    }
}

public static class ObjectDataSerializer {
    private static byte[] AppId = {(byte)'D', (byte)'E', (byte)'M', (byte)'O'};
    private static Int16 ProtocolMajorVersion = 1;
    private static Int16 ProtocolMinorVersion = 0;

    public static SerializedObjectData Serialize(TrackableId parentId, ObjectType typ, Vector3 position, Quaternion rotation){
        Debug.LogFormat("Serialize. trackableId: {0}", parentId);
        var bytes = MakeByteArray(parentId, typ, position, rotation);
        Debug.LogFormat("data size: {0}", bytes.Length);
        return new SerializedObjectData(bytes);
    }

    public static ObjectData TryDeserialize(NativeSlice<byte> slice){
        if(slice.Length != 56){
            // throw new Exception($"illegal data size {slice.Length}.");
            return null;
        }

        byte[] bytes = slice.ToArray();

        //
        // Check AppId
        //
        if(bytes[0] != AppId[0] ||
           bytes[1] != AppId[1] ||
           bytes[2] != AppId[2] ||
           bytes[3] != AppId[3])
        {
            Debug.LogFormat("unmatched AppId. {0},{1},{2},{3}", (char)bytes[0], (char)bytes[1], (char)bytes[2], (char)bytes[3]);
            return null;
        }

        //
        // Check Protocol Version
        //
        Debug.LogFormat("Protocol Major Version: {0}, Minor Version: {1}", BitConverter.ToInt16(bytes, 4), BitConverter.ToInt16(bytes, 6));

        //
        // Check Checksum
        //
        Debug.LogFormat("Checksum...  bytes[8]: {0}, bytes[9]: {1}", bytes[8], bytes[9]);
        byte checksum = bytes[9];
        byte calcedChecksum = CalcChecksum(bytes);

        if(checksum != calcedChecksum){
            Debug.LogFormat("unmatched checksum. {0} not equal {1}", checksum, calcedChecksum);
            return null;
        }

        ObjectType typ = (ObjectType)BitConverter.ToInt16(bytes, 10);

        ulong id1 = BitConverter.ToUInt64(bytes, 12);
        ulong id2 = BitConverter.ToUInt64(bytes, 12 + 8);

        float pos_x = BitConverter.ToSingle(bytes, 12 + 8 + 8);
        float pos_y = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4);
        float pos_z = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4 * 2);

        float rot_x = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4 * 3);
        float rot_y = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4 * 4);
        float rot_z = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4 * 5);
        float rot_w = BitConverter.ToSingle(bytes, 12 + 8 + 8 + 4 * 6);

        TrackableId id = new TrackableId(id1, id2);
        Vector3 position = new Vector3(pos_x, pos_y, pos_z);
        Quaternion rotation = new Quaternion(rot_x, rot_y, rot_z, rot_w);

        return new ObjectData(id, typ, position, rotation);
    }

    private static byte[] MakeByteArray(TrackableId parentId, ObjectType typ, Vector3 position, Quaternion rotation){
        byte[] b_majorVer   = BitConverter.GetBytes(ProtocolMajorVersion);
        byte[] b_minorVer   = BitConverter.GetBytes(ProtocolMinorVersion);

        byte[] b_subId1     = BitConverter.GetBytes(parentId.subId1);
        byte[] b_subId2     = BitConverter.GetBytes(parentId.subId2);

        byte[] b_type       = BitConverter.GetBytes((Int16)typ);
        byte[] b_position_x = BitConverter.GetBytes(position.x);
        byte[] b_position_y = BitConverter.GetBytes(position.y);
        byte[] b_position_z = BitConverter.GetBytes(position.z);

        byte[] b_rotation_x = BitConverter.GetBytes(rotation.x);
        byte[] b_rotation_y = BitConverter.GetBytes(rotation.y);
        byte[] b_rotation_z = BitConverter.GetBytes(rotation.z);
        byte[] b_rotation_w = BitConverter.GetBytes(rotation.w);

        byte[] bytes = new byte[AppId.Length +
                                b_majorVer.Length +
                                b_minorVer.Length +
                                2 +
                                b_subId1.Length +
                                b_subId2.Length +
                                b_type.Length +
                                b_position_x.Length +
                                b_position_y.Length +
                                b_position_z.Length +
                                b_rotation_x.Length +
                                b_rotation_y.Length +
                                b_rotation_z.Length +
                                b_rotation_w.Length];

        Debug.LogFormat("bytes.Length = {0}", bytes.Length);

        Array.Copy(AppId,        0, bytes, 0, 4);
        Array.Copy(b_majorVer,   0, bytes, 4, 2);
        Array.Copy(b_minorVer,   0, bytes, 6, 2);

        // a byte for nothing, a byte for checksum
        // bytes[8] = 0;
        // bytes[9] = CalcChecksum(bytes);

        Array.Copy(b_type,       0, bytes, 10, 2);
        Array.Copy(b_subId1,     0, bytes, 12, 8);
        Array.Copy(b_subId2,     0, bytes, 12 + 8, 8);
        Array.Copy(b_position_x, 0, bytes, 12 + 8 + 8, 4);
        Array.Copy(b_position_y, 0, bytes, 12 + 8 + 8 + 4, 4);
        Array.Copy(b_position_z, 0, bytes, 12 + 8 + 8 + 4 * 2, 4);
        Array.Copy(b_rotation_x, 0, bytes, 12 + 8 + 8 + 4 * 3, 4);
        Array.Copy(b_rotation_y, 0, bytes, 12 + 8 + 8 + 4 * 4, 4);
        Array.Copy(b_rotation_z, 0, bytes, 12 + 8 + 8 + 4 * 5, 4);
        Array.Copy(b_rotation_w, 0, bytes, 12 + 8 + 8 + 4 * 6, 4);

        bytes[8] = 0;
        bytes[9] = CalcChecksum(bytes);

        return bytes;
    }

    private static byte CalcChecksum(byte[] bytes){
        Debug.LogFormat("CalcChecksum. bytes.Length = {0}", bytes.Length);
        byte checksum = 0;

        for(int i = 10; i < bytes.Length; i++){
            checksum += bytes[i];
        }
        Debug.LogFormat(" calced checksum = {0}", checksum);

        return checksum;
    }
}

}