﻿#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GodmodeSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly Dictionary<IEntity, OldEntityInformation> _entities = new Dictionary<IEntity, OldEntityInformation>();

        public void Reset()
        {
            _entities.Clear();
        }

        public bool EnableGodmode(IEntity entity)
        {
            if (_entities.ContainsKey(entity))
            {
                return false;
            }

            _entities[entity] = new OldEntityInformation(entity);

            if (entity.HasComponent<MovedByPressureComponent>())
            {
                entity.RemoveComponent<MovedByPressureComponent>();
            }

            if (entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                damageable.AddFlag(DamageFlag.Invulnerable);
            }

            return true;
        }

        public bool HasGodmode(IEntity entity)
        {
            return _entities.ContainsKey(entity);
        }

        public bool DisableGodmode(IEntity entity)
        {
            if (!_entities.Remove(entity, out var old))
            {
                return false;
            }

            old.Restore();

            return true;
        }

        /// <summary>
        ///     Toggles godmode for a given entity.
        /// </summary>
        /// <param name="entity">The entity to toggle godmode for.</param>
        /// <returns>true if enabled, false if disabled.</returns>
        public bool ToggleGodmode(IEntity entity)
        {
            if (HasGodmode(entity))
            {
                DisableGodmode(entity);
                return false;
            }
            else
            {
                EnableGodmode(entity);
                return true;
            }
        }

        public class OldEntityInformation
        {
            public OldEntityInformation(IEntity entity)
            {
                Entity = entity;
                MovedByPressure = entity.GetComponentOrNull<MovedByPressureComponent>();
            }

            public IEntity Entity { get; }
            public MovedByPressureComponent? MovedByPressure { get; }

            public void Restore()
            {
                if (MovedByPressure != null)
                {
                    var newMoved = Entity.EnsureComponent<MovedByPressureComponent>();
                    newMoved.CopyValues(MovedByPressure);
                }

                if (Entity.TryGetComponent(out IDamageableComponent? damageable))
                {
                    damageable.RemoveFlag(DamageFlag.Invulnerable);
                }
            }
        }
    }
}
