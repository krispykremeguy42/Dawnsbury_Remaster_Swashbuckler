using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Modding;
using System.Collections.Generic;
using Dawnsbury.Mods.Phoenix;

namespace Dawnsbury.Mods.RemasteredSwashbuckler;

/// <summary>
/// Updates and loads the Remastered changes into the game for the Swashbuckler
/// </summary>
public class RemastereSwashbucklerLoader
{
    /// <summary>
    /// Runs on launch and patches the feats introduced by the legacy Swashbuckler
    /// </summary>
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        AllFeats.All.RemoveAll(RemasteredSwashbuckler.ShouldFeatBeRemoved);
        foreach (Feat feat in RemasteredSwashbuckler.CreateRemasteredSwashbucklerFeats())
        {
            ModManager.AddFeat(feat);
        }
        AllFeats.All.ForEach(RemasteredSwashbuckler.PatchSwashFeats);
    }
}