using System;
using System.Collections.Generic;
using UnityEngine;

// A gameplay timeline and the matching authored contact frames. No root teleportation.
public sealed class BossMove
{
    public readonly string Clip, Warning;
    public readonly float[] StrikeTimes, NativeContacts;
    public readonly float Duration, DamageRatio, Reach, Knockback, NativeStart, NativeEnd;
    public readonly bool Punch, Armor;
    public float Windup => StrikeTimes[0];
    public BossMove(string clip,string warning,float windup,float recovery,float damage,float reach,float knockback,
        bool armor,int[] contacts,int endFrame,float[] gaps=null,int startFrame=0)
    {
        if(contacts==null||contacts.Length==0||windup<.2f||recovery<.15f)throw new ArgumentException("Invalid boss move timeline");
        Clip=clip;Warning=warning;DamageRatio=damage;Reach=reach;Knockback=knockback;Armor=armor;
        Punch=clip.StartsWith("Punch")||clip=="HookPunch"||clip=="QuadPunch";
        StrikeTimes=new float[contacts.Length];NativeContacts=new float[contacts.Length];
        NativeStart=startFrame/60f;NativeEnd=endFrame/60f;
        for(int i=0;i<contacts.Length;i++)
        {
            if(contacts[i]<=startFrame||contacts[i]>=endFrame||i>0&&contacts[i]<=contacts[i-1])throw new ArgumentException("Invalid native boss contacts");
            NativeContacts[i]=contacts[i]/60f;
            StrikeTimes[i]=i==0?windup:StrikeTimes[i-1]+gaps[i-1];
        }
        Duration=StrikeTimes[StrikeTimes.Length-1]+recovery;
    }
    public float SampleTime(float elapsed)
    {
        float previousGame=0,previousNative=NativeStart;
        for(int i=0;i<StrikeTimes.Length;i++)
        {
            if(elapsed<=StrikeTimes[i])return Mathf.Lerp(previousNative,NativeContacts[i],Mathf.InverseLerp(previousGame,StrikeTimes[i],elapsed));
            previousGame=StrikeTimes[i];previousNative=NativeContacts[i];
        }
        return Mathf.Lerp(previousNative,NativeEnd,Mathf.InverseLerp(previousGame,Duration,elapsed));
    }
    public bool ArmoredAt(float elapsed)=>Armor&&elapsed>=Windup*.65f&&elapsed<=StrikeTimes[StrikeTimes.Length-1]+.10f;
}

public static class DojaehakMoves
{
    [Serializable] private sealed class Row { public string clip;public int frames,impactFrame;public int[] contactFrames; }
    [Serializable] private sealed class Info { public Row[] clips; }
    private static Dictionary<string,BossMove> cached;
    public static BossMove Get(string name)
    {
        if(cached==null)Load();
        if(!cached.TryGetValue(name,out BossMove move))throw new ArgumentException("Unknown Dojaehak move "+name);
        return move;
    }
    private static void Load()
    {
        var asset=Resources.Load<TextAsset>("Characters/Dojaehak/DojaehakMotionInfo");
        if(asset==null)throw new InvalidOperationException("Dojaehak contact metadata missing");
        var info=JsonUtility.FromJson<Info>(asset.text);if(info.clips==null||info.clips.Length!=10)throw new InvalidOperationException("Dojaehak motion metadata incomplete");
        var rows=new Dictionary<string,Row>();foreach(var row in info.clips)rows.Add(row.clip,row);
        cached=new Dictionary<string,BossMove>();
        Add(rows,"PunchLeft","원투!",.42f,.22f,.65f,1.45f,.5f,false);
        Add(rows,"PunchRight","오른손!",.30f,.24f,.75f,1.55f,.7f,false);
        Add(rows,"HookPunch","훅!",.50f,.42f,.85f,1.65f,1.0f,false,35,95);
        Add(rows,"QuadPunch","연속 네 번!",.65f,.90f,.45f,1.6f,.25f,true);
        Add(rows,"MmaKick","발차기!",.55f,.70f,1.10f,1.9f,3.2f,true);
        Add(rows,"SideKick","옆차기!",.58f,.60f,1.05f,2.1f,2.8f,true);
        Add(rows,"RoundhouseKick","돌려차기!",.64f,.95f,1.25f,2.05f,4.0f,true);
        Add(rows,"Kicking","연계 발차기!",.48f,.42f,.90f,1.85f,1.6f,false);
    }
    private static void Add(Dictionary<string,Row> rows,string name,string warning,float windup,float recovery,float damage,float reach,float knockback,bool armor,int start=0,int end=-1)
    {
        Row row=rows[name];int[] contacts=row.contactFrames!=null&&row.contactFrames.Length>0?row.contactFrames:new[]{row.impactFrame};
        cached.Add(name,new BossMove(name,warning,windup,recovery,damage,reach,knockback,armor,contacts,end<0?row.frames-1:end,
            name=="QuadPunch"?new[]{.30f,.30f,.34f}:null,start));
    }
}
