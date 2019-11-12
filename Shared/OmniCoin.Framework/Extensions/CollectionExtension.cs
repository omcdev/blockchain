


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Framework.Extensions
{
    public static class CollectionExtension
    {
        public static T GetRandom<T>(this IEnumerable<T> target)
        {
            if (target == null || !target.Any())
            {
                return default(T);
            }
            Random random = new Random();
            var index = random.Next(0, target.Count());
            return target.ElementAt(index);
        }
    }
}
