using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;

    private BlobAssetStore bas;
    private GameObjectConversionSettings gocs;
    private EntityManager entityManager;

    [SerializeField]
    private float3 spawnOffset = new float3(0, 1, 0);

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        bas = new BlobAssetStore();
        gocs = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, bas);
    }

    private void Start()
    {    
        var entities = new NativeArray<Entity>(1, Allocator.Temp);

        var convertedEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, gocs);

        entityManager.Instantiate(convertedEntity, entities);

        for (var i = 0; i < entities.Length; ++i)
        {
            //entityManager.AddComponentData(entities[i], new NavAgent
            //{
            //    TranslationSpeed = 20,
            //    ObstacleAversionDistance = 4f,
            //    AgentAversionDistance = 3f,
            //    SeparationPerceptionRadius = 1f,
            //    AlignmentPerceptionRadius = 3f,
            //    CohesionPerceptionRadius = 3f,
            //    RotationSpeed = 0.3f,
            //    TypeID = NavUtil.GetAgentType(NavConstants.HUMANOID),
            //    Offset = new float3(0, 1f, 0)
            //});

            //entityManager.AddComponentData<LocalToWorld>(entities[i], new LocalToWorld
            //{
            //    Value = float4x4.TRS(
            //        new float3(transform.position.x, 1f, transform.position.z),
            //        quaternion.identity,
            //        1
            //    )
            //});

            //entityManager.AddComponent<LocalToWorld>(entities[i]);
            //entityManager.AddComponent<Parent>(entities[i]);
            ////entityManager.AddComponent<LocalToParent>(entities[i]);
            //entityManager.AddComponent<NavNeedsSurface>(entities[i]);
            //entityManager.AddComponent<NavTerrainCapable>(entities[i]);
            //entityManager.AddComponent<NavFlocking>(entities[i]);
            //entityManager.AddComponent<NavObstacleSteering>(entities[i]);
            
            entityManager.SetName(entities[i], "TEST");
        }

        entities.Dispose();
        
    }

    private void OnDestroy()
    {
        bas.Dispose();
    }
}
