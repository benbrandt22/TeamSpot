using System.Drawing;
using System.Windows.Forms;
using TeamSpot.Service.Teams;

namespace TeamSpot.Service.Presentation
{
    internal class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private ITrayStateReader _trayStateReader { get; }
        private readonly Task _readerTask;
        private readonly ToolStripMenuItem _statusMenuItem;
        private readonly SynchronizationContext _uiContext;
        private readonly CancellationTokenSource _cts = new();

        public TrayApplicationContext(ITrayStateReader trayStateReader)
        {
            _trayStateReader = trayStateReader;
            _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No UI synchronization context.");

            var menu = new ContextMenuStrip();

            menu.Items.Add("Teams Monitor", null, null).Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            _statusMenuItem = new ToolStripMenuItem("Status: ...")
            {
                Enabled = false
            };
            menu.Items.Add(_statusMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Exit", null, (_, _) => ExitThread());

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Teams Mute Monitor",
                ContextMenuStrip = menu,
                Visible = true
            };

            // Start consuming channel messages.
            _readerTask = ReadTeamsStateAsync(_cts.Token);
        }

        private async Task ReadTeamsStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var state in _trayStateReader.StateReader.ReadAllAsync(cancellationToken))
                {
                    // Dispatch UI changes to the WinForms thread.
                    BeginInvokeOnUI(() => UpdateTrayMenu(state));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown.
            }
        }

        private void BeginInvokeOnUI(Action action)
        {
            // Implemented using a WinForms synchronization context.
            _uiContext.Post(_ => action(), null);
        }

        private void UpdateTrayMenu(TeamsSimplifiedState state)
        {
            var statusText = state switch
            {
                TeamsSimplifiedState.Offline => "Offline",
                TeamsSimplifiedState.Connecting => "Connecting",
                TeamsSimplifiedState.Connected => "Connected",
                TeamsSimplifiedState.MeetingMutedMic => "In Meeting (Mic Muted)",
                TeamsSimplifiedState.MeetingLiveMic => "In Meeting (Mic Unmuted)",
                _ => "Unknown"
            };

            _statusMenuItem.Text = $"Status: {statusText}";
        }

        protected override void ExitThreadCore()
        {
            _cts.Cancel();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            _cts.Dispose();

            base.ExitThreadCore();
        }
    }
}
