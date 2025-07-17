using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mech/PartData")]
public class MechPartData : ScriptableObject
{
    public string partCode;
    public List<MoveDefinition> moves;
}
