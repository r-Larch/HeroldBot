using System.Collections.Generic;
using System.Linq;


namespace LarchSys.Bot {
    public static class BatchLinq {
        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source) {
                if (bucket == null) {
                    bucket = new TSource[size];
                }

                bucket[count++] = item;
                if (count != size) {
                    continue;
                }

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0) {
                yield return bucket.Take(count);
            }
        }
        //public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        //{
        //    if (size <= 0) {
        //        throw new ArgumentOutOfRangeException("size", "Must be greater than zero.");
        //    }

        //    using var enumerator = source.GetEnumerator();
        //    while (enumerator.MoveNext()) {
        //        var i = 0;

        //        // Batch is a local function closing over `i` and `enumerator` that
        //        // executes the inner batch enumeration
        //        IEnumerable<T> Batch()
        //        {
        //            do yield return enumerator.Current;
        //            while (++i < size && enumerator.MoveNext());
        //        }

        //        yield return Batch();
        //        while (++i < size && enumerator.MoveNext()) ; // discard skipped items
        //    }
        //}
    }
}
