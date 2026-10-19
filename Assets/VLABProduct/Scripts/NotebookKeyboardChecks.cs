using System;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vlab.Product
{
    public static class NotebookKeyboardChecks
    {
        public static void Run(Action<string,bool> check,VlabUi ui,RectTransform parent)
        {
            var box=ui.Panel(parent,"Notebook keyboard check surface",0,0,1440,880);
            var fieldHost=ui.Rect(box,"Native notebook field",0,0,1,1);var text=ui.Text(fieldHost,"",0,0,1,1,30);text.richText=false;
            var target=fieldHost.gameObject.AddComponent<TMP_InputField>();target.textComponent=text;target.textViewport=fieldHost;target.richText=false;target.lineType=TMP_InputField.LineType.MultiLineNewline;target.characterLimit=200;
            int changes=0,finished=0;string delivered="";target.onValueChanged.AddListener(value=>{changes++;delivered=value;});
            NotebookKeyboard.Render(ui,box,target,true,()=>finished++);var state=box.GetComponent<NotebookKeyboardState>();
            void Key(string key){var button=box.GetComponentsInChildren<Button>().Single(x=>x.name=="Notebook key "+key);button.onClick.Invoke();}
            void Select(int anchor,int focus){target.selectionStringAnchorPosition=anchor;target.selectionStringFocusPosition=focus;state.CaptureSelection();}
            void Set(string value){target.text=value;Select(value.Length,value.Length);state.Refresh();}
            try
            {
                foreach(string key in new[]{"t","i","ê","acute","n","g","Space","Shift","v","Shift","i","ê","dot","t"})Key(key);
                check("World keyboard buttons type tiếng Việt",target.text=="tiếng Việt");
                check("Typing invokes the real field change callback",changes>=10&&delivered=="tiếng Việt");
                check("World keyboard preview updates from actual note text",state.Preview.text=="tiếng Việt|");
                Key("Newline");Key("ă");Key("grave");check("World keyboard inserts newline and tones a Vietnamese vowel",target.text=="tiếng Việt\nằ");
                Key("Backspace");check("Backspace removes one precomposed Vietnamese grapheme",target.text=="tiếng Việt\n");
                Set("abXYcd");Select(2,4);Key("đ");check("World keyboard replaces a real field selection",target.text=="abđcd");
                Key("Left");Key("Backspace");check("Caret-left and backspace edit the preceding grapheme",target.text=="ađcd");
                Key("Right");Key(".");check("Caret-right returns insertion between complete graphemes",target.text=="ađ.cd");
                Set("a\u0306\u0301");Key("Backspace");check("Backspace removes a decomposed grapheme without leaving marks",target.text=="");
                Set("a");Key("Left");Key("Right");Key("grave");check("Caret arrows preserve the immediately preceding vowel",target.text=="à");
                Set("ơ");Key("acute");Key("clear-tone");check("Clear tone preserves the Vietnamese horn",target.text=="ơ");
                Set("ă");Key("hook");Key("dot");check("Tone replacement preserves breve and replaces only tone",target.text=="ặ"&&target.text.Normalize(NormalizationForm.FormD).Count(c=>c=='\u0323')==1);
                Set("b");Key("acute");check("Tone buttons never invent a vowel or modify a consonant",target.text=="b");
                Set("ab");target.characterLimit=2;Key("c");check("Character limit retains text when another key cannot fit",target.text=="ab");
                Select(1,2);Key("đ");check("Replacement remains possible at character limit",target.text=="ađ");
                Key("Done");Key("Done");check("Done callback fires once and retains typed data",finished==1&&target.text=="ađ"&&delivered=="ađ");
                check("Every keyboard control is at least 70 px high",box.GetComponentsInChildren<Button>().All(button=>button.GetComponent<RectTransform>().rect.height>=70));
                Pure(check);
            }
            finally{box.gameObject.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(box.gameObject);else UnityEngine.Object.DestroyImmediate(box.gameObject);}
        }
        public static void Pure(Action<string,bool> check)
        {
            string text=NotebookKeyboard.Insert("abcd",3,1,"ê",4,out int caret,out bool accepted);check("Pure insertion replaces reverse selections",accepted&&text=="aêd"&&caret==2);
            text=NotebookKeyboard.Backspace("Ae\u0301",3,3,out caret);check("Pure erase preserves preceding Latin text",text=="A"&&caret==1);
            text=NotebookKeyboard.Backspace("x\U0001F642",3,3,out caret);check("Backspace keeps surrogate pairs intact",text=="x"&&caret==1);
            text=NotebookKeyboard.Tone("Ô",1,1,"tilde",10,out caret,out accepted);check("Uppercase circumflex and tone compose to NFC",accepted&&text=="Ỗ"&&text.IsNormalized(NormalizationForm.FormC));
            text=NotebookKeyboard.Tone("ǜ",1,1,"acute",10,out caret,out accepted);check("Tone changes preserve non-tone combining marks",accepted&&text.Normalize(NormalizationForm.FormD).Contains('\u0308')&&!text.Normalize(NormalizationForm.FormD).Contains('\u0300'));
            text=NotebookKeyboard.Insert("ab",1,2,"\U0001F642",2,out caret,out accepted);check("Rejected over-limit grapheme does not partially replace selection",!accepted&&text=="ab");
        }
    }
}
