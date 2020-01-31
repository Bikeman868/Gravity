using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Utility
{
    internal static class NumberComparer
    {
        public static IntComparer Int32 { get; } = new IntComparer();
        public static UnsignedShortComparer UnsignedShort { get; } = new UnsignedShortComparer();

        public class IntComparer: IComparer, IEqualityComparer, IComparer<int>, IEqualityComparer<int>
        {
            public int Compare(object x, object y)
            {
                return ((int)y) - ((int)x);
            }

            public new bool Equals(object x, object y)
            {
                return ((int)y) == ((int)x);
            }

            public int GetHashCode(object obj)
            {
                return ((int)obj).GetHashCode();
            }

            public int Compare(int x, int y)
            {
                return y - x;
            }

            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                return obj.GetHashCode();
            }
        }

        public class UnsignedShortComparer : IComparer, IEqualityComparer, IComparer<ushort>, IEqualityComparer<ushort>
        {
            public int Compare(object x, object y)
            {
                return ((ushort) y) - ((ushort) x);
            }

            public new bool Equals(object x, object y)
            {
                return ((ushort)y) == ((ushort)x);
            }

            public int GetHashCode(object obj)
            {
                return ((ushort) obj).GetHashCode();
            }

            public int Compare(ushort x, ushort y)
            {
                return y - x;
            }

            public bool Equals(ushort x, ushort y)
            {
                return x == y;
            }

            public int GetHashCode(ushort obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}