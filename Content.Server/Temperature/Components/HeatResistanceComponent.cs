using Content.Server.Clothing.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.Temperature.Components
{
    [RegisterComponent]
    public class HeatResistanceComponent : Component
    {
        public override string Name => "HeatResistance";

        public int GetHeatResistance()
        {
            // TODO: When making into system: Any animal that touches bulb that has no
            // InventoryComponent but still would have default heat resistance in the future (maybe)
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<InventoryComponent?>(Owner, out var inventoryComp))
            {
                // Magical number just copied from below
                return int.MinValue;
            }

            if (inventoryComp.TryGetSlotItem(EquipmentSlotDefines.Slots.GLOVES, out ClothingComponent? gloves))
            {
                return gloves?.HeatResistance ?? int.MinValue;
            }
            return int.MinValue;
        }
    }
}
