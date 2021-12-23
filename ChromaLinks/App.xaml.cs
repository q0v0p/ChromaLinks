using ChromaControl.Shared;

using CUESDK;

using Microsoft.Win32;

using NzxtRGB;

using Razer.Chroma.Broadcast;

using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ChromaLinks
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static RzChromaBroadcastAPI _api;
        private static SmartDeviceV2 _smartDeviceV2;
        private int _corsairDeviceCount;
        private Task _syncTask;
        private bool _isLinkNZXT;
        private bool _isLinkCorsair;
        private ContextMenuStrip _menu;
        private string _nzxtMenuText => _isLinkNZXT ? "NZXTとのリンクを解除する" : "NZXTとリンクする";
        private string _corsairMenuText => _isLinkCorsair ? "Corsairとのリンクを解除する" : "Corsairとリンクする";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var icon = GetResourceStream(new Uri("icon.ico", UriKind.Relative)).Stream;
            _menu = new ContextMenuStrip();
            _menu.Items.Add(_nzxtMenuText, null, NZXTLink_Click);
            _menu.Items.Add(_corsairMenuText, null, CorsairLink_Click);
            _menu.Items.Add("終了", null, Exit_Click);
            var notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = true,
                Icon = new System.Drawing.Icon(icon),
                Text = "ChromaLinks",
                ContextMenuStrip = _menu
            };
            notifyIcon.MouseClick += NotifyIcon_Click;
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(systemEvents_SessionSwitch);
            _syncTask = Task.Factory.StartNew(() => startChromaSync());
        }

        private void startChromaSync()
        {
            if (!Utilities.InitializeEnvironment("ASUS", "DEF05DCE-1662-4D9A-A312-A31028651915"))
                return;

            // Setup NZXT Device
            var config = new NzxtDeviceConfig();
            config.RestartNzxtCamOnFail = true;
            config.RestartNzxtOnClose = true;

            _smartDeviceV2 = SmartDeviceV2.OpenDeviceAsync(config).Result;

            // Setup Corsair Device
            CorsairLightingSDK.PerformProtocolHandshake();

            if (CorsairLightingSDK.GetLastError() != CorsairError.Success)
                Console.WriteLine("Failed to connect to iCUE");

            CorsairLightingSDK.RequestControl(CorsairAccessMode.ExclusiveLightingControl);
            _corsairDeviceCount = CorsairLightingSDK.GetDeviceCount();

            _api = new RzChromaBroadcastAPI();
            _api.ConnectionChanged += _api_ConnectionChanged;
            _api.ColorChanged += _api_ColorChanged;

            try
            {
                _api.Init(Guid.Parse(Utilities.ApplicationGuid));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                throw;
            }
        }

        private void _api_ColorChanged(object sender, RzChromaBroadcastColorChangedEventArgs e)
        {
            if (_isLinkNZXT)
            {
                Color[] channel1Colors = Enumerable.Repeat(e.Colors[0], 16).ToArray();
                Color[] channel2Colors = Enumerable.Repeat(e.Colors[0], 16).ToArray();
                _smartDeviceV2.SendRGB(1, channel1Colors);
                _smartDeviceV2.SendRGB(2, channel2Colors);
            }
            if (_isLinkCorsair)
            {
                try
                {
                    Color[] colors = e.Colors;
                    for (int i = 0; i < _corsairDeviceCount; i++)
                    {
                        CorsairLedPositions deviceLeds = CorsairLightingSDK.GetLedPositionsByDeviceIndex(i);
                        CorsairLedColor[] buffer = new CorsairLedColor[deviceLeds.NumberOfLeds];
                        CorsairLedColor corsairLedColor = default(CorsairLedColor);
                        corsairLedColor.R = colors[0].R;
                        corsairLedColor.G = colors[0].G;
                        corsairLedColor.B = colors[0].B;
                        CorsairLedColor currentColor = corsairLedColor;
                        for (int j = 0; j < deviceLeds.NumberOfLeds; j++)
                        {
                            buffer[j] = currentColor;
                            buffer[j].LedId = deviceLeds.LedPosition[j].LedId;
                        }
                        CorsairLightingSDK.SetLedsColorsBufferByDeviceIndex(i, buffer);
                        CorsairLightingSDK.SetLedsColorsFlushBuffer();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void _api_ConnectionChanged(object sender, RzChromaBroadcastConnectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void NotifyIcon_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {

            }
        }

        private void NZXTLink_Click(object sender, EventArgs e)
        {
            if (_isLinkNZXT)
                _isLinkNZXT = false;
            else
                _isLinkNZXT = true;

            _menu.Items[0].Text = _nzxtMenuText;
        }

        private void CorsairLink_Click(object sender, EventArgs e)
        {
            if (_isLinkCorsair)
                _isLinkCorsair = false;
            else
                _isLinkCorsair = true;

            _menu.Items[1].Text = _corsairMenuText;
        }

        private void systemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            SessionSwitchReason reason = e.Reason;
            if (reason != SessionSwitchReason.SessionLock)
                onLed();
            else
                offLed();
        }

        private void onLed()
        {
            if (CorsairLightingSDK.GetLastError() != CorsairError.Success)
                Console.WriteLine("Failed to connect to iCUE");

            Task.Delay(5000).Wait();
            CorsairLightingSDK.RequestControl(CorsairAccessMode.ExclusiveLightingControl);
            _corsairDeviceCount = CorsairLightingSDK.GetDeviceCount();

            _isLinkCorsair = true;
        }

        private void offLed()
        {
            Color[] channel1Colors;
            Color[] channel2Colors = (channel1Colors = new Color[0]);
            _smartDeviceV2.SendRGB(1, channel1Colors);
            _smartDeviceV2.SendRGB(2, channel2Colors);

            _isLinkCorsair = false;
            CorsairLightingSDK.ReleaseControl(CorsairAccessMode.ExclusiveLightingControl);
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            _smartDeviceV2?.Dispose();
            CorsairLightingSDK.ReleaseControl(CorsairAccessMode.ExclusiveLightingControl);
            Shutdown();
        }
    }
}
