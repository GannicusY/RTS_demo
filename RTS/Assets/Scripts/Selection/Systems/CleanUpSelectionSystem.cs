using Unity.Entities;

public partial class CleanUpSelectionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        _endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        Entities
            .WithNone<SelectedEntityTag>()
            .ForEach((Entity entity, in SelectionStateData selectionStateData) =>
            {
                ecb.DestroyEntity(selectionStateData.SelectionUI);
                ecb.RemoveComponent<SelectionStateData>(entity);
            }).Run();
    }
}
