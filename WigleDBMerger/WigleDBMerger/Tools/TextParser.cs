using System;
using System.Text.RegularExpressions;

namespace WigleDBMerger
{
    static class TextParser
    {
        /// <summary>
        /// Поисковик подстроки, удовлетворяющей рег. выражению
        /// </summary>
        public static string TextMatch(string regExpression, string text)
        {
            Regex reExp = new Regex(regExpression, RegexOptions.IgnoreCase);
            return reExp.Match(text).Value;            
        }

        /// <summary>
        /// Замена текста, удовлетворяющего рег выражению, на подстроку
        /// </summary>
        public static string TextReplace(string text, string regExpression, string strReplacement)
        {
            Regex reExp = new Regex(regExpression,RegexOptions.IgnoreCase);
            return reExp.Replace(text, strReplacement);
        }
    }
}
