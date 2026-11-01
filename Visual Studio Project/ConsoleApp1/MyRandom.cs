using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    static class MyRandom
    {
        static Random randGen = new Random(9143);

        public static int RandomInt(int iMax)
        {
            return randGen.Next(iMax);
        }

        public static T[] PickRandomFromArray<T>(T[] array, int iAmount)
        {
            if (iAmount >= array.Length)
            {
                return null;
            }
            T[] outArr = new T[iAmount];
            bool[] abChosen = ArraysExpanded.PopulateNew(array.Length, false);
            for (int i = 0; i < iAmount; i++)
            {
                int iSelected = RandomInt(iAmount - i);
                int iCounted = 0;
                for (int j = 0; j < array.Length && iCounted < i; j++)
                {
                    if (abChosen[j])
                    {
                        iCounted++;
                    }
                }
                outArr[i] = array[iSelected + iCounted];
                abChosen[iSelected + iCounted] = true;
            }
            return outArr;
        }

        public static T PickRandom<T>(T[] array)
        {
            return array[RandomInt(array.Length)];
        }

        public static (T, T[]) PickRandomWithReducedArray<T>(T[] array)
        {
            T outVal = PickRandom(array);
            T[] outArray = new T[array.Length - 1];
            bool bReduced = false;
            for (int i = 0; i < array.Length; i++)
            {
                if(outArray.Length < i)
                {
                    if (!bReduced && outVal.Equals(array[i]))
                    {
                        bReduced = true;
                    }
                    else
                    {
                        outArray[i] = array[(bReduced) ? i + 1 : i];
                    }
                }
            }
            return (outVal, outArray);
        }
    }
}
