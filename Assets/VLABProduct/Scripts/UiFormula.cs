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
            string[,] symbols={{"Delta","Δ"},{"pi","π"},{"theta","θ"},{"lambda","λ"},{"mu","μ"},{"rho","ρ"},{"omega","ω"},{"sigma","σ"},{"times","×"},{"cdot","·"},{"le","≤"},{"ge","≥"}};
            for(int i=0;i<symbols.GetLength(0);i++)text=Regex.Replace(text,@"\\"+symbols[i,0]+@"\b",symbols[i,1]);
            text=Regex.Replace(text,@"\\sqrt\{([^{}]+)\}","√($1)");
            text=Regex.Replace(text,@"\\frac\{([^{}]+)\}\{([^{}]+)\}","<sup>$1</sup>⁄<sub>$2</sub>");
            text=Regex.Replace(text,@"\^(?:\{([^{}]+)\}|([0-9+−-]+))",m=>"<sup>"+(m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value)+"</sup>");
            return Regex.Replace(text,@"_(?:\{([^{}]+)\}|([0-9]+))",m=>"<sub>"+(m.Groups[1].Success?m.Groups[1].Value:m.Groups[2].Value)+"</sub>");
        }
    }
}
