using System.Linq;

namespace ViewsSourceGenerator.Tools
{
    public static class StringHelpers
    {
        public static string NormalizeSlashes(this string pathToNormalize, char targetSlash)
        {
            if (string.IsNullOrEmpty(pathToNormalize))
            {
                return string.Empty;
            }
            
            if (targetSlash == '/')
            {
                return pathToNormalize.Replace('\\', '/');
            }
            else
            {
                return pathToNormalize.Replace('/', '\\');
            }
        }

        public static string Capitalize(this string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return string.Empty;
            }
            
            return char.ToUpperInvariant(sourceString[0]) + sourceString.Substring(1);
        }
        
        public static string Decapitalize(this string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return string.Empty;
            }
            
            return char.ToLowerInvariant(sourceString[0]) + sourceString.Substring(1);
        }
        
        public static string Camel(this string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return string.Empty;
            }
            
            return sourceString[0] == '_' ? sourceString : '_' + sourceString;
        }
        
        public static string ToPascalCase(this string sourceString)
        {
            if (!sourceString.Contains(' ') && !sourceString.Contains('_'))
            {
                return sourceString;
            }

            var pieces = sourceString.Split(' ', '_').Select(piece => piece.Capitalize()).ToArray();
            
            return string.Join(string.Empty, pieces);
        }
        
        public static string Remove(this string sourceString, string substringToRemove)
        {
            return sourceString.Replace(substringToRemove, "");
        }
    }
}
