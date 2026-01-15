using Unity.Netcode;
using UnityEngine;


public class GlobalVariable
{
    public static VariableCellStatesEditInterface GetGlobal()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        {
            return GlobalVariableTemp.instance;
            // return GlobalVariableUse.instance;
        }
        else
        {
            return GlobalVariableTemp.instance;
        }
    }
}
