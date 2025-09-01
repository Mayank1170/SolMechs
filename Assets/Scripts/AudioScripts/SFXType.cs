namespace SolMechs.Audio
{
    /// <summary>
    /// Enum defining all sound effect types used in SolMechs game
    /// </summary>
    public enum SFXType
    {
        // UI Sounds
        ButtonClick,
        ButtonHover,
        MenuTransition,

        // Combat Sounds
        MechHit,
        MechDestroyed,
        AttackPhysical,
        AttackEnergy,

        // System Sounds
        WalletConnect,
        MechAssemble,
        Victory,
        Defeat,

        // Module/Part Sounds
        ModuleEquip,
        ModuleRemove,
        BuffApply,
        DebuffApply,

        // Pack Opening Sounds
        PackOpen,
        RareCardReveal,

        // Ambient/Environment
        MenuAmbient,
        BattleAmbient
    }
}