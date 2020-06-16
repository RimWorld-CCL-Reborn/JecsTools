using System;
using System.Collections.Generic;

namespace PawnShields
{
    public static class ListExtensions
    {
        public static List<T> AsList<T>(this IEnumerable<T> enumerable) =>
            enumerable is List<T> list ? list : new List<T>(enumerable);

        public static List<T> PopAll<T>(this ICollection<T> collection)
        {
            var list = new List<T>(collection);
            collection.Clear();
            return list;
        }

        public static int FindSequenceIndex<T>(this List<T> list, params Predicate<T>[] sequenceMatches)
        {
            return list.FindSequenceIndex(0, list.Count, sequenceMatches);
        }

        public static int FindSequenceIndex<T>(this List<T> list, int startIndex, params Predicate<T>[] sequenceMatches)
        {
            return list.FindSequenceIndex(startIndex, list.Count - startIndex, sequenceMatches);
        }

        public static int FindSequenceIndex<T>(this List<T> list, int startIndex, int count, params Predicate<T>[] sequenceMatches)
        {
            if (sequenceMatches is null)
                throw new ArgumentNullException(nameof(sequenceMatches));
            if (sequenceMatches.Length == 0)
                throw new ArgumentException($"sequenceMatches must not be empty");
            if (count - sequenceMatches.Length < 0)
                return -1;
            count -= sequenceMatches.Length - 1;
            var index = list.FindIndex(startIndex, count, sequenceMatches[0]);
            while (index != -1)
            {
                var allMatched = true;
                for (var matchIndex = 1; matchIndex < sequenceMatches.Length; matchIndex++)
                {
                    if (!sequenceMatches[matchIndex](list[index + matchIndex]))
                    {
                        allMatched = false;
                        break;
                    }
                }
                if (allMatched)
                    break;
                startIndex++;
                count--;
                index = list.FindIndex(startIndex, count, sequenceMatches[0]);
            }
            return index;
        }
    }
}
