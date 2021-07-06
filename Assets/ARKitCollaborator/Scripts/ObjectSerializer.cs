using System;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

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

public static class ObjectSerializer {
    public static SerializedObjectData Serialize(TrackableId parentId, ObjectType typ, Vector3 position, Quaternion rotation){
        var bytes = MakeByteArray(parentId, typ, position, rotation);
        return new SerializedObjectData(bytes);
    }

    public static ObjectData Deserialize(NativeSlice<byte> slice){
        if(slice.Length != 48){
            throw new Exception("illegal data size.");
        }

        byte[] bytes = slice.ToArray();
        byte checksum = bytes[1];

        //
        // TODO: Check Checksum
        //
        byte calcedChecksum = CalcChecksum(bytes);

        if(checksum != calcedChecksum){
            Debug.LogFormat("unmatched checksum. {0} not equal {1}", checksum, calcedChecksum);
            return null;
        }

        ObjectType typ = (ObjectType)BitConverter.ToInt16(bytes, 2);

        ulong id1 = BitConverter.ToUInt64(bytes, 4);
        ulong id2 = BitConverter.ToUInt64(bytes, 4 + 8);

        float pos_x = BitConverter.ToSingle(bytes, 4 + 8 + 8);
        float pos_y = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4);
        float pos_z = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4 * 2);

        float rot_x = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4 * 3);
        float rot_y = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4 * 4);
        float rot_z = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4 * 5);
        float rot_w = BitConverter.ToSingle(bytes, 4 + 8 + 8 + 4 * 6);

        TrackableId id = new TrackableId(id1, id2);
        Vector3 position = new Vector3(pos_x, pos_y, pos_z);
        Quaternion rotation = new Quaternion(rot_x, rot_y, rot_z, rot_w);

        return new ObjectData(id, typ, position, rotation);
    }

    private static byte[] MakeByteArray(TrackableId parentId, ObjectType typ, Vector3 position, Quaternion rotation){
        byte checksum = 0;

        byte[] b_checksum   = BitConverter.GetBytes(checksum);

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

        byte[] bytes = new byte[1 +
                                b_checksum.Length +
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

        Debug.LogFormat("  checksum.Length = {0}", b_checksum.Length); // 
        Debug.LogFormat("  subId1.Length = {0}", b_subId1.Length);
        Debug.LogFormat("  subId2.Length = {0}", b_subId2.Length);
        Debug.LogFormat("  type.Length = {0}", b_type.Length);         // 
        Debug.LogFormat("  float Length = {0}", b_position_x.Length);  // 4
        Debug.LogFormat("bytes.Length = {0}", bytes.Length);       // 32

        Array.Copy(b_type,       0, bytes, 2, 2);
        Array.Copy(b_subId1,     0, bytes, 4, 8);
        Array.Copy(b_subId1,     0, bytes, 4 + 8, 8);
        Array.Copy(b_position_x, 0, bytes, 4 + 8 + 8, 4);
        Array.Copy(b_position_y, 0, bytes, 4 + 8 + 8 + 4, 4);
        Array.Copy(b_position_z, 0, bytes, 4 + 8 + 8 + 4 * 2, 4);
        Array.Copy(b_rotation_x, 0, bytes, 4 + 8 + 8 + 4 * 3, 4);
        Array.Copy(b_rotation_y, 0, bytes, 4 + 8 + 8 + 4 * 4, 4);
        Array.Copy(b_rotation_z, 0, bytes, 4 + 8 + 8 + 4 * 5, 4);
        Array.Copy(b_rotation_w, 0, bytes, 4 + 8 + 8 + 4 * 6, 4);

        bytes[0] = 0;
        bytes[1] = CalcChecksum(bytes);

        return bytes;
    }

    private static byte CalcChecksum(byte[] bytes){
        byte checksum = 0;

        for(int i = 2; i < bytes.Length; i++){
            checksum += bytes[i];
        }

        return checksum;
    }
}