using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Materials;
using Content.Server.Radiation.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles logic and interactions relating to <see cref="AnomalyComponent"/>
/// </summary>
public sealed partial class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public const float MinParticleVariation = 0.8f;
    public const float MaxParticleVariation = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, StartCollideEvent>(OnStartCollide);

        InitializeGenerator();
        InitializeScanner();
        InitializeVessel();
        InitializeCommands();
        InitializeBehaviour();
    }

    private void OnMapInit(Entity<AnomalyComponent> anomaly, ref MapInitEvent args)
    {
        anomaly.Comp.NextPulseTime = Timing.CurTime + GetPulseLength(anomaly.Comp) * 3; // longer the first time
        ChangeAnomalyStability(anomaly, Random.NextFloat(anomaly.Comp.InitialStabilityRange.Item1 , anomaly.Comp.InitialStabilityRange.Item2), anomaly.Comp);
        ChangeAnomalySeverity(anomaly, Random.NextFloat(anomaly.Comp.InitialSeverityRange.Item1, anomaly.Comp.InitialSeverityRange.Item2), anomaly.Comp);

        ShuffleParticlesEffect(anomaly.Comp);
        anomaly.Comp.Continuity = _random.NextFloat(anomaly.Comp.MinContituty, anomaly.Comp.MaxContituty);
        SetBehaviour(anomaly, GetRandomBehaviour());
    }

    public void ShuffleParticlesEffect(AnomalyComponent anomaly)
    {
        var particles = new List<AnomalousParticleType>
            { AnomalousParticleType.Delta, AnomalousParticleType.Epsilon, AnomalousParticleType.Zeta, AnomalousParticleType.Sigma };

        anomaly.SeverityParticleType = Random.PickAndTake(particles);
        anomaly.DestabilizingParticleType = Random.PickAndTake(particles);
        anomaly.WeakeningParticleType = Random.PickAndTake(particles);
        anomaly.TransformationParticleType = Random.PickAndTake(particles);
    }

    private void OnShutdown(Entity<AnomalyComponent> anomaly, ref ComponentShutdown args)
    {
        EndAnomaly(anomaly);
    }

    private void OnStartCollide(Entity<AnomalyComponent> anomaly, ref StartCollideEvent args)
    {
        if (!TryComp<AnomalousParticleComponent>(args.OtherEntity, out var particle))
            return;

        if (args.OtherFixtureId != particle.FixtureId)
            return;

        var behaviourMod = 1f;
        if (anomaly.Comp.CurrentBehaviour != null)
        {
            var b = _prototype.Index(anomaly.Comp.CurrentBehaviour.Value);
            behaviourMod = b.ParticleSensivity;
        }
        // small function to randomize because it's easier to read like this
        float VaryValue(float v) => v * behaviourMod * Random.NextFloat(MinParticleVariation, MaxParticleVariation);

        if (particle.ParticleType == anomaly.Comp.DestabilizingParticleType || particle.DestabilzingOverride)
        {
            ChangeAnomalyStability(anomaly, VaryValue(particle.StabilityPerDestabilizingHit), anomaly.Comp);
        }
        if (particle.ParticleType == anomaly.Comp.SeverityParticleType || particle.SeverityOverride)
        {
            ChangeAnomalySeverity(anomaly, VaryValue(particle.SeverityPerSeverityHit), anomaly.Comp);
        }
        if (particle.ParticleType == anomaly.Comp.WeakeningParticleType || particle.WeakeningOverride)
        {
            ChangeAnomalyHealth(anomaly, VaryValue(particle.HealthPerWeakeningeHit), anomaly.Comp);
            ChangeAnomalyStability(anomaly, VaryValue(particle.StabilityPerWeakeningeHit), anomaly.Comp);
        }
        if (particle.ParticleType == anomaly.Comp.TransformationParticleType || particle.TransmutationOverride)
        {
            ChangeAnomalySeverity(anomaly, VaryValue(particle.SeverityPerSeverityHit), anomaly.Comp);
            if (_random.Prob(anomaly.Comp.Continuity))
            {
                SetBehaviour(anomaly, GetRandomBehaviour());
                RefreshPulseTimer(anomaly);
            }
        }
    }

    /// <summary>
    /// Gets the amount of research points generated per second for an anomaly.
    /// </summary>
    /// <param name="anomaly"></param>
    /// <param name="component"></param>
    /// <returns>The amount of points</returns>
    public int GetAnomalyPointValue(EntityUid anomaly, AnomalyComponent? component = null)
    {
        if (!Resolve(anomaly, ref component, false))
            return 0;

        var multiplier = 1f;
        if (component.Stability > component.GrowthThreshold)
            multiplier = component.GrowingPointMultiplier; //more points for unstable

        //penalty of up to 50% based on health
        multiplier *= MathF.Pow(1.5f, component.Health) - 0.5f;

        //Apply behaviour modifier
        if (component.CurrentBehaviour != null)
        {
            var behaviour = _prototype.Index(component.CurrentBehaviour.Value);
            multiplier *= behaviour.EarnPointModifier;
        }

        var severityValue = 1 / (1 + MathF.Pow(MathF.E, -7 * (component.Severity - 0.5f)));

        return (int) ((component.MaxPointsPerSecond - component.MinPointsPerSecond) * severityValue * multiplier) + component.MinPointsPerSecond;
    }

    /// <summary>
    /// Gets the localized name of a particle.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetParticleLocale(AnomalousParticleType type)
    {
        return type switch
        {
            AnomalousParticleType.Delta => Loc.GetString("anomaly-particles-delta"),
            AnomalousParticleType.Epsilon => Loc.GetString("anomaly-particles-epsilon"),
            AnomalousParticleType.Zeta => Loc.GetString("anomaly-particles-zeta"),
            AnomalousParticleType.Sigma => Loc.GetString("anomaly-particles-sigma"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGenerator();
        UpdateVessels();
    }
}
