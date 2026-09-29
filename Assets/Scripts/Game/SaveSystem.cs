using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>Salva o personagem localmente. Num MMO de verdade isso fica no servidor.</summary>
    public static class SaveSystem
    {
        const string Key = "aldaria.personagem";

        public static PlayerProfile Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            try
            {
                var p = JsonUtility.FromJson<PlayerProfile>(PlayerPrefs.GetString(Key));
                return p != null && !string.IsNullOrEmpty(p.Name) ? p : null;
            }
            catch
            {
                return null;
            }
        }

        public static void Save(PlayerProfile p)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(p));
            PlayerPrefs.Save();
        }
    }
}
