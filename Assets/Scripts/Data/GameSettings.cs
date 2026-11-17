using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Battleground X/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Match")]
    public int   MaxPlayers        = 10;
    public float MatchDuration     = 900f;
    public float LobbyWait         = 15f;
    public float CountdownTime     = 5f;
    public float ReturnToLobbyDelay = 10f;
    public int   MinPlayersToStart = 2;

    [Header("Player")]
    public int   StartHealth       = 100;
    public float WalkSpeed         = 4.5f;
    public float RunSpeed          = 7f;
    public float SprintSpeed       = 10f;
    public float CrouchSpeed       = 2.5f;
    public float JumpForce         = 7f;
    public float Gravity           = -20f;
    public float MouseSensitivity  = 2f;

    [Header("Combat")]
    public int   AssaultRifleDmg   = 25;
    public int   ShotgunDmg        = 60;
    public int   SniperDmg         = 90;
    public float HitscanRange      = 150f;

    [Header("Inventory")]
    public int   StartRifleAmmo    = 60;
    public int   MaxRifleAmmo      = 180;
    public int   MaxShotgunAmmo    = 56;
    public int   MaxSniperAmmo     = 20;
    public int   MaxMedkits        = 5;
    public int   MedkitHeal        = 40;

    [Header("Safe Zone")]
    public float ZoneStartRadius   = 180f;
    public float ZoneFinalRadius   = 3f;

    [Header("Loot")]
    public float LootSpawnDelay    = 2f;
    public float LootRespawnChance = 0.3f;
    public float LootRespawnInterval = 60f;

    [Header("Network")]
    public int   TickRate          = 30;
    public int   MaxConnections    = 15;
}