using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeneticLib.Genome.NeuralGenomes;
using UnityEngine;

public class NJointCreatureAgent : AgentProxy
{
	public float torque = 1f;
	public Rigidbody2D[] rigidbodies;
	public HingeJoint2D[] hingeJoints;   
	public Rigidbody2D[] mutableRbs;   

	private Vector3[] rigidbodiesInitialPos;
	private Quaternion[] rigidbodiesInitialRot;

	private void Awake()
	{
		rigidbodiesInitialPos = rigidbodies.Select(x => x.transform.localPosition)
										   .ToArray();
		rigidbodiesInitialRot = rigidbodies.Select(x => x.transform.localRotation)
										   .ToArray();
	}

	private void FixedUpdate()
	{
		MoveFromNetwork();
	}

	public override void Init(PopulationProxy populationProxy)
	{
		base.Init(populationProxy);
		//maxPartDistance = FindBiggestDistanceOfPartFromCenter();
	}

	public override void MoveFromNetwork()
	{
		if (neuralGenome == null)
			return;

		neuralGenome.FeedNeuralNetwork(GenerateNetworkInputs());
        var outputs = neuralGenome.Outputs
		                          .Select(x => x.Value)
		                          .ToArray();

		Debug.Assert(outputs.Length == mutableRbs.Length);

		for (int i = 0; i < outputs.Length; i++)
		{
			mutableRbs[i].angularVelocity = outputs[i] * torque;
			//mutableRbs[i].AddTorque(outputs[i] * torque);
		}
	}

	protected float[] GenerateNetworkInputs()
	{
		var inputs = new List<float>();
		foreach (var rb in rigidbodies)
		{
			inputs.AddRange(new[]
			{
				rb.transform.localRotation.z,
				rb.transform.localRotation.w,
				rb.transform.rotation.z,
				rb.transform.rotation.w,
			});
		}
		return inputs.ToArray();
	}

	public override void ResetAgent(Vector3 pos, NeuralGenome newNeuralGenome = null)
	{
		base.ResetAgent(pos, newNeuralGenome);

		for (int i = 0; i < rigidbodies.Length; i++)
		{
			rigidbodies[i].transform.SetPositionAndRotation(
				rigidbodiesInitialPos[i],
				rigidbodiesInitialRot[i]);
			rigidbodies[i].angularVelocity = 0;
			rigidbodies[i].velocity = Vector3.zero;
			rigidbodies[i].Sleep();
		}
	}

	#region Helpers
	//private float FindBiggestDistanceOfPartFromCenter()
	//{
	//	return rigidbodies.Max(x => Vector3.Distance(
	//		transform.position,
	//		x.transform.position));
	//}   
    #endregion
}
