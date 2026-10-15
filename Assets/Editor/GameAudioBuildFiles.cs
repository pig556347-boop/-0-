#if UNITY_EDITOR
using System.IO;
using UnityEngine;

public static class GameAudioBuildFiles
{
    public static void CopyTo(string output)
    {
        string target=Path.Combine(output,"Audio");Directory.CreateDirectory(target);
        foreach(string name in new[]{"LobbyMusic","BattleMusic","Hit","Whoosh","Block","Parry","Down","Menu","Success"})
        {
            string source=Path.Combine(Application.dataPath,"Resources/Audio/"+name+".wav");
            if(!File.Exists(source))throw new FileNotFoundException("Required game audio missing",source);
            File.Copy(source,Path.Combine(target,name+".wav"),true);
        }
    }
}
#endif
