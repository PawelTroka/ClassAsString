using System.Text.RegularExpressions;

namespace ClassAsString.Model
{
    internal class CodeRegularExpressions
    {
        public static Regex ClassBodyRegex { get; } =
            new Regex(@"(\s|^|{)class\s+[^{]+({(?:[^{}]|(?<open>{)|(?<-open>}))+(?(open)(?!))})", RegexOptions.Compiled)
            ;

        public static Regex ToCodeRegex { get; } = new Regex(@"(;|^|}|{)[^;}{]*ToCode\s*=\s*(\s|.)+?[^""]+""\s*;",
            RegexOptions.Compiled);
    }
}