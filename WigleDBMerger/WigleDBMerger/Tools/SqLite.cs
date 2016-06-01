using System;
using System.Data;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WigleDBMerger.Tools
{
    class SqLite : IDisposable
    {
        private SQLiteConnection[] sqlConn;
        private SQLiteCommand[] sqlCmd;
        private SQLiteDataAdapter[] sqlDAdapter;
        private DataSet[] dSet;
        private DataTable[] dTable;

        /// <summary>
        /// Словари для раздельного хранения списка сетей и локаций
        /// </summary>
        private Dictionary<string, List<WiFiNet>> dicNets;
        private Dictionary<string, List<Location>> dicLocations;

        /// <summary>
        /// Загружено сетей из всех файлов. Суммарно
        /// </summary>
        private int _loadedNets;
        public int loadedNets
        {
            get { return _loadedNets; }
            set { _loadedNets = value; }
        }

        /// <summary>
        /// Загружено локаций из всех файлов. Суммарно
        /// </summary>
        public int loadedLocations
        {
            get { return _loadedLocations; }
            set { _loadedLocations = value; }
        }
        private int _loadedLocations;

        /// <summary>
        /// Сколько получилось в итоге уникальных сетей
        /// </summary>
        private int _outNets;
        public int outNets
        {
            get { return _outNets; }
            set { _outNets = value; }
        }

        /// <summary>
        /// Сколько получилось в итоге уникальных локаций
        /// </summary>
        private int _outLocations;
        public int outLocations
        {
            get { return _outLocations; }
            set { _outLocations = value; }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="args">Список файлов с БД</param>
        /// <param name="merge">истина - объединять, ложь - генерировать KML</param>
        public SqLite(string[] args)
        {

                sqlConn = new SQLiteConnection[args.Length - 1];
                dSet = new DataSet[args.Length - 1];
                dTable = new DataTable[args.Length - 1];
                for (int i = 0; i < args.Length - 1; i++)
                {
                    string connStr = string.Format("Data Source={0};Version=3;", args[i]);
                    try
                    {
                        sqlConn[i] = new SQLiteConnection(connStr);
                    }
                    catch (SQLiteException ex)
                    {
                        throw new Exception(string.Format("Произошла ошибка:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
                    }
                }
        }

        /// <summary>
        /// Выполнить sql запрос к базе
        /// </summary>
        /// <param name="numConn">номер sql соединения</param>
        /// <param name="txtQuery">sql запрос</param>
        public void ExecQuery(int numConn, string txtQuery)
        {
            sqlConn[numConn].Open();
            sqlCmd[numConn] = sqlConn[numConn].CreateCommand();
            sqlCmd[numConn].CommandText = txtQuery;
            SQLiteTransaction myTrans = sqlConn[numConn].BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                sqlCmd[numConn].ExecuteNonQuery();
                myTrans.Commit();
            }
            catch (SQLiteException ex)
            {
                myTrans.Rollback();
                throw new Exception(string.Format("Произошла ошибка:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                sqlConn[numConn].Close();
            }
        }

        /// <summary>
        /// Заполнить словари. Главный метод
        /// </summary>
        public void FillDicts()
        {
            try
            {
                _loadedNets = 0;
                _loadedLocations = 0;
                _outNets = 0;
                _outLocations = 0;
                sqlCmd = new SQLiteCommand[sqlConn.Length];
                sqlDAdapter = new SQLiteDataAdapter[sqlConn.Length];

                //--Выбираем все Wi-Fi сети-------------------------------------------
                string txtQuery = "select * from network where type = \"W\" and lasttime <> 0 and lastlat <> 0";
                for (int i = 0; i < sqlConn.Length; i++)
                {
                    FillNets(txtQuery, i);
                }
                //---Загружаем все локации--------------------------------------------
                txtQuery = "select * from location where altitude <> 0 order by bssid";
                for (int i = 0; i < sqlConn.Length; i++)
                {
                    FillLocations(txtQuery, i);
                }
            }
            catch (SQLiteException ex)
            {
                throw new Exception(string.Format("Произошла ошибка:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                for (int i = 0; i < sqlConn.Length; i++)
                    sqlConn[i].Close();
            }
        }

        /// <summary>
        /// Заполнить словарь сетей
        /// </summary>
        /// <param name="txtQuery">sql запрос для выбора полей из таблицы network</param>
        /// <param name="currConn">номер текущего sql соединения</param>
        private void FillNets(string txtQuery, int currConn)
        {
            if (sqlConn[currConn].State != ConnectionState.Open)
                sqlConn[currConn].Open();
            sqlCmd[currConn] = sqlConn[currConn].CreateCommand();
            sqlDAdapter[currConn] = new SQLiteDataAdapter(txtQuery, sqlConn[currConn]);
            if (dSet[currConn] == null)
                dSet[currConn] = new DataSet();
            dSet[currConn].Reset();
            sqlDAdapter[currConn].Fill(dSet[currConn]);
            if (dTable[currConn] == null)
                dTable[currConn] = new DataTable();
            dTable[currConn] = dSet[currConn].Tables[0];
            _loadedNets += dTable[currConn].Rows.Count;
            if (_loadedNets > 0)
            {
                if (dicNets == null)
                    dicNets = new Dictionary<string, List<WiFiNet>>();
                for (int j = 0; j < dTable[currConn].Rows.Count; j++)
                {
                    string bssid = dTable[currConn].Rows[j]["bssid"].ToString();
                    if (!dicNets.ContainsKey(bssid))
                    {
                        List<WiFiNet> wfList = new List<WiFiNet>();
                        wfList.Add(new WiFiNet(bssid,
                                                dTable[currConn].Rows[j]["ssid"].ToString(),
                                                dTable[currConn].Rows[j]["frequency"].ToString(),
                                                dTable[currConn].Rows[j]["capabilities"].ToString(),
                                                dTable[currConn].Rows[j]["lasttime"].ToString(),
                                                dTable[currConn].Rows[j]["lastlat"].ToString(),
                                                dTable[currConn].Rows[j]["lastlon"].ToString(),
                                                dTable[currConn].Rows[j]["bestlevel"].ToString(),
                                                dTable[currConn].Rows[j]["bestlat"].ToString(),
                                                dTable[currConn].Rows[j]["bestlon"].ToString()
                                                )
                                    );

                        dicNets.Add(bssid, wfList);
                        outNets++;
                    }
                    else
                    {
                        dicNets[bssid].Add(new WiFiNet(bssid,
                                                       dTable[currConn].Rows[j]["ssid"].ToString(),
                                                       dTable[currConn].Rows[j]["frequency"].ToString(),
                                                       dTable[currConn].Rows[j]["capabilities"].ToString(),
                                                       dTable[currConn].Rows[j]["lasttime"].ToString(),
                                                       dTable[currConn].Rows[j]["lastlat"].ToString(),
                                                       dTable[currConn].Rows[j]["lastlon"].ToString(),
                                                       dTable[currConn].Rows[j]["bestlevel"].ToString(),
                                                       dTable[currConn].Rows[j]["bestlat"].ToString(),
                                                       dTable[currConn].Rows[j]["bestlon"].ToString()
                                                      )
                                          );
                    }
                }
            }
            dTable[currConn].Dispose();
            dSet[currConn].Dispose();
            if (sqlCmd[currConn] != null)
                sqlCmd[currConn].Dispose();
            if (sqlDAdapter[currConn] != null)
                sqlDAdapter[currConn].Dispose();
        }

        /// <summary>
        /// Заполнить словарь локаций
        /// </summary>
        /// <param name="txtQuery">SQL Запрос для выбора полей из таблицы location</param>
        /// <param name="currConn">номер текущего sql соединения</param>
        private void FillLocations(string txtQuery, int currConn)
        {
            if (sqlConn[currConn].State != ConnectionState.Open)
                sqlConn[currConn].Open();
            sqlCmd[currConn] = sqlConn[currConn].CreateCommand();
            sqlDAdapter[currConn] = new SQLiteDataAdapter(txtQuery, sqlConn[currConn]);
            if (dSet[currConn] == null)
                dSet[currConn] = new DataSet();
            dSet[currConn].Reset();
            sqlDAdapter[currConn].Fill(dSet[currConn]);
            if (dTable[currConn] == null)
                dTable[currConn] = new DataTable();
            dTable[currConn] = dSet[currConn].Tables[0];
            _loadedLocations += dTable[currConn].Rows.Count;
            if (_loadedLocations > 0)
            {
                if (dicLocations == null)
                    dicLocations = new Dictionary<string, List<Location>>();
                for (int j = 0; j < dTable[currConn].Rows.Count; j++)
                {
                    string bssid = dTable[currConn].Rows[j]["bssid"].ToString();
                    if (!dicLocations.ContainsKey(bssid))
                    {
                        List<Location> lcList = new List<Location>();
                        lcList.Add(new Location(bssid,
                                                dTable[currConn].Rows[j]["level"].ToString(),
                                                dTable[currConn].Rows[j]["lat"].ToString(),
                                                dTable[currConn].Rows[j]["lon"].ToString(),
                                                dTable[currConn].Rows[j]["altitude"].ToString(),
                                                dTable[currConn].Rows[j]["accuracy"].ToString(),
                                                dTable[currConn].Rows[j]["time"].ToString()
                                               )
                                   );
                        dicLocations.Add(bssid, lcList);
                    }
                    else
                    {
                        dicLocations[bssid].Add(new Location(bssid,
                                                             dTable[currConn].Rows[j]["level"].ToString(),
                                                             dTable[currConn].Rows[j]["lat"].ToString(),
                                                             dTable[currConn].Rows[j]["lon"].ToString(),
                                                             dTable[currConn].Rows[j]["altitude"].ToString(),
                                                             dTable[currConn].Rows[j]["accuracy"].ToString(),
                                                             dTable[currConn].Rows[j]["time"].ToString()
                                                            )
                                               );
                    }
                }
            }
            dTable[currConn].Dispose();
            dSet[currConn].Dispose();
            if (sqlCmd[currConn] != null)
                sqlCmd[currConn].Dispose();
            if (sqlDAdapter[currConn] != null)
                sqlDAdapter[currConn].Dispose();
        }

        /// <summary>
        /// Сортировка списков в словарях по точности координат, уровню сигнала и дате
        /// </summary>
        public void SortDicts()
        {
            foreach (KeyValuePair<string, List<Location>> pair in dicLocations)
            {
                // отсортировали все локации с одинаковыми mac адресами по точности координат, уровню сигнала и дате
                pair.Value.Sort();
            }
            foreach (KeyValuePair<string, List<WiFiNet>> pair in dicNets)
            {
                // отсортировали все сети с одинаковыми mac адресами по уровню сигнала и дате
                pair.Value.Sort();
            }
        }

        /// <summary>
        /// Поиск самой свежей даты, когда сканилась сеть
        /// </summary>
        /// <param name="bssid">мак адрес сети</param>
        /// <returns></returns>
        private WiFiNet FindLastTimeInNets(string bssid)
        {

            DateTime lastDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int pos = 0;
            if (dicNets.ContainsKey(bssid))
            {
                for (int i = 0; i < dicNets[bssid].Count; i++)
                {
                    if (dicNets[bssid][i]._lasttime > lastDate)
                    {
                        lastDate = dicNets[bssid][i]._lasttime;
                        pos = i;
                    }
                }
                return dicNets[bssid][pos];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Поиск лучшего уровня сигнала по всем локациям с мак адресом сети = bssid
        /// </summary>
        /// <param name="bssid">мак адрес сети</param>
        /// <returns></returns>
        private int FindBestLevelInLocations(string bssid)
        {
            int bestLevel = -200;
            if (dicLocations.ContainsKey(bssid))
            {
                for (int i = 0; i < dicLocations[bssid].Count; i++)
                {
                    if (dicLocations[bssid][i]._level > bestLevel)
                    {
                        bestLevel = dicLocations[bssid][i]._level;
                    }
                }
                return bestLevel;
            }
            else
            {
                return bestLevel;
            }
        }

        /// <summary>
        /// Сгенерировать окончательный список сетей и локаций (без дублей)
        /// </summary>
        public void GenerateListOfNetworksViaBestCoordinates()
        {
            foreach (KeyValuePair<string, List<WiFiNet>> pair in dicNets)
            {
                string bssid = pair.Value[0]._bssid;

                WiFiNet wfWithLastSsid = FindLastTimeInNets(bssid);

                WiFiNet wf = new WiFiNet();
                wf._bssid = bssid;

                wf._ssid = wfWithLastSsid._ssid;
                wf._lasttime = wfWithLastSsid._lasttime;
                wf._lastlat = wfWithLastSsid._lastlat;
                wf._lastlon = wfWithLastSsid._lastlon;
                wf._capabilities = wfWithLastSsid._capabilities;
                wf._frequency = wfWithLastSsid._frequency;
                wfWithLastSsid = null;
                int blvl = FindBestLevelInLocations(bssid);
                wf._bestlevel = blvl != -200 ? blvl : pair.Value[0]._bestlevel;    //-200 признак отсутствия локаций с таким bssid
                if (dicLocations.ContainsKey(bssid))
                {
                    wf._bestlat = dicLocations[bssid][0]._lat;
                }
                else
                {
                    wf._bestlat = pair.Value[0]._bestlat;
                }
                if (dicLocations.ContainsKey(bssid))
                {
                    wf._bestlon = dicLocations[bssid][0]._lon;
                }
                else
                {
                    wf._bestlon = pair.Value[0]._bestlon;
                }

                dicNets[bssid].Clear();
                dicNets[bssid].Add(wf);
                DeleteDublicatesOfLocations(bssid);
            }
        }

        /// <summary>
        /// Выгрузка содержимого словарей в sqlite файл
        /// </summary>
        public void UploadDictsToFile(string filePath)
        {
            SQLiteConnection.CreateFile(filePath);
            using (SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=" + filePath + ";New=True;Version=3;UseUTF16Encoding=True"))
            {
                m_dbConnection.Open();
                SQLiteCommand command = m_dbConnection.CreateCommand();

                //----для ускорения добавления большого кол-ва элементов-------
                command.CommandText = "PRAGMA auto_vacuum = 1;PRAGMA synchronous = 0; PRAGMA cache_size=4000;";
                command.ExecuteNonQuery();

                SQLiteTransaction myTrans = m_dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                StringBuilder sb = new StringBuilder();
                try
                {
                    //----------------Создаем таблицу network---------------------------------------
                    sb.Append("CREATE TABLE network(");
                    sb.Append("bssid text primary key not null, ");
                    sb.Append("ssid text not null, ");
                    sb.Append("frequency int not null, ");
                    sb.Append("capabilities text not null, ");
                    sb.Append("lasttime long not null, ");
                    sb.Append("lastlat double not null, ");
                    sb.Append("lastlon double not null, ");
                    sb.Append("type text not null default 'W', ");
                    sb.Append("bestlevel integer not null default 0, ");
                    sb.Append("bestlat double not null default 0, ");
                    sb.Append("bestlon double not null default 0");
                    sb.Append(")");
                    command.CommandText = sb.ToString();
 
                    command.ExecuteNonQuery();

                    //----------Создаем таблицу location--------------                
                    sb.Clear();
                    sb.Append("CREATE TABLE location (");
                    sb.Append("_id integer primary key autoincrement,");
                    sb.Append("bssid text not null,");
                    sb.Append("level integer not null,");
                    sb.Append("lat double not null,");
                    sb.Append("lon double not null,");
                    sb.Append("altitude double not null,");
                    sb.Append("accuracy float not null,");
                    sb.Append("time long not null");
                    sb.Append(")");
                    command.CommandText = sb.ToString();

                    command.ExecuteNonQuery();

                    //----------Добавление элементов networks-----------------
                    double idx= 0;
                    foreach (KeyValuePair<string, List<WiFiNet>> pair in dicNets)
                    {
                        sb.Clear();
                        sb.Append("INSERT INTO network(");
                        sb.Append("bssid, ");
                        sb.Append("ssid, ");
                        sb.Append("frequency, ");
                        sb.Append("capabilities, ");
                        sb.Append("lasttime, ");
                        sb.Append("lastlat, ");
                        sb.Append("lastlon, ");
                        sb.Append("type, ");
                        sb.Append("bestlevel, ");
                        sb.Append("bestlat, ");
                        sb.Append("bestlon) ");
                        sb.Append("VALUES(");
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._bssid));
                        sb.Append(string.Format("'{0}', ", TextParser.TextReplace(pair.Value[0]._ssid, @"[^-_.,!?&~;:><^$#@\)\(\[\]/\\0-9A-Za-zА-Яа-я]", "")));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._frequency));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._capabilities));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._lasttime));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._lastlat));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._lastlon));
                        sb.Append("'W', ");
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._bestlevel));
                        sb.Append(string.Format("'{0}', ", pair.Value[0]._bestlat));
                        sb.Append(string.Format("'{0}'", pair.Value[0]._bestlon));
                        sb.Append(");");
                        command.CommandText = sb.ToString();
                        Console.SetCursorPosition(0, 12);
                        Console.Write(string.Format("Выгрузка сетей: {0}%",Math.Round(idx/dicNets.Count*100,0)));
                        command.ExecuteNonQuery();
                        idx++;
                    }

                    //----------Добавление элементов locations-----------------
                    idx = 0;
                    foreach (KeyValuePair<string, List<Location>> pair in dicLocations)
                    {
                        for (int i = 0; i < pair.Value.Count; i++)
                        {
                            sb.Clear();
                            sb.Append("INSERT INTO location (");
                            sb.Append("bssid,");
                            sb.Append("level,");
                            sb.Append("lat,");
                            sb.Append("lon,");
                            sb.Append("altitude,");
                            sb.Append("accuracy,");
                            sb.Append("time)");
                            sb.Append("VALUES(");
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._bssid));
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._level));
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._lat));
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._lon));
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._altitude));
                            sb.Append(string.Format("'{0}', ", pair.Value[i]._accuracy));
                            sb.Append(string.Format("'{0}'", pair.Value[i]._time));
                            sb.Append(");");
                            command.CommandText = sb.ToString();
                            command.ExecuteNonQuery();                            
                            Console.SetCursorPosition(0, 13);
                            Console.Write(string.Format("Выгрузка локаций: {0}%", Math.Round(idx / dicLocations.Count * 100, 0)));
                        }
                        idx++;
                    }
                    myTrans.Commit();
                    m_dbConnection.Close();
                }
                catch (SQLiteException ex)
                {
                    myTrans.Rollback();
                    m_dbConnection.Close();
                    m_dbConnection.Dispose();
                    throw new Exception(string.Format("Произошла ошибка:\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
                }
            }
        }

        public void UploadOpenNetsKML()
        {
            try
            {
                using (Stream fs = new FileStream("openNets.kml", FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sw.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\"><Document><Style id=\"red\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/ms/icons/red-dot.png</href></Icon></IconStyle></Style><Style id=\"yellow\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/ms/icons/yellow-dot.png</href></Icon></IconStyle></Style><Style id=\"green\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/ms/icons/green-dot.png</href></Icon></IconStyle></Style><Folder><name>Wifi Networks</name>");
                        foreach (KeyValuePair<string, List<WiFiNet>> pair in dicNets)
                        {
                            if (!pair.Value[0]._capabilities.ToLower().Contains("wpa") && !pair.Value[0]._capabilities.ToLower().Contains("wep"))
                            {
                                sw.WriteLine("<Placemark>");
                                sw.WriteLine(string.Format("\t<name><![CDATA[{0}]]></name>", TextParser.TextReplace(pair.Value[0]._ssid, @"[^-_.,!?&~;:><^$#@\)\(\[\]/\\0-9A-Za-zА-Яа-я]","")));
                                sw.WriteLine("\t<description>");
                                sw.Write(string.Format("\t\t<![CDATA[BSSID: <b>{0}</b><br />",pair.Value[0]._bssid));
                                sw.Write(string.Format("</b><br/>ДатаСканирования: <b>{0}</b><br />", pair.Value[0]._lasttime));
                                sw.Write(string.Format("Шифрование: <b>{0}</b><br />", pair.Value[0]._capabilities));
                                sw.WriteLine(string.Format("Частота: <b>{0}</b><br />]]>", pair.Value[0]._frequency));
                                sw.WriteLine("\t</description>\r\n\t<styleUrl>#red</styleUrl>");
                                sw.WriteLine(string.Format("\t<Point>\r\n\t\t<coordinates>{0},{1}</coordinates>\r\n\t</Point>", pair.Value[0]._bestlon.ToString().Replace(',', '.'), pair.Value[0]._bestlat.ToString().Replace(',', '.')));
                                sw.WriteLine("</Placemark>");
                            }
                        }
                        sw.WriteLine("</Folder>");
                        sw.WriteLine("</Document></kml>");
                        sw.Close();
                        System.Diagnostics.Process.Start("explorer.exe", @"/select, openNets.kml");
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message + ex.StackTrace,ex);
            }
        }

        /// <summary>
        /// Удалить одинаковые локации
        /// </summary>
        /// <param name="bssid">мак адрес сети</param>
        private void DeleteDublicatesOfLocations(string bssid)
        {
            int i = 0;
            int j = 1;
            if (dicLocations.ContainsKey(bssid))
            {
                while (i < dicLocations[bssid].Count - 1)
                {
                    while (j < dicLocations[bssid].Count)
                    {
                        if (dicLocations[bssid][i].Equals(dicLocations[bssid][j]))
                        {
                            dicLocations[bssid].RemoveAt(j);
                            continue;
                        }
                        j++;
                    }
                    i++;
                    outLocations++;
                }
            }
        }

        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < sqlConn.Length; i++)
            {
                sqlConn[i].Dispose();
                dTable[i].Dispose();
                dSet[i].Dispose();
                if (sqlCmd[i] != null)
                    sqlCmd[i].Dispose();
                if (sqlDAdapter[i] != null)
                    sqlDAdapter[i].Dispose();
            }
            dicNets.Clear();
            dicLocations.Clear();
        }
    }
}
