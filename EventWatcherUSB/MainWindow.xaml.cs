using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// If you enjoy this project, you can support it by making a donation!
// Donation link: https://buymeacoffee.com/_ronniexcoder

namespace EventWatcherUSB
{
 
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private ManagementEventWatcher watcher;
        private WqlEventQuery query;

        public record File
        {
            public string Filename { get; set; }
            public string FileType { get; set; }
        }

        private ObservableCollection<File> Contents { get; set; } = new ObservableCollection<File>();
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {

            string driverName = string.Empty;

            Contents.Clear();

            query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE " +
                "TargetInstance ISA 'Win32_LogicalDisk'");

            using (watcher = new ManagementEventWatcher(query))
            {

                StatusBlock.Text = "Waiting for USB device connection...\n";

                watcher.EventArrived += async (sender, e) =>
                {

                    ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

                    if (instance != null)
                    {


                        DispatcherQueue.TryEnqueue(() =>
                        {
                            StatusBlock.Text += "USB device connected\n";

                            });

                        foreach (var property in instance.Properties)
                        {

                            if (property.Name == "Name") driverName = property.Value.ToString();

                        }

                        await ShowDeviceContentsAsync(driverName);

                    }

                };
            }

            watcher.Start();

        }

        public async Task ShowDeviceContentsAsync(string driverName)
        {
            try
            {

                DriveInfo drive = new DriveInfo(driverName);


                DispatcherQueue.TryEnqueue(() =>
                {
                    Contents.Clear();
                    StatusBlock.Text += $"Contents of the device {drive.Name}:\n";
                });

                DirectoryInfo directory = new DirectoryInfo(driverName);

                await Task.Run(() =>
                {
                    foreach (var item in directory.GetFileSystemInfos())
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            Contents.Add(new File
                            {
                                Filename = item.Name,
                                FileType =
                                (item is DirectoryInfo ? "Folder" : "File")
                            });
                        });
                    }
                });

            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusBlock.Text += $"Error while trying to display device contents: { ex.Message} ";
                    });
            }
        }


        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (watcher != null) watcher.Stop();
            StatusBlock.Text = "";
            Contents.Clear();
        }
        
        // If you enjoy this project, you can support it by making a donation!
        // Donation link: https://buymeacoffee.com/_ronniexcoder
    }
}
