using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SelectMultipleUnitsJobSystem : SystemBase
{
    private StepPhysicsWorld stepPhysicsWorld;
    //private BuildPhysicsWorld buildPhysicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem endFixedECBSystem;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<SelectionColliderTag>();

        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        // buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        endFixedECBSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = endFixedECBSystem.CreateCommandBuffer();

        var jobHandle = new SelectionJob
        {
            SelectionVolumes = GetComponentDataFromEntity<SelectionColliderTag>(),
            Units = GetComponentDataFromEntity<SelectableUnitTag>(),
            ECB = ecb
        }.Schedule(stepPhysicsWorld.Simulation, Dependency);

        jobHandle.Complete();

        var selectionEntity = GetSingletonEntity<SelectionColliderTag>();

        if (HasComponent<StepsToLiveData>(selectionEntity))
        {
            var stepsToLive = GetComponent<StepsToLiveData>(selectionEntity);
            stepsToLive.Value--;
            ecb.SetComponent(selectionEntity, stepsToLive);
            if (stepsToLive.Value <= 0)
            {
                ecb.DestroyEntity(selectionEntity);
            }
        }
        else
        {
            ecb.AddComponent<StepsToLiveData>(selectionEntity);
            ecb.SetComponent(selectionEntity, new StepsToLiveData { Value = 1 });
        }
    }
}

public struct SelectionJob : ITriggerEventsJob
{
    public ComponentDataFromEntity<SelectionColliderTag> SelectionVolumes;
    public ComponentDataFromEntity<SelectableUnitTag> Units;
    public EntityCommandBuffer ECB;

    public void Execute(TriggerEvent triggerEvent)
    {
        var entityA = triggerEvent.EntityA;
        var entityB = triggerEvent.EntityB;

        var isBodyASelection = SelectionVolumes.HasComponent(entityA);
        var isBodyBSelection = SelectionVolumes.HasComponent(entityB);

        if (isBodyASelection && isBodyBSelection)
        {
            return;
        }

        var isBodyAUnit = Units.HasComponent(entityA);
        var isBodyBUnit = Units.HasComponent(entityB);

        if ((isBodyASelection && !isBodyBUnit) || (isBodyBSelection && !isBodyAUnit))
        {
            return;
        }

        var selectedUnit = isBodyASelection ? entityB : entityA;
        ECB.AddComponent<SelectedEntityTag>(selectedUnit);
    }

}
