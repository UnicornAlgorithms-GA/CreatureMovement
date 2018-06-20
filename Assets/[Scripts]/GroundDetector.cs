using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GroundDetector : MonoBehaviour
{
	public UnityEvent onCollide;

	private void OnCollisionEnter2D(Collision2D collision)
	{
		onCollide?.Invoke();
	}
}
