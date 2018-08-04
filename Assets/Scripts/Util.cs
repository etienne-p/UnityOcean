using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util 
{
	// naive implementation, however ix and iy are expected to be small in size, so no real harm
	public static float LinInterp (float x, float[] ix, float[] iy)
	{
        // handle edge cases
		if (x < ix [0])
		{
			return iy [0];
		}
		if (x >= ix [ix.Length - 1])
		{
			return iy [ix.Length - 1];
		}
        // general case
		for (int i = 0; i != ix.Length - 1; ++i)
		{
			if (x >= ix [i] && x < ix [i + 1])
			{
				return Mathf.Lerp (iy [i], iy [i + 1], (x - ix [i]) / (ix [i + 1] - ix [i]));
			}
		}
		return -1;
	}
}
