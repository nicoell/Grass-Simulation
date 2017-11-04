using GrassSimulation;
using UnityEngine;

public class UniqueGrassData
{
    public UniqueGrassData(SimulationSettings settings, Texture2D heightMap, TerrainData terrainData, int startIndex)
    {
    }

    public Vector4[] GrassDataA { get; private set; } //bladeUp.xyz, position.y
    public Vector4[] GrassDataB { get; private set; } //bladeV1.xyz, height
    public Vector4[] GrassDataC { get; private set; } //bladeV2.xyz, dirAlpha
}