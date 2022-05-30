using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ArrayExtentions
{
    public static T[] Add<T> (this T[] array, T item)
    {
        T[] result;
        if (array == null || array.Length == 0)
        {
            result = new T[1] { item };
            return result;
        }

        result = new T[array.Length + 1];

        for (int i = 0; i < array.Length; i++)
        {
            result[i] = array[i];
        }
        result[array.Length] = item;
        return result;
    }
}
