using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    static class ArraysExpanded
    {
        public static T[] PopulateNew<T>(int iCount, T val)
        {
            T[] outArr = new T[iCount];
            for (int i = 0; i < iCount; i++)
            {
                outArr[i] = val;
            }
            return outArr;
        }

        public static int[] PopulateNewIntArray(int iMin, int iMax)
        {
            int[] aiOut = new int[iMax - iMin];
            for (int i = 0; i < iMax - iMin; i++)
            {
                aiOut[i] = iMin + i;
            }
            return aiOut;
        }
    }
}
