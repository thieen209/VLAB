using System.Text.RegularExpressions;

namespace Vlab.Product
{
    // Explicit opt-in for authored lesson formula strings, never personal notebook input.
    // Inline typography only: no TeX engine, equation solving or numerical transformation.
    public static class UiFormula
    {
        public static string RichText(string source)
        {
            if(string.IsNullOrEmpty(source))return source;
            string text=source;
            text=Regex.Replace(text,"[⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻]+",m=>"<sup>"+Digits(m.Value,"⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻")+"</sup>");
            text=Regex.Replace(text,"[₀₁₂₃₄₅₆₇₈₉₊₋]+",m=>"<sub>"+Digits(m.Value,"₀₁₂₃₄₅₆₇₈₉₊₋")+"</sub>");
            string[,] symbols={{"Delta","Δ"},{"pi","π"},{"theta","θ"},{"lambda","λ"},{"mu","μ"},{"rho","ρ"},{"omega","ω"},{"sigma","σ"},{"times","×"},{"cdot","·"},{"le","≤"},{"ge","≥"}};
            for(int i=0;i<symbols.GetLength(0);i++)text=Regex.Replace(text,@"\\"+symbols[i,0]+@"\b",symbols[i,1]);
            text=Regex.Replace(text,@"\\sqrt\{([^{}]+)\}","√($1)");
            text=Regex.Replace(text,@"\\frac\{([^{}]+)\}\{([^{}]+)\}","<sup>$1</sup>⁄<sub>$2</sub>");
            text=Regex.Replace(text,@"\^(?:\{([^{}]+)\}|([0-9+−-]+))",m=>"<sup>"+(m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value)+"</sup>");
            return Regex.Replace(text,@"_(?:\{([^{}]+)\}|([0-9]+))",m=>"<sub>"+(m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value)+"</sub>");
        }
        static string Digits(string text,string alphabet)
        {var chars=text.ToCharArray();for(int n=0;n<chars.Length;n++)chars[n]="0123456789+-"[alphabet.IndexOf(chars[n])];return new string(chars);}
    }
}
