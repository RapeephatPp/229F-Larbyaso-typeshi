using UnityEngine;

public class PlayerData 
{
    public static float SavedHP;
    public static int SavedTotalAmmo;
    public static int SavedcurrentAmmo;

    public static bool HasData = false;


    public static void ResetData()
    {
        HasData = false;
    }
}