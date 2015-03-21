using System;
using System.Collections.Generic;

namespace WigleDBMerger.Tools
{
    class WiFiNet : IComparable <WiFiNet>
    {
        public string _bssid;
        public string _ssid;
        public int _frequency;
        public string _capabilities;
        public DateTime _lasttime;
        public double _lastlat;
        public double _lastlon;
        public int _bestlevel;
        public double _bestlat;
        public double _bestlon;

        public WiFiNet(string bssid, string ssid, string frequency, string capabilities,
            string lasttime, string lastlat, string lastlon, string bestlevel, string bestlat, string bestlon)
        {
            _bssid      = bssid;
            _ssid       = ssid;
            _frequency  = int.Parse(frequency);
            _capabilities = capabilities;
            _lasttime   = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(lasttime));
            _lastlat    = double.Parse(lastlat);
            _lastlon    = double.Parse(lastlon);
            _bestlevel  = int.Parse(bestlevel);
            _bestlat    = double.Parse(bestlat);
            _bestlon    = double.Parse(bestlon);
        }

        public WiFiNet() { }

        public int CompareTo(WiFiNet other)
        {
            if(this._bssid == other._bssid)
            {
                // точки с одинаковыми mac адресами
                if (this._bestlevel != other._bestlevel)
                {
                    //сначала сортировка по уровню сигнала по-убыванию (сначала самый мощный сигнал)
                    return other._bestlevel.CompareTo(this._bestlevel);
                }
                else if(this._lasttime != other._lasttime)
                {
                    //далее сортировка по дате
                    return other._lasttime.CompareTo(this._lasttime);
                }
            }            
            // точки с разными mac адресами сортируются по mac адресу по-возрастанию
            return this._bssid.CompareTo(other._bssid);
        }

        public override string ToString()
        {
            return string.Format("bssid: {0} level: {1}",this._bssid,this._bestlevel);
        }
    }
}
