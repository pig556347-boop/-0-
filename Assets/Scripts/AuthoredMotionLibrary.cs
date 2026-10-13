using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Baked model-space deltas avoid Legacy FBX unit/axis curves changing a skinned rig's scale.
public sealed class AuthoredMotionLibrary
{
    public sealed class Frame { public Vector3 HipDelta; public Quaternion[] Rotations; }
    public sealed class Clip
    {
        public string Name; public Frame[] Frames; public int FPS;
        public float Length => (Frames.Length-1)/(float)FPS;
    }
    public string[] BoneNames { get; private set; }
    public readonly Dictionary<string,Clip> Clips=new Dictionary<string,Clip>(StringComparer.Ordinal);
    private static string ReadString(BinaryReader reader)
    {
        int length=reader.ReadInt32();
        if(length<1||length>256)throw new InvalidDataException("Invalid motion name length");
        byte[] data=reader.ReadBytes(length);
        if(data.Length!=length)throw new EndOfStreamException();
        return Encoding.UTF8.GetString(data);
    }
    private static float Number(BinaryReader reader)
    {
        float value=reader.ReadSingle();
        if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidDataException("Non-finite motion value");
        return value;
    }
    private static AuthoredMotionLibrary cached;
    public static AuthoredMotionLibrary Load()
    {
        if(cached!=null)return cached;
        TextAsset asset=Resources.Load<TextAsset>("Motions/PlayerPoseData");
        if(asset==null)throw new InvalidDataException("Baked player pose data missing");
        cached=Read(asset.bytes);return cached;
    }
    public static AuthoredMotionLibrary Read(byte[] bytes)
    {
        var result=new AuthoredMotionLibrary();
        using(var stream=new MemoryStream(bytes,false))
        using(var reader=new BinaryReader(stream,Encoding.UTF8))
        {
            if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="KMP1")throw new InvalidDataException("Motion format mismatch");
            int fps=reader.ReadInt32(),bones=reader.ReadInt32();
            if(fps!=60||bones!=19)throw new InvalidDataException("Motion rig/FPS mismatch");
            result.BoneNames=new string[bones];
            for(int i=0;i<bones;i++)result.BoneNames[i]=ReadString(reader);
            int count=reader.ReadInt32();
            if(count!=20)throw new InvalidDataException("Expected twenty supplied motion candidates");
            for(int clipIndex=0;clipIndex<count;clipIndex++)
            {
                string name=ReadString(reader);int frames=reader.ReadInt32();
                if(frames<2||frames>4096)throw new InvalidDataException("Invalid motion frame count");
                var clip=new Clip {Name=name,FPS=fps,Frames=new Frame[frames]};
                for(int i=0;i<frames;i++)
                {
                    var frame=new Frame {HipDelta=new Vector3(Number(reader),Number(reader),Number(reader)),Rotations=new Quaternion[bones]};
                    for(int j=0;j<bones;j++)
                    {
                        Quaternion rotation=new Quaternion(Number(reader),Number(reader),Number(reader),Number(reader));
                        float magnitude=rotation.x*rotation.x+rotation.y*rotation.y+rotation.z*rotation.z+rotation.w*rotation.w;
                        if(magnitude<.9f||magnitude>1.1f)throw new InvalidDataException("Invalid motion quaternion");
                        frame.Rotations[j]=rotation.normalized;
                    }
                    clip.Frames[i]=frame;
                }
                result.Clips.Add(name,clip);
            }
            if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing motion data");
        }
        return result;
    }
}
