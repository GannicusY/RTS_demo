using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;

[UpdateAfter(typeof(SelectionSystem))]
public partial class SpawnSelectionSystem : SystemBase
{
    public SelectionUIPrefab SelectionUIPrefab => selectionUIPrefab;

    private SelectionUIPrefab selectionUIPrefab;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    public Color MyColor;

    protected override void OnStartRunning()
    {
        selectionUIPrefab = GetSingleton<SelectionUIPrefab>();
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        MyColor = EntityManager.GetSharedComponentData<RenderMesh>(selectionUIPrefab.Value).material.GetColor("Color_2a356a32c05c4894b68f27d64898eee2");
    }

    protected override void OnUpdate()
    {
        var ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        var selectionPrefab = selectionUIPrefab.Value;
        Entities
            .WithAll<SelectedEntityTag>()
            .WithNone<SelectionStateData>()
            .ForEach((Entity selectedEntity) =>
            {
                var selectionUI = ecb.Instantiate(selectionPrefab);
                var newSelectionStateData = new SelectionStateData()
                {
                    SelectionUI = selectionUI
                };

                ecb.AddComponent(selectedEntity, newSelectionStateData);
                ecb.AddComponent(selectionUI, new Parent { Value = selectedEntity });
                ecb.AddComponent(selectionUI, new LocalToParent { Value = float4x4.zero });
            }).Run();
    }
}
