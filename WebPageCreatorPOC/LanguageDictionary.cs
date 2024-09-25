using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebPageCreatorPOC
{
  public static class LanguageDictionary
  {
    static Dictionary<string, string> SupportedLanguages = new Dictionary<string, string>()
{
            { "C#", "language-csharp" },
            { "JavaScript", "language-javascript" },
            { "Python", "language-python" },
            { "Java", "language-java" },
            { "C++", "language-cpp" },
            { "HTML", "language-html" },
            { "CSS", "language-css" },
            { "Ruby", "language-ruby" },
            { "Go", "language-go" },
            { "PHP", "language-php" },
            { "TypeScript", "language-typescript" },
            { "Swift", "language-swift" },
            { "Rust", "language-rust" },
            { "Kotlin", "language-kotlin" },
            { "Shell", "language-shell" },
            { "Scala", "language-scala" },
            { "Perl", "language-perl" },
            { "Lua", "language-lua" },
            { "Objective-C", "language-objectivec" },
            { "R", "language-r" },
            { "Haskell", "language-haskell" },
            { "Elixir", "language-elixir" },
            { "Dart", "language-dart" },
            { "PowerShell", "language-powershell" },
            { "Matlab", "language-matlab" },
            { "CoffeeScript", "language-coffeescript" },
            { "Vim script", "language-vimscript" },
            { "TeX", "language-tex" },
            { "C", "language-c" },
            { "Dockerfile", "language-dockerfile" },
            { "Markdown", "language-markdown" },
            { "F#", "language-fsharp" },
            { "Erlang", "language-erlang" },
            { "V", "language-v" }
};

    public static bool TryGetLanguageHTMLClass(string key, out string value)
    {
      if (SupportedLanguages.ContainsKey(key))
      {
        value = SupportedLanguages[key];
        return true;
      }

      value = string.Empty;
      return false;
    }
  }
}
