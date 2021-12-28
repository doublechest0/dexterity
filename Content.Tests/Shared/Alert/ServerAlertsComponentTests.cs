using System;
using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Alert
{
    [TestFixture]
    [TestOf(typeof(AlertsComponent))]
    public class ServerAlertsComponentTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alert
  name: AlertLowPressure
  alertType: LowPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/lowpressure.png

- type: alert
  name: AlertHighPressure
  alertType: HighPressure
  category: Pressure
  icon: /Textures/Interface/Alerts/Pressure/highpressure.png
";

        [Test]
        [Ignore("There is no way to load extra Systems in a unit test, fixing RobustUnitTest is out of scope.")]
        public void ShowAlerts()
        {
            // this is kind of unnecessary because there's integration test coverage of Alert components
            // but wanted to keep it anyway to see what's possible w.r.t. testing components
            // in a unit test

            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.RegisterClass<AlertsComponent>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.Resync();

            var entSys = IoCManager.Resolve<IEntitySystemManager>();
            entSys.LoadExtraSystemType<AlertsSystem>();

            var alertsComponent = new AlertsComponent();
            alertsComponent = IoCManager.InjectDependencies(alertsComponent);

            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.LowPressure, out var lowpressure));
            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.HighPressure, out var highpressure));

            EntitySystem.Get<AlertsSystem>().ShowAlert(alertsComponent.Owner, AlertType.LowPressure, null, null);
            var alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.NotNull(alertState);
            Assert.That(alertState.Alerts.Count, Is.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(lowpressure.AlertKey));

            EntitySystem.Get<AlertsSystem>().ShowAlert(alertsComponent.Owner, AlertType.HighPressure, null, null);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts.Count, Is.EqualTo(1));
            Assert.That(alertState.Alerts.ContainsKey(highpressure.AlertKey));

            EntitySystem.Get<AlertsSystem>().ClearAlertCategory(alertsComponent.Owner, AlertCategory.Pressure);
            alertState = alertsComponent.GetComponentState() as AlertsComponentState;
            Assert.That(alertState.Alerts.Count, Is.EqualTo(0));
        }
    }
}
