using UnityEngine;
public sealed class VlabLessonPanel : MonoBehaviour
{
 [SerializeField] UnityEngine.UI.Text title,body,progress,data;
 [SerializeField] UnityEngine.UI.Button next,reset;
 string[] steps=new string[0];
 public string LessonId { get; private set; }="";
 public int StepIndex { get; private set; }
 public int StepCount=>steps.Length;
 static bool Valid(string value,int limit){if(string.IsNullOrWhiteSpace(value)||value.Length>limit)return false;foreach(char c in value)if(char.IsControl(c))return false;return true;}
 public bool Configure(string id,string heading,string[] instructions,string[] rows){
  if(!isActiveAndEnabled||id==null||id.Length!=3||"BCMP".IndexOf(id[0])<0||id[1]!='0'||id[2]<'1'||id[2]>'8'||!Valid(heading,80)||instructions==null||instructions.Length<1||instructions.Length>12||rows==null||rows.Length>6)return false;
  foreach(var s in instructions)if(!Valid(s,300))return false;foreach(var s in rows)if(!Valid(s,64))return false;
  LessonId=id;title.text=id+"  ·  "+heading;steps=(string[])instructions.Clone();data.text=string.Join("\n",rows);StepIndex=0;Refresh();return true;
 }
 void Refresh(){body.text=steps.Length>0?steps[StepIndex]:"Chưa chọn nội dung";progress.text=steps.Length>0?"Bước "+(StepIndex+1)+" / "+steps.Length:"";next.interactable=steps.Length>0&&StepIndex<steps.Length-1;reset.interactable=steps.Length>0;}
 public void NextStep(){if(!isActiveAndEnabled||StepIndex>=steps.Length-1)return;StepIndex++;Refresh();}
 public void ResetSteps(){if(!isActiveAndEnabled)return;StepIndex=0;Refresh();}
 public void ClearLesson(){LessonId="";steps=new string[0];StepIndex=0;title.text="Chọn bài học";data.text="";Refresh();}
}
