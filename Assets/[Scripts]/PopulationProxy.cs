using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeneticLib.Generations;
using GeneticLib.Generations.InitialGeneration;
using GeneticLib.GeneticManager;
using GeneticLib.Genome.NeuralGenomes;
using GeneticLib.Genome.NeuralGenomes.NetworkOperationBakers;
using GeneticLib.GenomeFactory;
using GeneticLib.GenomeFactory.GenomeProducer;
using GeneticLib.GenomeFactory.GenomeProducer.Breeding;
using GeneticLib.GenomeFactory.GenomeProducer.Breeding.Crossover;
using GeneticLib.GenomeFactory.GenomeProducer.Reinsertion;
using GeneticLib.GenomeFactory.GenomeProducer.Selection;
using GeneticLib.GenomeFactory.Mutation;
using GeneticLib.GenomeFactory.Mutation.NeuralMutations;
using GeneticLib.Neurology;
using GeneticLib.Neurology.NeuralModels;
using GeneticLib.Neurology.Neurons;
using GeneticLib.Neurology.NeuronValueModifiers;
using GeneticLib.Randomness;
using GeneticLib.Utils.Graph;
using GeneticLib.Utils.NeuralUtils;
using MoreLinq;
using UnityEngine;
using UnityEngine.UI;

public abstract class PopulationProxy : MonoBehaviour
{
	// Network Drawing configurations.
	private static readonly string pyAssemblyCmd =
		"/usr/local/bin/python3";
	private static readonly string pyNeuralNetGraphDrawerPath =
		"./Submodules/MachineLearningPyGraphUtils/PyNeuralNetDrawer.py";
	private static readonly string pyFitnessGraphPath =
		"../Submodules/MachineLearningPyGraphUtils/DrawGraph.py";
	
	private NeuralNetDrawer neuralNetDrawer = null;

	[Header("Agents")]
	public GameObject agentPrefab;
	public Transform agentStartPos;
	public AgentProxy[] agents;

	[Header("Configs")]
	public bool drawNetwork = true;

	public float lifeSpan = 10f;
	private float startTime;

	[Header("Genetics configurations")]
	public int genomesCount = 50;

	public Vector2 weightConstraints = Vector2.one * float.MaxValue;

	public float singleSynapseMutChance = 0.2f;
	public float singleSynapseMutValue = 3f;

	public float allSynapsesMutChance = 0.1f;
	public float allSynapsesMutChanceEach = 1f;
	public float allSynapsesMutValue = 1f;

	public float crossoverPart = 0.80f;
	public float reinsertionPart = 0.2f;

	public float dropoutValue = 0.1f;
	private GeneticManagerClassic geneticManager;

	[Header("Other")]
	public Text generationCounter;

	public PopulationProxy()
	{
		ConfigureStaticSettings();
	}

	protected void Init()
    {
        GARandomManager.Random = new RandomClassic(TimeSinceEpochSeconds());

        if (drawNetwork)
            neuralNetDrawer = new NeuralNetDrawer(false);

		agents = InitAgents().ToArray();
        InitGeneticManager();
        AssignBrains();
    }

	protected virtual void OnFixedUpdate()
	{
		if (Time.time > startTime + lifeSpan)
            Evolve();
	}

	#region Genetic Specific
	protected virtual void Evolve()
    {
        foreach (var agent in agents)
            agent.End();

		if (drawNetwork)
            DrawBestGenome();

        geneticManager.Evolve();
        AssignBrains();

		if (generationCounter != null)
            generationCounter.text = "Generation: " + geneticManager.GenerationNumber;
    }

    protected void InitGeneticManager()
    {
        var initialGenerationGenerator = new NeuralInitialGenerationCreatorBase(
			InitNeuralModel(),
            new RecursiveNetworkOpBaker());
            
        var selection = new RouletteWheelSelectionWithRepetion();

        var crossover = new OnePointCrossover(true);
        var breeding = new BreedingClassic(
            crossoverPart,
			minProduction: 1,
			selection: selection,
			crossover: crossover,
			mutationManager: InitMutations()
        );

        var reinsertion = new ReinsertionFromSelection(
			reinsertionPart,
			minProduction: 0,
			selection: new EliteSelection());
        var producers = new IGenomeProducer[] { breeding, reinsertion };
        var genomeForge = new GenomeForge(producers);
      
        var generationManager = new GenerationManagerKeepLast();
        geneticManager = new GeneticManagerClassic(
            generationManager,
            initialGenerationGenerator,
            genomeForge,
            genomesCount
        );

        geneticManager.Init();
    }

	protected abstract INeuralModel InitNeuralModel();

	protected virtual MutationManager InitMutations()
    {
		var result = new MutationManager();
        result.MutationEntries.Add(new MutationEntry(
            new SingleSynapseWeightMutation(() => singleSynapseMutValue),
            singleSynapseMutChance,
            EMutationType.Independent
        ));

        result.MutationEntries.Add(new MutationEntry(
            new SingleSynapseWeightMutation(() => singleSynapseMutValue * 3),
            singleSynapseMutChance / 40,
            EMutationType.Independent
        ));

        result.MutationEntries.Add(new MutationEntry(
            new AllSynapsesWeightMutation(
                () => allSynapsesMutValue,
                allSynapsesMutChanceEach),
            allSynapsesMutChance,
            EMutationType.Independent
        ));

        return result;
    }
	#endregion

	#region Helpers
	protected void ConfigureStaticSettings()
    {
        NeuralGenomeToJSONExtension.distBetweenNodes *= 5;
        NeuralGenomeToJSONExtension.randomPosTries = 10;
        NeuralGenomeToJSONExtension.xPadding = 0.03f;
        NeuralGenomeToJSONExtension.yPadding = 0.03f;

		NeuralNetDrawer.pyGraphDrawerPath = pyNeuralNetGraphDrawerPath;
		NeuralNetDrawer.pyAssemblyCmd = pyAssemblyCmd;
		PyDrawGraph.pyGraphDrawerFilePath = pyFitnessGraphPath;
    }

	private int TimeSinceEpochSeconds()
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return (int)DateTime.Now.Subtract(epoch).TotalSeconds;
	}

	protected IEnumerable<AgentProxy> InitAgents()
    {
		for (int i = 0; i < genomesCount; i++)
		{
			var agent = Instantiate(agentPrefab, transform).GetComponent<AgentProxy>();
			agent.Init(this);
			yield return agent;
		}
    }

	protected void AssignBrains()
    {
        var genomes = geneticManager.GenerationManager
                                    .CurrentGeneration
                                    .Genomes
                                    .Select(x => x as NeuralGenome)
                                    .ToArray();

        UnityEngine.Debug.Assert(agents.Length == genomes.Count());
      
		for (int i = 0; i < genomes.Length; i++)
			agents[i].ResetAgent(agentStartPos.position, genomes[i]);

        startTime = Time.time;
    }

	protected void DrawBestGenome()
    {
        var best = geneticManager.GenerationManager
                                 .CurrentGeneration
                                 .BestGenome as NeuralGenome;
        var str = best.ToJson(
            neuronRadius: 0.02f,
            maxWeight: 5,
            edgeWidth: 1f);

        neuralNetDrawer.QueueNeuralNetJson(str);
    }
    #endregion
}
