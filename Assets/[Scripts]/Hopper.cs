using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeneticLib.Genome.NeuralGenomes;
using UnityEngine;

public class Hopper : AgentProxy
{
	public float torque = 10;
	public Rigidbody2D basePart;

	public Rigidbody2D[] rigidbodies;
	public Rigidbody2D[] mutableRBs;
	public HingeJoint2D[] hingeJoints;
	public SpringJoint2D[] springJoints;
	private float[] springJointsNormDist;

	private InitialAgentPartState[] initialAgentPartStates;
    
	private void Awake()
	{
		initialAgentPartStates = rigidbodies.Select(x => new InitialAgentPartState
		{
			localPos = x.transform.localPosition,
			localRot = x.transform.localRotation
		}).ToArray();

		springJointsNormDist = springJoints.Select(x => x.distance)
										   .ToArray();
	}

	private void FixedUpdate()
    {
		if (neuralGenome == null)
            return;
		
        MoveFromNetwork();

		//neuralGenome.Fitness = Mathf.Max(neuralGenome.Fitness, rigidbodies[0].transform.position.y);
		neuralGenome.Fitness = Mathf.Max(neuralGenome.Fitness, basePart.transform.position.y);
    }

	public override void MoveFromNetwork()
	{
		if (neuralGenome == null)
			return;
		
		neuralGenome.FeedNeuralNetwork(GenerateNetworkInputs());
        var outputs = neuralGenome.Outputs
                                  .Select(x => x.Value)
                                  .ToArray();
  
		Debug.Assert(outputs.Length == springJoints.Length);

		for (int i = 0; i < outputs.Length; i++)
		{
			var joint = hingeJoints[i];
			var rb = joint.attachedRigidbody;

			var angle = Mathf.Lerp(
				joint.limits.min,
				joint.limits.max,
				outputs[i]);

			var delta = angle - rb.transform.localRotation.eulerAngles.z;
			rb.transform.RotateAround(
				joint.anchor,
				new Vector3(0, 0, 1),
				delta);
				

			//springJoints[i].distance = Mathf.Lerp(
				//0,
				//springJointsNormDist[i] * 2,
				//outputs[i]);
        }
	}

	protected float[] GenerateNetworkInputs()
	{
		var result = new List<float>();
		foreach (var joint in hingeJoints)
		{
			var input = Mathf.InverseLerp(
				joint.limits.min,
				joint.limits.max,
				joint.attachedRigidbody.transform.localRotation.eulerAngles.z);
            
			result.Add(input);
		}

		//for (var i = 0; i < springJoints.Length; i++)
		//{
		//	var input = Mathf.InverseLerp(
		//		0,
		//		springJointsNormDist[i] * 2,
		//		springJoints[i].distance);
		//	result.Add(input);
		//}

		result.Add(basePart.transform.rotation.z);
		result.Add(basePart.transform.rotation.w);

		//Debug.Log(TabToStr(result));

		return result.ToArray();
	}   

	private string TabToStr(IEnumerable<float> tab)
	{
		var result = "";
		foreach (var x in tab)
			result += x.ToString() + " ";
		return result;
	}

	public override void ResetAgent(Vector3 pos, NeuralGenome newNeuralGenome = null)
    {
        base.ResetAgent(pos, newNeuralGenome);
		transform.rotation = Quaternion.Euler(0, 0, 90);      

        for (int i = 0; i < rigidbodies.Length; i++)
        {
			rigidbodies[i].transform.localPosition = initialAgentPartStates[i].localPos;
			rigidbodies[i].transform.localRotation = initialAgentPartStates[i].localRot;
			rigidbodies[i].angularVelocity = 0;
			rigidbodies[i].velocity = Vector2.zero;
			rigidbodies[i].Sleep();
        }
    }

	public void OnFall()
	{
		if (neuralGenome == null)
			return;
		//neuralGenome.Fitness -= 0;
		//End();
	}
}