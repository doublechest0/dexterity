using System.Collections.Generic;
using System.Linq;
using Content.Shared.Singularity.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Singularity
{
    public sealed class SingularityOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     Maximum number of distortions that can be shown on screen at a time.
        ///     If this value is changed, the shader itself also needs to be updated.
        /// </summary>
        public const int MaxCount = 5;

        private const float MaxDist = 15.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _shader;
        private readonly Dictionary<EntityUid, SingularityShaderInstance> _singularities = new();

        public SingularityOverlay()
        {
            IoCManager.InjectDependencies(this);
            _shader = _prototypeManager.Index<ShaderPrototype>("Singularity").Instance().Duplicate();
        }

        public override bool OverwriteTargetFrameBuffer()
        {
            return _singularities.Count > 0;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            SingularityQuery(args.Viewport.Eye);

            var viewportWB = args.WorldAABB;
            // Has to be correctly handled because of the way intensity/falloff transform works so just do it.
            _shader?.SetParameter("renderScale", args.Viewport.RenderScale);

            var position = new Vector2[MaxCount];
            var intensity = new float[MaxCount];
            var falloffPower = new float[MaxCount];
            int index = 0;
            foreach (var instance in _singularities.Values)
            {
                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = args.Viewport.WorldToLocal(instance.CurrentMapCoords);
                tempCoords.Y = args.Viewport.Size.Y - tempCoords.Y;

                position[index] = tempCoords;
                intensity[index] = instance.Intensity;
                falloffPower[index] = instance.FalloffPower;
                index++;

                if (index == MaxCount)
                    break;
            }

            _shader?.SetParameter("count", index);
            _shader?.SetParameter("position", position);
            _shader?.SetParameter("intensity", intensity);
            _shader?.SetParameter("falloffPower", falloffPower);
            _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(viewportWB, Color.White);
            worldHandle.UseShader(null);
        }

        //Queries all singulos on the map and either adds or removes them from the list of rendered singulos based on whether they should be drawn (in range? on the same z-level/map? singulo entity still exists?)
        private void SingularityQuery(IEye? currentEye)
        {
            if (currentEye == null)
            {
                _singularities.Clear();
                return;
            }

            var currentEyeLoc = currentEye.Position;

            var distortions = _entityManager.EntityQuery<SingularityDistortionComponent>();
            foreach (var distortion in distortions) //Add all singulos that are not added yet but qualify
            {
                var singuloEntity = distortion.Owner;

                if (!_singularities.Keys.Contains(singuloEntity) && SinguloQualifies(singuloEntity, currentEyeLoc))
                {
                    _singularities.Add(singuloEntity, new SingularityShaderInstance(_entityManager.GetComponent<TransformComponent>(singuloEntity).MapPosition.Position, distortion.Intensity, distortion.FalloffPower));
                }
            }

            var activeShaderIds = _singularities.Keys;
            foreach (var activeSingulo in activeShaderIds) //Remove all singulos that are added and no longer qualify
            {
                if (_entityManager.EntityExists(activeSingulo))
                {
                    if (!SinguloQualifies(activeSingulo, currentEyeLoc))
                    {
                        _singularities.Remove(activeSingulo);
                    }
                    else
                    {
                        if (!_entityManager.TryGetComponent<SingularityDistortionComponent?>(activeSingulo, out var distortion))
                        {
                            _singularities.Remove(activeSingulo);
                        }
                        else
                        {
                            var shaderInstance = _singularities[activeSingulo];
                            shaderInstance.CurrentMapCoords = _entityManager.GetComponent<TransformComponent>(activeSingulo).MapPosition.Position;
                            shaderInstance.Intensity = distortion.Intensity;
                            shaderInstance.FalloffPower = distortion.FalloffPower;
                        }
                    }

                }
                else
                {
                    _singularities.Remove(activeSingulo);
                }
            }

        }

        private bool SinguloQualifies(EntityUid singuloEntity, MapCoordinates currentEyeLoc)
        {
            return _entityManager.GetComponent<TransformComponent>(singuloEntity).MapID == currentEyeLoc.MapId && _entityManager.GetComponent<TransformComponent>(singuloEntity).Coordinates.InRange(_entityManager, EntityCoordinates.FromMap(_entityManager, _entityManager.GetComponent<TransformComponent>(singuloEntity).ParentUid, currentEyeLoc), MaxDist);
        }

        private sealed class SingularityShaderInstance
        {
            public Vector2 CurrentMapCoords;
            public float Intensity;
            public float FalloffPower;

            public SingularityShaderInstance(Vector2 mapCoords, float intensity, float falloffPower)
            {
                CurrentMapCoords = mapCoords;
                Intensity = intensity;
                FalloffPower = falloffPower;
            }
        }
    }
}

