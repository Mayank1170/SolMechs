using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MoveOptionsDatabase", menuName = "MechBattle/Move Options Database")]
public class MoveOptionsDatabase : ScriptableObject
{
    public List<string> types = new List<string>()
    {
        "Physical", "Special", "Energy", "Support"
    };

    public List<string> categories = new List<string>()
    {
        "Ranged", "Contact", "AOE", "Buff", "Debuff", "Self"
    };

    public List<string> effects = new List<string>()
    {
        "Stun (1 turn)",
        "Silence (1 turn)",
        "AOE damage",
        "+20% ATK (2 turns)",
        "+20% ENG (2 turns)",
        "-10 SPD (2 turns)",
        "-15 SYS (2 turns)",
        "Cleansing",
        "Swap turn order",
        "Ignore 50% DEF",
        "Reflect damage",
        "Shield next hit"
    };
}
