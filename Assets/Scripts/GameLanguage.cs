using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(300)]
public sealed class GameLanguage : MonoBehaviour
{
    public static bool English;
    private readonly List<Text> texts=new List<Text>();
    private readonly Dictionary<Text,string> original=new Dictionary<Text,string>(), rendered=new Dictionary<Text,string>();
    private static readonly string[,] Words={
        {"교실에서 시작되는 한 방","One classroom. One fight."},{"전체화면 전환","Toggle fullscreen"},
        {"로비로 돌아가기","RETURN TO LOBBY"},{"게임시작","START GAME"},{"제작자","CREDITS"},{"뒤로","BACK"},{"전체 음량","Master volume"},
        {"배경음악","Music volume"},{"효과음","Effects volume"},{"언어","LANGUAGE"},{"설정","SETTINGS"},
        {"개인 개발 프로젝트","An independent game"},{"제작:","Created by:"},
        {"스페이스바를 눌러 점프해보세요!","Press Space to jump!"},{"착지하면 다음 연습으로 넘어가요.","Land on the floor to continue."},
        {"WASD로 교실 바닥을 따라 이동해보세요.","Use WASD to move around the classroom."},
        {"조금만 걸어가면 성공이에요.","Walk a short distance to complete this lesson."},
        {"C를 0.6초 동안 누르고 있어보세요.","Hold C for 0.6 seconds."},{"키를 놓으면 다시 일어나요.","Release the key to stand again."},
        {"가까이 다가가 J를 연속으로 눌러보세요.","Approach with WASD and press J repeatedly."},
        {"공격이 끝나기 전에 이어 눌러 3타를 맞혀요.","Press again before recovery to land all 3 hits."},
        {"WASD로 다가가 K로 강공격을 맞혀보세요.","Approach with WASD and land a heavy attack with K."},
        {"준비 동작이 길지만 강하게 밀어낼 수 있어요.","It starts slower but hits harder."},
        {"상대를 바라보며 L을 누르고 있어보세요.","Face your opponent and hold L."},{"정면 공격을 한 번 막으면 성공이에요.","Block one frontal attack to continue."},
        {"공격이 닿기 직전에 L을 눌러보세요.","Tap L just before the attack lands."},{"미리 누르지 말고 공격 예고를 보고 눌러요.","Watch the warning; do not hold L in advance."},
        {"Shift와 WASD를 함께 눌러 피해보세요.","Press Shift and a WASD direction to dodge."},{"방향을 누른 채 Shift를 눌러도 돼요.","You can hold a direction and then press Shift."},
        {"상대 가까이에서 I를 눌러 잡아보세요.","Approach with WASD and press I to grab."},{"거리가 멀다면 WASD로 더 다가가세요.","Move closer if the grab does not connect."},
        {"J 또는 K로 공격해 구슬 3개를 채워보세요.","Land J or K attacks to fill all 3 orbs."},{"WASD로 거리를 맞추고 공격을 적중시켜요.","Adjust your distance with WASD and land hits."},
        {"상대 가까이에서 Q를 눌러보세요.","Approach with WASD and press Q."},{"구슬 3개로 6연타 궁극기를 적중시키세요.","Use all 3 orbs to land the six-hit ultimate."},
        {"모든 조작을 배웠어요. 자유롭게 연습해보세요!","Training complete! All controls are now available."},
        {"잘했어요! 다음 연습으로 넘어갈게요.","Great! On to the next lesson."},{"현재 연습에 필요한 키만 사용할 수 있어요.","Only the keys needed for this lesson are enabled."},
        {"지금은 표시된 키로 먼저 연습해보세요.","Try the highlighted keys for this lesson first."},
        {"지금 L!","L — NOW!"},{"공격 예고를 보고 기다렸다가 L을 눌러요.","Watch the warning, then tap L just before impact."},
        {"조작 연습","CONTROLS"},{"튜토리얼 완료","TRAINING COMPLETE"},{"연속 적중","CONSECUTIVE HITS"},
        {"특수 게이지","SPECIAL"},{"일시정지","PAUSED"},{"계속하기","RESUME"},{"쓰러졌다","DEFEATED"},
        {"현재 라운드 다시 도전","RETRY ROUND"},{"전투 완료","BATTLE COMPLETE"},{"1학년 완료","YEAR ONE COMPLETE"},
        {"WASD 이동","WASD Move"},{"Space 점프","Space Jump"},{"C 앉기","C Crouch"},{"Shift+방향 회피","Shift+direction Dodge"},
        {"J 기본공격","J Attack"},{"K 강공격","K Heavy"},{"L 가드/타이밍 방어","L Guard/Parry"},{"I 잡기","I Grab"},{"Q 궁극기","Q Ultimate"},
        {"그냥 조용히 지내고 싶을 뿐이야.","I just want to be left alone."},{"야, 전학생. 여기서 자냐?","Hey, new kid. Sleeping here?"},
        {"책상이 걷어차였다.","The desk was kicked away."},{"…그만해.","...Enough."},
        {"권민준","Gwon Minjun"},{"명치만","Myeong Chiman"},{"설명해","Seol Myeonghae"},{"덩치","Deongchi"},
        {"학교","SCHOOL"},{"교실 튜토리얼","Classroom tutorial"},{"연습","LESSON"}
    };
    public static string T(string value)
    {if(!English||string.IsNullOrEmpty(value))return value;foreach(var pair in Pairs())value=value.Replace(pair.Key,pair.Value);return value;}
    private static IEnumerable<KeyValuePair<string,string>> Pairs()
    {for(int i=0;i<Words.GetLength(0);i++)yield return new KeyValuePair<string,string>(Words[i,0],Words[i,1]);}
    private void LateUpdate()
    {
        texts.Clear();GetComponentsInChildren(false,texts);
        foreach(Text text in texts)
        {
            string last;
            if(!rendered.TryGetValue(text,out last)||text.text!=last)original[text]=text.text;
            text.text=T(original[text]);rendered[text]=text.text;
        }
    }
}
