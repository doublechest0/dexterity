using Content.Shared.Humanoid;
using Content.Shared.Markings;

namespace Content.Client.Humanoid;

public sealed class HumanoidComponent : SharedHumanoidComponent
{
    public List<Marking> CurrentMarkings = new();
}
