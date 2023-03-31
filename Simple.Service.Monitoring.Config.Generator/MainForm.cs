using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReaLTaiizor.Forms;
using Simple.Service.Monitoring.Config.Generator.Config;
using Simple.Service.Monitoring.Config.Generator.Infrastructure;
using Simple.Service.Monitoring.Library.Models;
using System.Linq;
using Simple.Service.Monitoring.Library.Options;
using Telegram.Bot.Types;

namespace Simple.Service.Monitoring.Config.Generator
{
    public partial class MainForm : LostForm, IObserver<ServiceHealthCheck>
    {
        private readonly IExtensionValidatorService _extensionValidatorService;
        private readonly Func<ExtensionType, IConfigManipulator<MonitoringWrapper>> _configManipulatorLocator;
        private readonly IServiceProvider _serviceProvider;
        private int EditingCount = 0;


        public MainForm(IExtensionValidatorService extensionValidatorService,
            Func<ExtensionType, IConfigManipulator<MonitoringWrapper>> configManipulatorLocator,
            IServiceProvider serviceProvider)
        {
            _extensionValidatorService = extensionValidatorService;
            _configManipulatorLocator = configManipulatorLocator;
            _serviceProvider = serviceProvider;
            InitializeComponent();

            listView1.MouseDown += new MouseEventHandler(listView1_MouseDown);
            listView1.MouseDoubleClick += new MouseEventHandler(listView1_MouseDoubleClick);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();

            dialog.InitialDirectory = Application.ExecutablePath;
            dialog.Multiselect = false;

            dialog.Filter = "(Yaml Files)|*.yml;*.yaml|(JSON Files)|*.json";
            dialog.FileOk += (o, args) =>
            {
                var fileName = (o as OpenFileDialog)?.FileName;

                if (fileName == null) return;

                var extension = _extensionValidatorService.GetCurrentExtension(fileName);

                using var fileStream = (o as OpenFileDialog)?.OpenFile();

                if (fileStream == null) return;

                var configManipulator = _configManipulatorLocator.Invoke(extension);

                var config = configManipulator.Deserialize(fileStream);

                foreach (var healthCheck in config.Monitoring.HealthChecks)
                {
                    AddHealthCheckToLv(healthCheck);
                }
            };

            dialog.ShowDialog(this);
        }

        private void AddHealthCheckToLv(ServiceHealthCheck healthCheck)
        {
            var name = healthCheck.Name;
            var hostOrWhat = string.Empty;

            var type = Enum.GetName(healthCheck.ServiceType);

            switch (healthCheck.ServiceType)
            {
                case ServiceType.Custom:
                    hostOrWhat = healthCheck.FullClassName;
                    break;
                case ServiceType.Http or ServiceType.ElasticSearch or ServiceType.Ping:
                    hostOrWhat = healthCheck.EndpointOrHost;
                    break;
                case ServiceType.MsSql or ServiceType.Redis or ServiceType.Hangfire or ServiceType.Rmq:
                    hostOrWhat = healthCheck.ConnectionString;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var item = listView1.Items.Add(healthCheck.Name, healthCheck.Name, 0);
            item.SubItems.Add(hostOrWhat);
            item.SubItems.Add(type);
            item.SubItems.Add(healthCheck.Alert ? "Alert enabled" : "Alert disabled");
            item.Tag = healthCheck;
        }


        void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = listView1.HitTest(e.X, e.Y);
            ListViewItem item = info.Item;

            if (item != null)
            {
                var healthCheckEditForm = _serviceProvider.GetRequiredService<HealthCheckForm>();
                healthCheckEditForm.EditHealthCheck(this, item.Tag as ServiceHealthCheck);
                healthCheckEditForm.Subscribe(this);
                EditingCount++;
                healthCheckEditForm.Show();
            }
            else
            {
                this.listView1.SelectedItems.Clear();
                MessageBox.Show("No Item is selected");
            }
        }



        void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = listView1.HitTest(e.X, e.Y);
            ListViewItem item = info.Item;

            if (item != null)
            {

            }
            else
            {

            }
        }

        private void crownListView1_Click(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lostButton2_Click(object sender, EventArgs e)
        {

        }

        private void listView1_Click(object sender, EventArgs e)
        {

        }

        private void lostButton1_Click(object sender, EventArgs e)
        {

        }

        public void OnCompleted()
        {
            EditingCount--;
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(ServiceHealthCheck value)
        {
            var item = listView1
                .Items
                .Find(value.Name, false)
                .FirstOrDefault();
            var name = value.Name;
            var hostOrWhat = string.Empty;

            var type = Enum.GetName(value.ServiceType);

            switch (value.ServiceType)
            {
                case ServiceType.Custom:
                    hostOrWhat = value.FullClassName;
                    break;
                case ServiceType.Http or ServiceType.ElasticSearch or ServiceType.Ping:
                    hostOrWhat = value.EndpointOrHost;
                    break;
                case ServiceType.MsSql or ServiceType.Redis or ServiceType.Hangfire or ServiceType.Rmq:
                    hostOrWhat = value.ConnectionString;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            item.SubItems.Clear();
            item.Text = value.Name;
            item.Name = value.Name;
            item.SubItems.Add(hostOrWhat);
            item.SubItems.Add(type);
            item.SubItems.Add(value.Alert ? "Alert enabled" : "Alert disabled");
            item.Tag = value;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}