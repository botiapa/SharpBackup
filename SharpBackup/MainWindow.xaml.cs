using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.Drawing;

namespace SharpBackup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Grid> contentGrids;
        private BackupManager bm;
        private readonly string templateBackupString = "{0},{1},{2},1/1/0001 12:00:00 AM";
        private ObservableCollection<backupListRow> backupListContents;
        private TaskbarIcon taskbarIcon;

        public MainWindow()
        {
            InitializeComponent();

            // Set up the content grids.
            contentGrids = new List<Grid>
            {
                StatusGrid,
                BackupsGrid
            };

            // show only the status content grid.
            hideAllContentGrids();
            contentGrids[0].Visibility = Visibility.Visible;

            // Set up the backupmanager.
            bm = new BackupManager(this);
            Thread bmthr = new Thread(() => bm.MainAsync());
            bmthr.Priority = ThreadPriority.AboveNormal;
            bmthr.Start();

            // Set up backuplist
            backupListContents = new ObservableCollection<backupListRow>();
            backupList.ItemsSource = backupListContents;

            //Set up taskbar icon.
            taskbarIcon = new TaskbarIcon();
            taskbarIcon.ToolTip = "SharpBackup";
            taskbarIcon.Icon = SystemIcons.Application;
            taskbarIcon.Visibility = Visibility.Collapsed;
            taskbarIcon.DoubleClickCommand = new RelayCommand(this);

            bm.backups.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                ObservableCollection<backupListRow> newBackupListContents = new ObservableCollection<backupListRow>();

                for (int i=0; i < bm.backups.Count; i++)
                {
                    var item = bm.backups[i];
                    newBackupListContents.Add(new backupListRow() { Name = item.name, Path = item.url, LastBackup = item.lastBackup.ToLongDateString(), Status = item.StatusString, Progress = item.progressString, SizeProgress = item.sizeProgress, AllProgress = item.allProgress, paused=item.paused });
                }

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () {
                    backupListContents.Clear();
                    for (int i = 0; i < newBackupListContents.Count; i++)
                    {
                        backupListContents.Add(newBackupListContents[i]);
                        /*Button pauseButton = (Button)((ListViewItem)backupList.Items.GetItemAt(i)).FindName("pauseButton");
                        if  (newBackupListContents[i].paused)
                        {
                            pauseButton.Content = "Resume";
                        }
                        else
                        {
                            pauseButton.Content = "Pause";
                        }*/
                    }
                    statusText.Text = String.Format("Loaded {0} backups.", bm.backups.Count());
                }));
                
                
                
            };
        }

        private class RelayCommand : ICommand
        {
            private MainWindow main;
            public RelayCommand(MainWindow main)
            {
                this.main = main;
            }
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                main.trayIconClick();
            }
        }

        private void onLogoMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private string getStatusString(Backup backup)
        {
            string status = String.Empty;
            if (backup.isBackupRunning) { status = "Running"; }
            else { status = "Idle"; }
            return status;
        }

        #region BasicButtons
        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
            taskbarIcon.Visibility = Visibility.Visible;

            Visibility = Visibility.Collapsed;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            bm.stopBackupManager();
            Application.Current.Shutdown();
        }
        #endregion

        #region SideBarButtons
        private void hideAllContentGrids()
        {
            for(int i=0;i < contentGrids.Count;i++)
            {
                contentGrids[i].Visibility = Visibility.Collapsed;
            }
        }

        private void sideBarButton1_Click(object sender, RoutedEventArgs e)
        {
            hideAllContentGrids();
            contentGrids[0].Visibility = Visibility.Visible;
        }

        private void sideBarButton2_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNewBackupDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.Success)
            {
                bm.backups.Add(new Backup(String.Format(templateBackupString, dialog.nameTextBox.Text, dialog.locationTextBox.Text, (Int32.Parse(dialog.delayTextBox.Text) * 60).ToString())));
            }
        }

        private void sideBarButton3_Click(object sender, RoutedEventArgs e)
        {
            if (backupList.SelectedItem == null)
            {
                MessageBox.Show(this, "You need to select an item", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int index = backupList.SelectedIndex;
            var dialog = new AddNewBackupDialog();
            // Customize the dialog.
            dialog.Title.Content = "Edit the backup";
            Backup backup = bm.backups[index];
            dialog.nameTextBox.Text = backup.name;
            dialog.locationTextBox.Text = backup.url;
            dialog.delayTextBox.Text = (backup.backupDelay / 60).ToString();

            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.Success)
            {
                // Refresh the variable(It could've changed because of the threads.)
                backup = bm.backups[index];
                // Update the backup with the new properties.
                backup.name = dialog.nameTextBox.Text;
                backup.url = dialog.locationTextBox.Text;
                backup.backupDelay = int.Parse(dialog.delayTextBox.Text) * 60;
                bm.backups[index] = backup;
                bm.SaveBackupFile();
            }
        }

        private void sideBarButton4_Click(object sender, RoutedEventArgs e)
        {
            if (backupList.SelectedItem == null)
            {
                MessageBox.Show(this, "You need to select an item", "Error",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var result = MessageBox.Show(this, "Are you sure you want to delete the selected backup?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                bm.backups.RemoveAt(backupList.SelectedIndex);
                bm.SaveBackupFile();
            }
        }

        private void sideBarButton5_Click(object sender, RoutedEventArgs e)
        {
            hideAllContentGrids();
            contentGrids[1].Visibility = Visibility.Visible;
        }
        #endregion

        public void trayIconClick()
        {
            taskbarIcon.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Visible;
            
        }

        

    }

    public class backupListRow
    { 
        public string Name { get; set; }
        public string Path { get; set; }
        public string LastBackup { get; set; }
        public string Status { get; set; }
        public string Progress { get; set; }
        public string SizeProgress { get; set; }
        public int AllProgress { get; set; }
        public bool paused { get; set; }
    }

    public class CircularProgress : Shape
    {
        static CircularProgress()
        {
            System.Windows.Media.Brush myGreenBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 6, 176, 37));
            myGreenBrush.Freeze();

            StrokeProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(myGreenBrush));
            FillProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(System.Windows.Media.Brushes.Transparent));

            StrokeThicknessProperty.OverrideMetadata(
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(10.0));
        }

        // Value (0-100)
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // DependencyProperty - Value (0 - 100)
        private static FrameworkPropertyMetadata valueMetadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    new CoerceValueCallback(CoerceValue));   // Coerce value callback

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(CircularProgress), valueMetadata);

        private static object CoerceValue(DependencyObject depObj, object baseVal)
        {
            double val = (double)baseVal;
            val = Math.Min(val, 99.999);
            val = Math.Max(val, 0.0);
            return val;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                double startAngle = 90.0;
                double endAngle = 90.0 - ((Value / 100.0) * 360.0);

                double maxWidth = Math.Max(0.0, RenderSize.Width - StrokeThickness);
                double maxHeight = Math.Max(0.0, RenderSize.Height - StrokeThickness);

                double xStart = maxWidth / 2.0 * Math.Cos(startAngle * Math.PI / 180.0);
                double yStart = maxHeight / 2.0 * Math.Sin(startAngle * Math.PI / 180.0);

                double xEnd = maxWidth / 2.0 * Math.Cos(endAngle * Math.PI / 180.0);
                double yEnd = maxHeight / 2.0 * Math.Sin(endAngle * Math.PI / 180.0);

                StreamGeometry geom = new StreamGeometry();
                using (StreamGeometryContext ctx = geom.Open())
                {
                    ctx.BeginFigure(
                        new System.Windows.Point((RenderSize.Width / 2.0) + xStart,
                                  (RenderSize.Height / 2.0) - yStart),
                        true,   // Filled
                        false);  // Closed
                    ctx.ArcTo(
                        new System.Windows.Point((RenderSize.Width / 2.0) + xEnd,
                                  (RenderSize.Height / 2.0) - yEnd),
                        new System.Windows.Size(maxWidth / 2.0, maxHeight / 2),
                        0.0,     // rotationAngle
                        (startAngle - endAngle) > 180,   // greater than 180 deg?
                        SweepDirection.Clockwise,
                        true,    // isStroked
                        false);
                    //    ctx.LineTo(new Point((RenderSize.Width / 2.0), (RenderSize.Height / 2.0)), true, true);
                }

                return geom;
            }
        }
    }

    public class DoubleToPctConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string result = "";
            double d = (double)value;
            result = string.Format("{0}%", d);
            
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


