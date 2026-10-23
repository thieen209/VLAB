using UnityEngine;

namespace Vlab.Product
{
    public sealed class UiPalette
    {
        public readonly Color panel,raised,stroke,text,secondary;
        public UiPalette(string panel,string raised,string stroke,string text,string secondary)
        {this.panel=VlabUi.Hex(panel);this.raised=VlabUi.Hex(raised);this.stroke=VlabUi.Hex(stroke);this.text=VlabUi.Hex(text);this.secondary=VlabUi.Hex(secondary);}
    }
    public static class UiTheme
    {
        static readonly UiPalette[] palettes={new UiPalette("111827","1F2937","64748B","F3F4F6","CBD5E1"),new UiPalette("F5F1E7","E5DFD2","786F62","202C36","4C5863"),new UiPalette("23272D","343A42","697581","F7FAFF","C3CDD6")};
        public static int Index{get;private set;}
        public static float Opacity{get;private set;}=.8f;
        public static UiPalette Current=>palettes[Index];
        public static UiPalette Palette(int index)=>palettes[Mathf.Clamp(index,0,2)];
        public static void Select(int index,float opacity){Index=Mathf.Clamp(index,0,2);Opacity=Mathf.Clamp(opacity,.55f,1);}
        static float Luminance(Color color){Color linear=color.linear;return .2126f*linear.r+.7152f*linear.g+.0722f*linear.b;}
        public static float Contrast(Color a,Color b){float x=Luminance(a),y=Luminance(b);return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f);}
        public static Color On(Color background)
        {Color dark=VlabUi.Hex("101D32"),light=VlabUi.Hex("F7FAFF");var chosen=Contrast(dark,background)>Contrast(light,background)?dark:light;if(Contrast(chosen,background)>=4.5f)return chosen;return Contrast(Color.black,background)>Contrast(Color.white,background)?Color.black:Color.white;}
        public static Color Readable(Color proposed,Color background)
        {if(Contrast(proposed,background)>=4.5f)return proposed;Color target=On(background);for(int i=1;i<=10;i++){var value=Color.Lerp(proposed,target,i/10f);if(Contrast(value,background)>=4.5f)return value;}return target;}
    }
}
