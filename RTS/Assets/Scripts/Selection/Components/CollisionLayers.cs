using System;

[Flags]
public enum CollisionLayers
{
    Selection = 1 << 0,
    Ground = 1 << 1,
    Units = 1 << 2
}
