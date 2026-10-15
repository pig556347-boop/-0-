using UnityEngine;

// One sequence per actor. A three-second break starts a fresh left/right opening.
public sealed class PunchSequence
{
    private int count;
    private float last=float.NegativeInfinity;
    public int Count => count;
    public bool Next(float now)
    {
        if(now-last>3)count=0;
        last=now;count++;
        return count==1||count>2&&Random.Range(0,2)==0;
    }
    public void Reset(){count=0;last=float.NegativeInfinity;}
}
