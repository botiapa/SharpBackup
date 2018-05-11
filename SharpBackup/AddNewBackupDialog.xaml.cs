using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SharpBackup
{
    /// <summary>
    /// Interaction logic for AddNewBackupDialog.xaml
    /// </summary>
    public partial class AddNewBackupDialog : Window
    {
        public Boolean Success = false;
        public AddNewBackupDialog()
        {
            InitializeComponent();

        }

        #region nameTextBox
        private readonly string nameTextBoxPlaceholder = "e.g.: example name";
        private void nameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            nameTextBox.BorderBrush = Brushes.Black;
            if (nameTextBox.Text == nameTextBoxPlaceholder) { nameTextBox.Text = ""; }
        }

        private void nameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(nameTextBox.Text)) { nameTextBox.Text = nameTextBoxPlaceholder; }
            nameTextBox_Validate();
        }

        private Boolean nameTextBox_Validate()
        {
            if(String.IsNullOrWhiteSpace(nameTextBox.Text) || nameTextBox.Text == nameTextBoxPlaceholder)
            {
                nameTextBox.BorderBrush = Brushes.Red;
                return false;
            }
            return true;
        }
        #endregion

        #region locationTextBox
        private readonly string locationTextBoxPlaceholder = "e.g.: C:\\examplefolder";
        private void locationTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            locationTextBox.BorderBrush = Brushes.Black;
            if (locationTextBox.Text == locationTextBoxPlaceholder) { locationTextBox.Text = ""; }
        }

        private void locationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(locationTextBox.Text)) { locationTextBox.Text = locationTextBoxPlaceholder; }
            locationTextBox_Validate();
        }

        private Boolean locationTextBox_Validate()
        {
            if (String.IsNullOrWhiteSpace(locationTextBox.Text) || locationTextBox.Text == locationTextBoxPlaceholder || !Directory.Exists(locationTextBox.Text))
            {
                locationTextBox.BorderBrush = Brushes.Red;
                return false;
            }
            return true;
        }
        #endregion

        #region delayTextBox
        private readonly string delayTextBoxPlaceholder = "e.g.: 60";
        private void delayTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            delayTextBox.BorderBrush = Brushes.Black;
            if (delayTextBox.Text == delayTextBoxPlaceholder) { delayTextBox.Text = ""; }

        }

        private void delayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(String.IsNullOrWhiteSpace(delayTextBox.Text)) { delayTextBox.Text = delayTextBoxPlaceholder; }
            delayTextBox_Validate();
        }

        private Boolean delayTextBox_Validate()
        {
            if (!Int32.TryParse(delayTextBox.Text, out int _))
            {
                delayTextBox.BorderBrush = Brushes.Red;
                return false;
            }
            return true;
        }
        #endregion
        #region Buttons
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if(!validateAllInputs())
            {
                return;
            }

            Success = true;
            Close();
        }

        Boolean validateAllInputs()
        {
            if(!(nameTextBox_Validate() & locationTextBox_Validate() & delayTextBox_Validate()))
            {
                return false;
            }
            return true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            nameTextBox.Text = String.Empty;
            locationTextBox.Text = String.Empty;
            delayTextBox.Text = String.Empty;
            Close();
        }

        #endregion

        private void TitleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void folderChooserButton_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    locationTextBox.Text = fbd.SelectedPath;
                    locationTextBox.BorderBrush = Brushes.Black;
                }
            }
        }
    }


}
