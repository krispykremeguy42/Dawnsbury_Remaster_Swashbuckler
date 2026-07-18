using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RemasteredSwashbuckler.RegisteredComponents
{
    public static class RemasteredSwashbucklerFeatNames
    {
        /// <summary>
        /// Rascal Swashbuckler style FeatName
        /// </summary>
        public static readonly FeatName RascalStyle = ModManager.RegisterFeatName("Rascal");
        
        /// <summary>
        /// The new FeatNames for feats which have been added
        /// </summary>
        public static readonly FeatName DastardlyDash = ModManager.RegisterFeatName("Dastardly Dash", "Dastardly Dash {icon:Action}");
        public static readonly FeatName ExtravagantParry = ModManager.RegisterFeatName("Extravagant Parry", "Extravagant Parry {icon:Action}");
        public static readonly FeatName LeadingDance = ModManager.RegisterFeatName("LeadingDanceRemaster", "Leading Dance {icon:Action}");
        public static readonly FeatName FlashyDodge = ModManager.RegisterFeatName("Flashy Dodge", "Flashy Dodge {icon:Reaction}");
        public static readonly FeatName FlashyRoll = ModManager.RegisterFeatName("Flashy Roll", "Flashy Roll");
        public static readonly FeatName RemasterSwashTumbleBehind = ModManager.RegisterFeatName("RemasterSwashTumbleBehind", "Tumble Behind");
    }
}
