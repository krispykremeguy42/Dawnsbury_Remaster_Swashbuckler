using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.RemasteredSwashbuckler.RegisteredComponents
{
    public static class RemasteredSwashbucklerFeatNames
    {
        /// <summary>
        /// The new FeatNames for feats which have been added
        /// </summary>
        public static readonly FeatName ExtravagantParry = ModManager.RegisterFeatName("Extravagant Parry", "Extravagant Parry {icon:Action}");
        public static readonly FeatName LeadingDance = ModManager.RegisterFeatName("LeadingDanceRemaster", "Leading Dance {icon:Action}");
        public static readonly FeatName FlashyDodge = ModManager.RegisterFeatName("Flashy Dodge", "Flashy Dodge {icon:Reaction}");
        public static readonly FeatName FlashyRoll = ModManager.RegisterFeatName("Flashy Roll", "Flashy Roll");
        public static readonly FeatName RemasterSwashTumbleBehind = ModManager.RegisterFeatName("RemasterSwashTumbleBehind", "Tumble Behind");
    }
}
