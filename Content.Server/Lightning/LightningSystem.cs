﻿using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Random;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<LightningComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, LightningComponent component, ComponentRemove args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam)
            || lightningBeam.VirtualBeamController == null
            || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        beamController.CreatedBeams.Remove(uid);
    }

    private void OnCollide(EntityUid uid, LightningComponent component, StartCollideEvent args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam)
            || lightningBeam.VirtualBeamController == null
            || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        if (component.CanArc)
        {
            while (beamController.CreatedBeams.Count < component.MaxTotalArcs)
            {
                Arc(component, args.OtherFixture.Body.Owner, lightningBeam.VirtualBeamController.Value);

                var spriteState = LightningRandomizer();

                component.ArcTargets.Add(args.OtherFixture.Body.Owner);
                component.ArcTargets.Add(component.ArcTarget);

                _beam.TryCreateBeam(args.OtherFixture.Body.Owner, component.ArcTarget, component.LightningPrototype, spriteState, controller: lightningBeam.VirtualBeamController.Value);

                //Break from this loop so other created bolts can collide and arc
                break;
            }
        }
    }

    /// <summary>
    /// Fires lightning from user to target
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="target">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    public void ShootLightning(EntityUid user, EntityUid target, string lightningPrototype = "Lightning")
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);
    }

    /// <summary>
    /// Looks for a target to arc to in all 8 directions, adds the closest to a local dictionary and picks at random
    /// </summary>
    /// <param name="component"></param>
    /// <param name="target"></param>
    /// <param name="controllerEntity"></param>
    private void Arc(LightningComponent component, EntityUid target, EntityUid controllerEntity)
    {
        if (!TryComp<BeamComponent>(controllerEntity, out var controller))
            return;

        var targetXForm = Transform(target);
        var directions = Enum.GetValues<Direction>().Length;

        var lightningQuery = GetEntityQuery<LightningComponent>();
        var beamQuery = GetEntityQuery<BeamComponent>();

        Dictionary<Direction, EntityUid> arcDirections =  new();

        //TODO: Add scoring system for the Tesla PR which will have grounding rods
        for (int i = 0; i < directions; i++)
        {
            var direction = (Direction) i;
            var dirRad = direction.ToAngle() + targetXForm.GetWorldPositionRotation().WorldRotation;
            var ray = new CollisionRay(targetXForm.GetWorldPositionRotation().WorldPosition, dirRad.ToVec(), component.CollisionMask);
            var rayCastResults = _physics.IntersectRay(targetXForm.MapID, ray, component.MaxLength, target, false).ToList();

            RayCastResults? closestResult = null;

            foreach (var result in rayCastResults)
            {
                if (lightningQuery.HasComponent(result.HitEntity)
                    || beamQuery.HasComponent(result.HitEntity)
                    || component.ArcTargets.Contains(result.HitEntity)
                    || controller.HitTargets.Contains(result.HitEntity)
                    || controller.BeamShooter == result.HitEntity)
                {
                    continue;
                }

                closestResult = result;
            }

            if (closestResult == null)
            {
                continue;
            }

            arcDirections.Add(direction, closestResult.Value.HitEntity);
        }

        var randomDirection = (Direction) _random.Next(0, 7);

        if (arcDirections.ContainsKey(randomDirection))
        {
            component.ArcTarget = arcDirections.GetValueOrDefault(randomDirection);
            arcDirections.Clear();
        }
    }
}
