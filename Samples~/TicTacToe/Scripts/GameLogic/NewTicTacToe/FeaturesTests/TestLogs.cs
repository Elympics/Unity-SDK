using UnityEngine;
using System.Collections;

public class TestLogs : MonoBehaviour
{
	IEnumerator Start()
	{
		for (int i = 0; i < 3; i++)
		{
			Debug.Log(i);
			yield return new WaitForSeconds(1);
		}
	}
}
