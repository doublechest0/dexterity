using Content.Server.Actions;
using Content.Server.Body.Components;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphedEntitySystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphedEntityComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PolymorphedEntityComponent, AfterPolymorphEvent>(OnStartup);
            SubscribeLocalEvent<PolymorphedEntityComponent, RevertTransformationActionEvent>(Revert);
        }

        public void Revert(EntityUid uid, PolymorphedEntityComponent component, RevertTransformationActionEvent args)
        {
            for(int i = 0; i < component.ParentContainer.ContainedEntities.Count; i++)
            {
                var entity = component.ParentContainer.ContainedEntities[i];
                component.ParentContainer.Remove(entity);

                if(entity == component.Parent)
                {
                    if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
                    {
                        mind.Mind.TransferTo(entity);
                    }
                }
            }
            QueueDel(uid);
        }

        private void OnStartup(EntityUid uid, PolymorphedEntityComponent component, AfterPolymorphEvent args)
        {
            if (component.Forced)
                return;
            
            var act = new InstantAction();

            act.Event = new RevertTransformationActionEvent();
            act.Name = "wacka wacka";
            act.Description = "pac man";

            _actions.AddAction(uid, act, null);
        }

        private void OnComponentInit(EntityUid uid, PolymorphedEntityComponent component, ComponentInit args)
        {
            component.ParentContainer = _container.EnsureContainer<Container>(uid, component.Name);
        }
    }
}

public sealed class RevertTransformationActionEvent : InstantActionEvent { };
