using UnityEngine;
using System.Collections;
using GeneticLib.Neurology.NeuralModels;
using GeneticLib.Randomness;
using GeneticLib.Neurology;
using System.Collections.Generic;
using GeneticLib.Neurology.Neurons;
using System.Linq;

public class CreaturePopulation : PopulationProxy
{
	private void Start()
	{
		Init();
	}

	private void FixedUpdate()
	{
		OnFixedUpdate();
	}

	protected override INeuralModel InitNeuralModel()
    {
        var model = new NeuralModelBase();
        model.defaultWeightInitializer = () => GARandomManager.NextFloat(-1, 1);

        model.WeightConstraints = weightConstraints.ToTuple();

		var layers = new List<Neuron[]>()
		{
            // Inputs
            model.AddInputNeurons(
				agentPrefab.GetComponent<AgentProxy>().nbOfInputs
			).ToArray(),

			model.AddNeurons(
				new Neuron(-1, ActivationFunctions.TanH),
				count: agentPrefab.GetComponent<AgentProxy>().nbOfInputs * 4
			).ToArray(),

            // Outputs
            model.AddOutputNeurons(
                agentPrefab.GetComponent<AgentProxy>().nbOfOutputs,
				ActivationFunctions.Sigmoid
            ).ToArray(),
        };

        model.ConnectLayers(layers);

		var outputs = layers.Last();
		//model.ConnectNeurons(outputs, outputs);
		model.ConnectLayers(new[] { outputs, outputs });
        return model;
    }

	protected override void Evolve()
	{
		foreach (var agent in agents)
		{
			//var hopper = agent as Hopper;
			//agent.neuralGenome.Fitness += hopper.basePart.position.y;
		}

		base.Evolve();  
	}
}
