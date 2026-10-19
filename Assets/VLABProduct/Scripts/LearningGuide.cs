using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Vlab.Product
{
    public static class LearningGuide
    {
        [Serializable] public sealed class Source {public string sourceID,title,url;}
        [Serializable] public sealed class Entry
        {public string id,titleVi,learnVi,practiceVi,pitfallsVi,examplesVi,examVi,checkVi;public Source[] sources;}
        [Serializable] sealed class Catalogue {public Entry[] items;}
        static Dictionary<string,Entry> entries;
        public static IReadOnlyDictionary<string,Entry> All
        {
            get
            {
                if(entries==null)
                {
                    var resource=Resources.Load<TextAsset>("Catalog/learning-guides-vi");
                    var data=resource?JsonUtility.FromJson<Catalogue>(resource.text):null;
                    entries=(data?.items??Array.Empty<Entry>()).Where(item=>item!=null&&!string.IsNullOrEmpty(item.id)).ToDictionary(item=>item.id,StringComparer.Ordinal);
                }
                return entries;
            }
        }
        public static Entry Load(string id)=>id!=null&&All.TryGetValue(id,out var entry)?entry:null;
        public static string Chapter(string id,int chapter,bool vi)
        {
            var entry=Load(id);if(entry==null)return vi?"Tài liệu bài học chưa có trong bản cài đặt này.":"This installation does not contain this Vietnamese study guide.";
            var text=new StringBuilder();if(!vi)text.AppendLine("VIETNAMESE STUDY GUIDE\nThe source learning material below is in Vietnamese.\n");
            string knowledge=entry.learnVi;int firstSentence=knowledge.IndexOf(". ",StringComparison.Ordinal);if(firstSentence>=0)knowledge=knowledge.Insert(firstSentence+2,"\n");
            switch(Mathf.Clamp(chapter,0,3))
            {
                case 0:text.AppendLine("KIẾN THỨC CẦN NHỚ").AppendLine().AppendLine(knowledge).AppendLine().AppendLine("TỰ THỰC HÀNH").AppendLine().AppendLine(entry.practiceVi).AppendLine().AppendLine("PHẠM VI VÀ CÁCH HIỂU MÔ PHỎNG").AppendLine().AppendLine(LessonContext.Explanation(id,true));break;
                case 1:text.AppendLine("VÍ DỤ MINH HỌA").AppendLine(entry.examplesVi).AppendLine().AppendLine("VẬN DỤNG VÀO BÀI TẬP").AppendLine(entry.examVi);break;
                case 2:text.AppendLine("TỰ KIỂM TRA").AppendLine("Tự trả lời trước khi đọc lại kiến thức hoặc thực hành thêm.").AppendLine().AppendLine(entry.checkVi).AppendLine().AppendLine("CHỖ DỄ NHẦM").AppendLine(entry.pitfallsVi);break;
                default:text.AppendLine("NGUỒN ĐỂ ĐỌC THÊM").AppendLine("Phần bài học đã được lưu để đọc ngoại tuyến. Đường dẫn dưới đây là thông tin tham khảo; mở trang gốc cần mạng.").AppendLine();
                    foreach(var source in entry.sources??Array.Empty<Source>())text.AppendLine(source.title).AppendLine(source.url).AppendLine();break;
            }
            return text.ToString().Trim();
        }
        public static string[] ChapterLabels(bool vi)=>vi?new[]{"Kiến thức","Vận dụng","Tự kiểm tra","Nguồn"}:new[]{"Knowledge","Application","Self-check","Sources"};
        public static string[] Pages(TMPro.TextMeshProUGUI template,string id,int chapter,bool vi,out int answerPage)
        {
            string source=Chapter(id,chapter,vi);answerPage=-1;int split=chapter==2?source.IndexOf("Đáp:",StringComparison.Ordinal):-1;
            if(split<0)return JournalView.Paginate(template,source);
            var questions=JournalView.Paginate(template,source.Substring(0,split));answerPage=questions.Length;
            return questions.Concat(JournalView.Paginate(template,source.Substring(split))).ToArray();
        }
        public static void Render(VlabUi ui,RectTransform page,string id,bool vi,JournalState state,Action refresh,Color accent)
        {
            state.studyChapter=Mathf.Clamp(state.studyChapter,0,3);var labels=ChapterLabels(vi);
            for(int i=0;i<4;i++){int chapter=i;var tab=ui.Button(page,labels[i],32+i*263,164,247,64,()=>{state.studyChapter=chapter;state.contentPage=0;refresh();},state.studyChapter==i?accent:(Color?)null);tab.name="Journal study chapter "+i;}
            var body=ui.Text(page,"",32,248,1036,292,28);body.name="Journal study page";body.richText=false;
            var pages=Pages(body,id,state.studyChapter,vi,out int answerPage);state.contentPage=Mathf.Clamp(state.contentPage,0,pages.Length-1);body.text=pages[state.contentPage];
            var previous=ui.Button(page,state.contentPage==answerPage?(vi?"‹ Câu hỏi":"‹ Question"):(vi?"‹ Trang trước":"‹ Previous page"),32,552,240,64,()=>{state.contentPage=Math.Max(0,state.contentPage-1);refresh();});previous.name="Journal content previous";previous.interactable=state.contentPage>0;
            ui.Text(page,(vi?"Trang ":"Page ")+(state.contentPage+1)+" / "+pages.Length,302,568,432,44,24,VlabUi.Secondary,TMPro.TextAlignmentOptions.Center);
            var next=ui.Button(page,state.contentPage+1==answerPage?(vi?"Xem đáp án ›":"Reveal answer ›"):(vi?"Trang sau ›":"Next page ›"),828,552,240,64,()=>{state.contentPage=Math.Min(pages.Length-1,state.contentPage+1);refresh();});next.name="Journal content next";next.interactable=state.contentPage+1<pages.Length;
        }
    }
}
