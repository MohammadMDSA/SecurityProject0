using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public static class Helper
    {
        private static string Chars => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static Random random = new Random();
        public static string SocketMessageAttributeSeperator => "@@@@@@@@@@";
        public static string SocketMessageSplitter => "||||||||||";
        public static string RandomString64 => new string(Enumerable.Repeat(Chars, 64)
              .Select(s => s[random.Next(s.Length)]).ToArray());

        public static IEnumerable<string> Split(this string value, int desiredLength)
        {
            var characters = StringInfo.GetTextElementEnumerator(value);
            while (characters.MoveNext())
                yield return String.Concat(Take(characters, desiredLength));
        }

        private static IEnumerable<string> Take(TextElementEnumerator enumerator, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                yield return (string)enumerator.Current;

                if (!enumerator.MoveNext())
                    yield break;
            }
        }
    }
}
