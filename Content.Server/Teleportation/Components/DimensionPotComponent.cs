using Content.Server.Teleportation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Teleportation.Components;

/// <summary>
/// Creates a pocket dimension map on spawn.
/// When activated by alt verb, spawns a portal to this dimension or closes it.
/// </summary>
[RegisterComponent]
[Access(typeof(DimensionPotSystem))]
public sealed class DimensionPotComponent : Component
{
    /// <summary>
    /// The portal on the pot, if it is open right now.
    /// </summary>
    [ViewVariables]
    public EntityUid? PotPortal = null;

    /// <summary>
    /// The portal in the pocket dimension, exists after first opening.
    /// </summary>
    [ViewVariables]
    public EntityUid? DimensionPortal = null;

    /// <summary>
    /// Map of the pocket dimension, exists after first opening.
    /// </summary>
    [ViewVariables]
    public EntityUid? PocketDimensionMap = MapId.Nullspace;

    /// <summary>
    /// Path to the pocket dimension's map file
    /// </summary>
    [DataField("pocketDimensionPath")]
    public ResPath PocketDimensionPath = new ResPath("/Maps/Misc/pocket_dimension.yml");

    /// <summary>
    /// The prototype to spawn for the portal spawned on the pot.
    /// </summary>
    [DataField("potPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PotPortalPrototype = "PortalRed";

    /// <summary>
    /// The prototype to spawn for the portal spawned in the pocket dimension.
    /// </summary>
    [DataField("dimensionPortalPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DimensionPortalPrototype = "PortalBlue";

    [DataField("openPortalSound")]
    public SoundSpecifier OpenPortalSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };

    [DataField("closePortalSound")]
    public SoundSpecifier ClosePortalSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
