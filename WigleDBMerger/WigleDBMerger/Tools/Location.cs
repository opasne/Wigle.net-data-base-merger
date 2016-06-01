using System;

namespace WigleDBMerger.Tools
{
    class Location :IComparable<Location>,IEquatable<Location>
    {
        public string _bssid;
        public int _level;
        public double _lat;
        public double _lon;
        public double _altitude;
        public double _accuracy;
        public DateTime _time;

        public Location(string bssid, string level, string lat, string lon, string altitude, string accuracy, string time)
        {
            _bssid = bssid;
            _level = int.Parse(level);
            _lat = double.Parse(lat);
            _lon = double.Parse(lon);
            _altitude = double.Parse(altitude);
            _accuracy = double.Parse(accuracy);
            _time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(time));
        }

        public Location() { }

        public int CompareTo(Location other)
        {
            if(this._bssid == other._bssid)
            {
                // точки с одинаковыми mac адресами
                if (this._accuracy != other._accuracy)
                {
                    //сначала сортировка по точности координат по возрастанию от 1 до 255 (сначала самые точные)
                    return this._accuracy.CompareTo(other._accuracy);
                }
                else if(this._level != other._level)
                {
                    //далее сортировка по уровню сигнала
                    return other._level.CompareTo(this._level);
                }
                else if(this._time != other._time)
                {
                    //далее сортировка по дате
                    return other._time.CompareTo(this._time);
                }
            }
            // точки с разными mac адресами сортируются по mac адресу по-возрастанию
            return other._bssid.CompareTo(this._bssid);
        }

        public override string ToString()
        {
            return string.Format("bssid: {0} accuracy: {1} level: {2} date: {3}", this._bssid, this._accuracy, this._level, this._time);
        }

        public bool Equals(Location other)
        {
            if (other == null)
                return false;

            //Здесь сравнение по ссылкам необязательно.
            //Если вы уверены, что многие проверки на идентичность будут отсекаться на проверке по ссылке - //можно имплементировать.
            if (object.ReferenceEquals(this, other))
                return true;

            //Если по логике проверки, экземпляры родительского класса и класса потомка могут считаться равными,
            //то проверять на идентичность необязательно и можно переходить сразу к сравниванию полей.
            if (this.GetType() != other.GetType())
                return false;

            if ((_bssid == other._bssid) && (_accuracy == other._accuracy) && (_altitude == other._altitude) &&
                 (_lat == other._lat) && (_level == other._level) && (_lon == other._lon) && (_time == other._time))
                return true;
            else
                return false;
        }

    }
}
