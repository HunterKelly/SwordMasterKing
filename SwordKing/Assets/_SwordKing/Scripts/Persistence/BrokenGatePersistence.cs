using System;
using UnityEngine;

namespace SwordKing
{
    public partial class BrokenGateLevel
    {
        void Save()
        {
            save.power=Player.power; save.recovery=Player.recovery; save.speed=Player.speed;
            try { PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(save)); PlayerPrefs.Save(); hasSave=true; saveWarning=""; }
            catch(Exception) { saveWarning="Progress could not be saved on this device."; }
        }
    }
}
