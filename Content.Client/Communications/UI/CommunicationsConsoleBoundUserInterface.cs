using System;
using Content.Shared.Communications;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables] private CommunicationsConsoleMenu? _menu;

        public bool CanAnnounce { get; private set; }
        public bool CanCall { get; private set; }

        public bool CountdownStarted { get; private set; }

        public bool AlertLevelSelectable { get; private set; }

        public string CurrentLevel { get; private set; } = default!;

        public float Countdown => _expectedCountdownTime == null ? 0.0f : (float)Math.Max(_expectedCountdownTime.Value.Subtract(_gameTiming.CurTime).TotalSeconds, 0);
        private TimeSpan? _expectedCountdownTime;

        public CommunicationsConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            _menu = new CommunicationsConsoleMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void AlertLevelSelected(string level)
        {
            if (AlertLevelSelectable)
            {
                CurrentLevel = level;
                SendMessage(new CommunicationsConsoleSelectAlertLevelMessage(level));
            }
        }

        public void EmergencyShuttleButtonPressed()
        {
            if (CountdownStarted)
                RecallShuttle();
            else
                CallShuttle();
        }

        public void AnnounceButtonPressed(string message)
        {
            var msg = (message.Length <= 256 ? message.Trim() : $"{message.Trim().Substring(0, 256)}...").ToCharArray();

            // No more than 2 newlines, other replaced to spaces
            var newlines = 0;
            for (var i = 0; i < msg.Length; i++)
            {
                if (msg[i] != '\n')
                    continue;

                if (newlines >= 2)
                    msg[i] = ' ';

                newlines++;
            }

            if (msg.Length > 0)
            {
                SendMessage(new CommunicationsConsoleAnnounceMessage(new string(msg)));
            }
        }

        public void CallShuttle()
        {
            SendMessage(new CommunicationsConsoleCallEmergencyShuttleMessage());
        }

        public void RecallShuttle()
        {
            SendMessage(new CommunicationsConsoleRecallEmergencyShuttleMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CommunicationsConsoleInterfaceState commsState)
                return;

            CanAnnounce = commsState.CanAnnounce;
            CanCall = commsState.CanCall;
            _expectedCountdownTime = commsState.ExpectedCountdownEnd;
            CountdownStarted = commsState.CountdownStarted;
            AlertLevelSelectable = commsState.AlertLevels != null && !float.IsNaN(commsState.CurrentAlertDelay) && commsState.CurrentAlertDelay <= 0;
            CurrentLevel = commsState.CurrentAlert;

            if (_menu != null)
            {
                //<todo.eoin Tidy this up
                _menu.ConsoleName.Text = Loc.GetString(commsState.CommsConsoleName);
                _menu.UpdateCountdown();
                _menu.UpdateAlertLevels(commsState.AlertLevels, commsState.AlertColors, CurrentLevel);
                _menu.AlertLevelSelectable = AlertLevelSelectable;
                _menu.EmergencyShuttleCallButton.Disabled = !(CanCall && !CountdownStarted);
                _menu.EmergencyShuttleRecallButton.Disabled = !(CanCall && CountdownStarted);
                _menu.AnnounceButton.Disabled = !CanAnnounce;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _menu?.Dispose();
        }
    }
}
