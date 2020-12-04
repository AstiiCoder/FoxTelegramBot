using System;
using System.Collections.Generic;
using System.IO;


namespace FoxTeleBo
    {
    class Class_NetPing
        {
        private static List<string> IPList = new List<string>();
        public static string PingDescriptionSended = "";

        /// <summary>
        /// Use this method to fill list based on adresses list from the file
        /// </summary>
        private static void ReadPingerList()
            {
            string path = Environment.CurrentDirectory + "\\iplist.txt";

            //очищаем и заполняем список всех адресов
            if (IPList.Count > 0) IPList.Clear();
            if (System.IO.File.Exists(path))
                using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                    {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        {
                        if ((line.Length > 3) && (line.Substring(0, 2) != "::")) IPList.Add(line);
                        }
                    }
            }

        /// <summary>
        /// Method for get hostname or IP and description of the adress
        /// </summary>
        private static void GetIPAndDescrFromList(string item, ref string adr, ref string descr)
            {
            string text = item;
            text = text.Trim().Replace(")", "");
            string[] words = text.Split(new char[] { '(' });
            if (words.Length > 0) adr = words[0].Trim();
            if (words.Length > 1) descr = words[1]; else descr = words[0];
            }

        /// <summary>
        /// Network pinging method. Execute for list of adresses. Return message for sending to admin group
        /// </summary>
        public static string DoPingByList()
            {
            ReadPingerList();

            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();

            string NoPingDescription = "";
            string adr = "";
            string descr = "";

            if (IPList.Count == 0) return "";

            // делаем ping всем по очереди, собираем результаты о плохих 
            foreach (var item in IPList)
                {
                GetIPAndDescrFromList((String)item, ref adr, ref descr);
                try
                    {
                    System.Net.NetworkInformation.PingReply pingReply = ping.Send(adr);
                    if (pingReply.RoundtripTime >= 300) NoPingDescription += descr + $" - ping {pingReply.RoundtripTime}, status - {pingReply.Status}\n";
                    }
                catch (Exception)
                    {
                    NoPingDescription += descr + " - no ping\n";
                    }
                }

            if ((PingDescriptionSended != NoPingDescription) && (NoPingDescription.Length > 0))
                {
                return NoPingDescription;
                }
            else
                return "";
            }

        }
    }
