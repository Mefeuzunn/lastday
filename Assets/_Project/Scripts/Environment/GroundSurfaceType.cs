namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Phase 1 (Step 1C): Five distinct tactical ground surface classifications
    /// for surface-aware footsteps, particle puffs, and acoustic propagation.
    /// </summary>
    public enum GroundSurfaceType
    {
        AsphaltConcrete, // 1.00x baseline acoustic radius
        GrassSoil,       // 0.80x quieter acoustic radius
        WoodInterior,    // 0.90x acoustic radius
        MetalIndustrial, // 1.35x louder acoustic radius
        ShallowWater     // 1.30x acoustic radius
    }
}
