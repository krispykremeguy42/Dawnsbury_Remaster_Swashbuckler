using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.RemasteredSwashbuckler.RegisteredComponents;

public static class RemasteredSwashbucklerTraits
{
    /// <summary>
    /// The Bravado Trait
    /// </summary>
    public static readonly Trait BravadoTrait = ModManager.RegisterTrait("Bravado", new TraitProperties("Bravado", true, "Actions with the bravado trait generate panache on anything other than a critical failure, although on a failure, panache only lasts until the end of your turn.", true));
}
