using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour
{
    public static float CalculateApproach(float start, float end, float t)
    {
        //
        if (start < end)
            return Mathf.Min(start + t, end);
        else
            return Mathf.Max(start - t, end);
    }
}