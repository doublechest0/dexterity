﻿using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;

namespace Content.Server.GameObjects.Components.NewPower.PowerNetComponents
{
    public abstract class BasePowerNetComponent : BaseNetConnectorComponent<IPowerNet>
    {
        protected override IPowerNet NullNet => PowerNetNodeGroup.NullNet;
    }
}
