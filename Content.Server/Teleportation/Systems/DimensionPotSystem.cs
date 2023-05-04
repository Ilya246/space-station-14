using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Teleportation.Systems;

/// <summary>
/// This handles dimension pot portals and maps.
/// </summary>
public sealed class DimensionPotSystem : EntitySystem
{
	[Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
		base.Initialize();
        SubscribeLocalEvent<DimensionPotComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<DimensionPotComponent, GetVerbsEvent<AlternativeVerb>>(AddTogglePortalVerb);

        _sawmill = Logger.GetSawmill("dimension_pot");
    }

    private void OnRemoved(EntityUid uid, DimensionPotComponent comp, ComponentRemove args)
    {
        if (comp.PocketDimensionMap != null)
        {
            // everything inside will be destroyed so this better be indestructible
            QueueDel(comp.PocketDimensionMap);
        }

        if (comp.PotPortal != null)
            QueueDel(comp.PotPortal.Value);
    }

    private void AddTogglePortalVerb(EntityUid uid, DimensionPotComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("dimension-pot-verb-text"),
            Act = () => HandleActivation(uid, comp, args.User)
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Creates or removes the portals to the pocket dimension.
    /// </summary>
    private void HandleActivation(EntityUid uid, DimensionPotComponent comp, EntityUid user)
    {
		if (comp.PocketDimensionMap == null)
		{
			var map = _mapMan.CreateMap();
			if (!_mapLoader.TryLoad(map, comp.PocketDimensionPath.ToString(), out var roots))
			{
				_sawmill.Error($"Failed to load pocket dimension map {comp.PocketDimensionPath}");
				QueueDel(uid);
				return;
			}

            comp.PocketDimensionMap = _mapMan.GetMapEntityId(map);
			if (TryComp<GravityComponent>(comp.PocketDimensionMap, out var gravity))
				gravity.Enabled = true;

			// find the pocket dimension's first grid and put the portal there
			bool foundGrid = false;
			foreach (var root in roots)
			{
				if (!HasComp<MapGridComponent>(root))
					continue;

				// spawn the permanent portal into the pocket dimension, now ready to be used
				var pos = Transform(root).Coordinates;
				comp.DimensionPortal = Spawn(comp.DimensionPortalPrototype, pos);
				_sawmill.Info($"Created pocket dimension on grid {root} of map {map}");

				// if someone closes your portal you can use the one inside to escape
				_link.TryLink(uid, comp.DimensionPortal.Value);
				foundGrid = true;
			}
			if (!foundGrid)
			{
				_sawmill.Error($"Pocket dimension {comp.PocketDimensionPath} had no grids!");
				QueueDel(uid);
				return;
			}
		}

        var dimension = comp.DimensionPortal!.Value;
        if (comp.PotPortal != null)
        {
            // portal already exists so unlink and delete it
            _link.TryUnlink(dimension, comp.PotPortal.Value);
            QueueDel(comp.PotPortal.Value);
            comp.PotPortal = null;
			_audio.PlayPvs(comp.ClosePortalSound, uid);

            // if you are stuck inside the pocket dimension you can use the internal portal to escape
            _link.TryLink(uid, dimension);
        }
        else
        {
            // create a portal and link it to the pocket dimension
            comp.PotPortal = Spawn(comp.PotPortalPrototype, Transform(uid).Coordinates);
            _link.TryLink(dimension, comp.PotPortal.Value);
            _transform.SetParent(comp.PotPortal.Value, uid);
			_audio.PlayPvs(comp.OpenPortalSound, uid);

            _link.TryUnlink(uid, dimension);
        }
    }
}
