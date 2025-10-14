using UnityEngine;
using MechBattle;

public class DroneTest : MonoBehaviour
{
    void Start()
    {
        var catalog = Resources.Load<DroneCatalog>("DroneCatalog");
        catalog.Init();

        // Teste cada comando
        string[] commands = { "titan", "striker", "arclight", "heartcore", "solus", "underdog" };

        foreach (string cmd in commands)
        {
            var drone = catalog.GetByCommandString(cmd);
            if (drone != null)
            {
                Debug.Log($"✅ {cmd} → {drone.droneName} (HP:{drone.statModifiers.HP}, Damage:{drone.moves[0].baseDamage})");
            }
            else
            {
                Debug.LogError($"❌ {cmd} failed!");
            }
        }
    }
}