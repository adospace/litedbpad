using LINQPad.Extensibility.DataContext;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LiteDBPad
{
    public class ConnectionProperties
    {
        IConnectionInfo _cxInfo;

        public ConnectionProperties(IConnectionInfo cxInfo)
        {
            _cxInfo = cxInfo ?? throw new ArgumentNullException(nameof(cxInfo));
        }

        public ConnectionString GetConnectionString()
        {
            var cs = new ConnectionString()
            {
                Filename = Filename,
                Connection = Mode,
                InitialSize = InitialSize,
                Password = Password,
                ReadOnly = ReadOnly,
                Upgrade = Upgrade
            };

            return cs;
        }

        public bool Persist
        {
            get { return _cxInfo.Persist; }
            set { _cxInfo.Persist = value; }
        }
        public string Filename
        {
            get { return _cxInfo.DriverData.ElementValue("Filename"); }
            set { _cxInfo.DriverData.SetElementValue("Filename", value); }
        }
        public long InitialSize
        {
            get { return _cxInfo.DriverData.ElementValue("InitialSize", long.Parse, new ConnectionString().InitialSize); }
            set { _cxInfo.DriverData.SetElementValue("InitialSize", value); }
        }
        public bool Upgrade
        {
            get { return _cxInfo.DriverData.ElementValue("Upgrade", bool.Parse, new ConnectionString().Upgrade); }
            set { _cxInfo.DriverData.SetElementValue("Upgrade", value); }
        }
        public bool ReadOnly
        {
            get { return _cxInfo.DriverData.ElementValue("ReadOnly", bool.Parse, new ConnectionString().ReadOnly); }
            set { _cxInfo.DriverData.SetElementValue("ReadOnly", value); }
        }
        public ConnectionType Mode
        {
            get { return _cxInfo.DriverData.ElementValue("Mode", (v) => (ConnectionType)Enum.Parse(typeof(ConnectionType), v), ConnectionType.Shared); }
            set { _cxInfo.DriverData.SetElementValue("Mode", value); }
        }
        public string Password
        {
            get { return _cxInfo.DriverData.ElementValue("Password", (v) => v == null ? null : _cxInfo.Decrypt(v), new ConnectionString().Password); }
            set { _cxInfo.DriverData.SetElementValue("Password", string.IsNullOrWhiteSpace(value) ? null : _cxInfo.Encrypt(value)); }
        }
    }
}
