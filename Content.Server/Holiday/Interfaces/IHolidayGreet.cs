using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.Holiday.Interfaces
{
    public interface IHolidayGreet : IExposeData
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }
        string Greet(HolidayPrototype holiday);
    }
}
