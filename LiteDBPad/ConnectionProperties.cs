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
            if (cxInfo == null)
                throw new ArgumentNullException(nameof(cxInfo));
            _cxInfo = cxInfo;
        }

        public ConnectionString GetConnectionString()
        {
            var cs = new ConnectionString(Filename);

            cs.CacheSize = CacheSize;
            cs.InitialSize = InitialSize;
            cs.Journal = Journal;
            cs.LimitSize = LimitSize;
            cs.Mode = Mode;
            cs.Password = Password;
            cs.Timeout = Timeout;
            cs.Upgrade = Upgrade;

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
        public int CacheSize
        {
            get { return _cxInfo.DriverData.ElementValue("CacheSize", int.Parse, new ConnectionString().CacheSize); }
            set { _cxInfo.DriverData.SetElementValue("CacheSize", value); }
        }
        public long InitialSize
        {
            get { return _cxInfo.DriverData.ElementValue("InitialSize", long.Parse, new ConnectionString().InitialSize); }
            set { _cxInfo.DriverData.SetElementValue("InitialSize", value); }
        }
        public bool Journal
        {
            get { return _cxInfo.DriverData.ElementValue("Journal", bool.Parse, new ConnectionString().Journal); }
            set { _cxInfo.DriverData.SetElementValue("Journal", value); }
        }
        public long LimitSize
        {
            get { return _cxInfo.DriverData.ElementValue("LimitSize", long.Parse, new ConnectionString().LimitSize); }
            set { _cxInfo.DriverData.SetElementValue("LimitSize", value); }
        }
        public FileMode Mode
        {
            get { return _cxInfo.DriverData.ElementValue("Mode", (v) => (FileMode)Enum.Parse(typeof(FileMode), v), new ConnectionString().Mode); }
            set { _cxInfo.DriverData.SetElementValue("Mode", value); }
        }
        public string Password
        {
            get { return _cxInfo.DriverData.ElementValue("Password", (v) => v == null ? null : _cxInfo.Decrypt(v), new ConnectionString().Password); }
            set { _cxInfo.DriverData.SetElementValue("Password", string.IsNullOrWhiteSpace(value) ? null : _cxInfo.Encrypt(value)); }
        }
        public TimeSpan Timeout
        {
            get { return _cxInfo.DriverData.ElementValue("Timeout", XmlConvert.ToTimeSpan, new ConnectionString().Timeout); }
            set { _cxInfo.DriverData.SetElementValue("Timeout", XmlConvert.ToString(value)); }
        }
        public bool Upgrade
        {
            get { return _cxInfo.DriverData.ElementValue("Upgrade", bool.Parse, new ConnectionString().Upgrade); }
            set { _cxInfo.DriverData.SetElementValue("Upgrade", value); }
        }

    }
}
