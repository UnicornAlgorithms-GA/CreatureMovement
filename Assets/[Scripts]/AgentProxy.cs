using System;
using System.Linq;
using GeneticLib.Genome.NeuralGenomes;
using UnityEngine;

public abstract class AgentProxy : MonoBehaviour
{
	public NeuralGenome neuralGenome;
	private PopulationProxy populationProxy;

	public int nbOfInputs;
	public int nbOfOutputs;

	public virtual void Init(PopulationProxy populationProxy)
	{
		this.populationProxy = populationProxy;
	}

	public virtual void ResetAgent(
		Vector3 pos,
		NeuralGenome newNeuralGenome = null)
	{
		this.transform.position = pos;
		this.neuralGenome = newNeuralGenome;

		gameObject.SetActive(true);
		this.neuralGenome.Fitness = 0;
	}

	public virtual void End()
	{
		gameObject.SetActive(false);
	}

	#region Genetics
	public abstract void MoveFromNetwork();
	#endregion
}


public struct InitialAgentPartState
{
	public Vector3 localPos;
	public Quaternion localRot;
}