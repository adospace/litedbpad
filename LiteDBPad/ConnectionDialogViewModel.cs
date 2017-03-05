using LINQPad.Extensibility.DataContext;
using LiteDB;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace LiteDBPad
{
    public class ConnectionDialogViewModel : INotifyPropertyChanged
    {
        public ConnectionDialog View { get; private set; }
        public ConnectionDialogViewModel(ConnectionDialog view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            View = view;
            View.pwdBox.PasswordChanged += (s, e) => Password = View.pwdBox.Password;
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        internal void SaveTo(ConnectionProperties cxProperties)
        {
            cxProperties.Persist = Persist;
            cxProperties.Filename = Filename;
            cxProperties.CacheSize = CacheSize;
            cxProperties.InitialSize = InitialSize;
            cxProperties.Journal = Journal;
            cxProperties.LimitSize = LimitSize;
            cxProperties.Mode = Mode;
            cxProperties.Password = Password;
        }

        internal void LoadFrom(ConnectionProperties cxInfo)
        {
            Persist = cxInfo.Persist;
            Filename = cxInfo.Filename;
            CacheSize = cxInfo.CacheSize;
            InitialSize = cxInfo.InitialSize;
            Journal = cxInfo.Journal;
            LimitSize = cxInfo.LimitSize;
            Mode = cxInfo.Mode;
            Password = View.pwdBox.Password = cxInfo.Password;
        }

        #region Persist

        private bool _persist = false;
        public bool Persist
        {
            get { return _persist; }
            set
            {
                if (_persist != value)
                {
                    _persist = value;
                    RaisePropertyChanged("Persist");
                }
            }
        }

        #endregion

        #region Filename

        private string _filename = null;
        public string Filename
        {
            get { return _filename; }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    RaisePropertyChanged("Filename");
                }
            }
        }

        #endregion

        #region CacheSize

        private int _cacheSize = 0;
        public int CacheSize
        {
            get { return _cacheSize; }
            set
            {
                if (_cacheSize != value)
                {
                    _cacheSize = value;
                    RaisePropertyChanged("CacheSize");
                }
            }
        }

        #endregion

        #region InitialSize

        private long _initialSize = 0;
        public long InitialSize
        {
            get { return _initialSize; }
            set
            {
                if (_initialSize != value)
                {
                    _initialSize = value;
                    RaisePropertyChanged("InitialSize");
                }
            }
        }

        #endregion

        #region Journal

        private bool _journal = false;
        public bool Journal
        {
            get { return _journal; }
            set
            {
                if (_journal != value)
                {
                    _journal = value;
                    RaisePropertyChanged("Journal");
                }
            }
        }

        #endregion

        #region LimitSize

        private long _limitSize = 0;
        public long LimitSize
        {
            get { return _limitSize; }
            set
            {
                if (_limitSize != value)
                {
                    _limitSize = value;
                    RaisePropertyChanged("LimitSize");
                }
            }
        }

        #endregion

        #region Mode

        private FileMode _mode = FileMode.Exclusive;
        public FileMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    RaisePropertyChanged("Mode");
                }
            }
        }

        #endregion

        #region Password

        private string _password = null;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = string.IsNullOrWhiteSpace(value) ? null : value;
                    RaisePropertyChanged("Password");
                }
            }
        }

        #endregion




        #region Connection


        #region Browse Command
        RelayCommand _browseCommand = null;
        public ICommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                    _browseCommand = new RelayCommand((p) => Browse(p), (p) => CanBrowse(p));

                return _browseCommand;
            }
        }

        void Browse(object parameter)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "LiteDB Database (*.db)|*.db|All files (*.*)|*.*";
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Filename = dlg.FileName;
                RaisePropertyChanged("ConnectionProperties");
            }
        }

        bool CanBrowse(object parameter)
        {
            return true;
        }
        #endregion


        #endregion

        #region FileModes
        public IEnumerable<FileMode> Modes
        {
            get
            {
                return Enum.GetValues(typeof(FileMode))
                    .Cast<FileMode>();
            }
        }
        #endregion

        #region TestConnection Command
        RelayCommand _testConnectionCommand = null;
        public ICommand TestConnectionCommand
        {
            get
            {
                if (_testConnectionCommand == null)
                    _testConnectionCommand = new RelayCommand((p) => TestConnection(), (p) => CanTestConnection(p));

                return _testConnectionCommand;
            }
        }

        bool TestConnection(bool showSuccedMessage = true)
        {
            try
            {
                var cs = new ConnectionString(Filename);

                cs.CacheSize = CacheSize;
                cs.InitialSize = InitialSize;
                cs.Journal = Journal;
                cs.LimitSize = LimitSize;
                cs.Mode = Mode;
                cs.Password = Password;

                using (var db = new LiteDatabase(cs))
                    db.GetCollectionNames();

                if (showSuccedMessage)
                    MessageBox.Show("Test succeed!", "LiteDB Connection");
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show(View, "Unable to open database, please check connection settings and try again", "LiteDB Connection");
            }

            return false;
        }

        bool CanTestConnection(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Filename);
        }
        #endregion

        #region Cancel Command
        RelayCommand _cancelCommand = null;
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                    _cancelCommand = new RelayCommand((p) => Cancel(p), (p) => CanCancel(p));

                return _cancelCommand;
            }
        }

        void Cancel(object parameter)
        {
            View.DialogResult = false;

        }

        bool CanCancel(object parameter)
        {
            return true;
        }
        #endregion


        #region Confirm Command
        RelayCommand _confirmCommand = null;
        public ICommand ConfirmCommand
        {
            get
            {
                if (_confirmCommand == null)
                    _confirmCommand = new RelayCommand((p) => Confirm(p), (p) => CanConfirm(p));

                return _confirmCommand;
            }
        }

        void Confirm(object parameter)
        {
            View.DialogResult = true;
        }

        bool CanConfirm(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Filename);
        }
        #endregion
    }
}
