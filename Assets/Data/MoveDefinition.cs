using UnityEngine;

[System.Serializable]
public class MoveDefinition
{
    public string moveName;
    public int baseDamage;
    public string damageType; // Substitua por enum DamageType se definido
    public TargetType targetType;
    public string effect;
}