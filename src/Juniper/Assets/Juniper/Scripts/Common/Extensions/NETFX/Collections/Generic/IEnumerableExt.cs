using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extension methods for <c>System.Collections.Generic.IEnumerable{T}</c>
    /// </summary>
    public static class IEnumerableExt
    {
        public static void MaybeAdd<T>(this List<T> collect, T value)
        {
            if (!collect.Contains(value))
            {
                collect.Add(value);
            }
        }

        public static void MaybeAddRange<T>(this List<T> collect, IEnumerable<T> values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    collect.MaybeAdd(value);
                }
            }
        }

        /// <summary>
        /// A random number generator to use with the following methods.
        /// </summary>
        private static Random r = new Random();

        /// <summary>
        /// Get a random item out of the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns>A random item out of the collection.</returns>
        /// <example>
        /// // This is just one potential outcome. var arr = new int[]{ 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        /// arr.Random(); // --&gt; 3 arr.Random(); // --&gt; 1 arr.Random(); // --&gt; 4
        /// arr.Random(); // --&gt; 1 arr.Random(); // --&gt; 5 arr.Random(); // --&gt; 9
        /// arr.Random(); // --&gt; 2 arr.Random(); // --&gt; 6
        /// </example>
        public static T Random<T>(this IEnumerable<T> collection)
        {
            var count = collection.Count();
            var skip = r.Next(0, count);
            return collection.Skip(skip)
                .FirstOrDefault();
        }

        /// <summary>
        /// Checks two collections to see if they contain all the same items.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// True if the collections are the same length and contain the same items, in the same order.
        /// </returns>
        /// <example>
        /// var a = new int[]{ 1, 2, 3, 4, 5 }; var b = new int[]{ 1, 2, 3, 4, 5 }; var c = new
        /// int[]{ 1, 2, 3, 4, 6 }; var d = new int[]{ 1, 2, 3, 4, 5, 6 }; var e = new int[]{ 1, 2,
        /// 3, 4 }; /// a.Matches(b); // --&gt; true a.Matches(c); // --&gt; false a.Matches(d); //
        /// --&gt; false a.Matches(e); // --&gt; false
        /// </example>
        public static bool Matches<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a.Count() != b.Count())
            {
                return false;
            }

            using (var c = a.GetEnumerator())
            using (var d = b.GetEnumerator())
            {
                while (c.MoveNext() && d.MoveNext())
                {
                    if (!c.Current.Equals(d.Current))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks two collections to see if they contain all the same items.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// True if the collections are the same length and contain the same items, in the same order.
        /// </returns>
        /// <example>
        /// var a = new int[]{ 1, 2, 3, 4, 5 }; var b = new int[]{ 1, 2, 3, 4, 5 }; var c = new
        /// int[]{ 1, 2, 3, 4, 6 }; var d = new int[]{ 1, 2, 3, 4, 5, 6 }; var e = new int[]{ 1, 2,
        /// 3, 4 }; /// a.Matches(b); // --&gt; true a.Matches(c); // --&gt; false a.Matches(d); //
        /// --&gt; false a.Matches(e); // --&gt; false
        /// </example>
        public static bool Matches<T>(this IEnumerable<T> a, IEnumerable b)
        {
            return a.Matches(b.Cast<T>());
        }

        /// <summary>
        /// Checks two collections to see if they contain all the same items.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// True if the collections are the same length and contain the same items, in the same order.
        /// </returns>
        /// <example>
        /// var a = new int[]{ 1, 2, 3, 4, 5 }; var b = new int[]{ 1, 2, 3, 4, 5 }; var c = new
        /// int[]{ 1, 2, 3, 4, 6 }; var d = new int[]{ 1, 2, 3, 4, 5, 6 }; var e = new int[]{ 1, 2,
        /// 3, 4 }; /// a.Matches(b); // --&gt; true a.Matches(c); // --&gt; false a.Matches(d); //
        /// --&gt; false a.Matches(e); // --&gt; false
        /// </example>
        public static bool Matches<T>(this IEnumerable<T> a, IEnumerator b)
        {
            return a.Matches(b.AsEnumerable().Cast<T>());
        }

        /// <summary>
        /// Check two collections of types to see if they contain all the same items.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Matches(this IEnumerable<Type> a, IEnumerable<Type> b)
        {
            if (a.Count() != b.Count())
            {
                return false;
            }

            using (var c = a.GetEnumerator())
            using (var d = b.GetEnumerator())
            {
                while (c.MoveNext() && d.MoveNext())
                {
#if NETFX_CORE
                    var e = c.Current.GetTypeInfo();
                    var f = d.Current.GetTypeInfo();
#else
                    var e = c.Current;
                    var f = d.Current;
#endif
                    if (!e.IsAssignableFrom(f))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Takes N enumerators and moves through them all, even if some of them are null or finished.
        /// </summary>
        /// <param name="enums"></param>
        /// <returns><c>True</c> when all enumerators can no longer move to another item.</returns>
        public static bool MoveNext(this IEnumerable<IEnumerator> enums)
        {
            var anyNext = false;
            foreach (var iter in enums)
            {
                anyNext |= iter != null && iter.MoveNext();
            }
            return anyNext;
        }

        /// <summary>
        /// For N enumerators, returns all M <c>Current</c> values of each non-null entry in the
        /// <paramref name="enums"/> collection. M is less than or equal to N.
        /// </summary>
        /// <param name="enums"></param>
        /// <returns></returns>
        public static IEnumerable Current(this IEnumerable<IEnumerator> enums)
        {
            var anyExist = false;
            var anySucceed = false;
            foreach (var e in enums)
            {
                if (e != null)
                {
                    anyExist = true;
                    object value;
                    try
                    {
                        value = e.Current;
                        anySucceed = true;
                    }
                    catch (InvalidOperationException)
                    {
                        value = null;
                    }

                    if (value != null)
                    {
                        yield return value;
                    }
                }
            }

            if (anyExist && !anySucceed)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Filters a collection and checks to see if it would return 0 results.
        /// </summary>
        /// <returns>The empty.</returns>
        /// <param name="enumer">Enumer.</param>
        /// <param name="predicate">Predicate.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static bool Empty<T>(this IEnumerable<T> enumer, Func<T, bool> predicate)
        {
            return !enumer.Any(predicate);
        }

        /// <summary>
        /// Evaluates a collection and checks to see if it has 0 items.
        /// </summary>
        /// <returns>The empty.</returns>
        /// <param name="enumer">Enumer.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static bool Empty<T>(this IEnumerable<T> enumer)
        {
            return !enumer.Any();
        }

        /// <summary>
        /// Evaluates a collection and checks to see if it has 0 items.
        /// </summary>
        /// <returns>The empty.</returns>
        /// <param name="enumer">Enumer.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static bool Empty(this IEnumerable enumer)
        {
            return enumer.Cast<object>().Empty();
        }

        /// <summary>
        /// Convert an Enumerator collection to an Enumerable collection.
        /// </summary>
        /// <returns>The enumerable.</returns>
        /// <param name="iter">Iter.</param>
        public static IEnumerable AsEnumerable(this IEnumerator iter)
        {
            while (iter?.MoveNext() == true)
            {
                yield return iter.Current;
            }
        }

        /// <summary>
        /// Create an <see cref="InterleavedEnumerator"/> out of a sequence of enumerators.
        /// </summary>
        /// <param name="iters">The enumerators to interleave</param>
        /// <returns>The interleaved enumerator</returns>
        public static IEnumerator Interleave(this IEnumerable<IEnumerator> iters)
        {
            return new InterleavedEnumerator(iters);
        }

        /// <summary>
        /// Add an entire collection to a queue.
        /// </summary>
        /// <typeparam name="T">The type of elements in the queue.</typeparam>
        /// <param name="q">The queue to which to add items.</param>
        /// <param name="e">The items to add to the queue, in order.</param>
        public static void AddRange<T>(this Queue<T> q, IEnumerable<T> e)
        {
            foreach (var o in e)
            {
                q.Enqueue(o);
            }
        }

        /// <summary>
        /// Make Queues usable with collection initializer sytnax.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the queue</typeparam>
        /// <param name="q">The queue to which to add the item.</param>
        /// <param name="value">The item to add to the queue.</param>
        public static void Add<T>(this Queue<T> q, T value)
        {
            q.Enqueue(value);
        }
    }
}
