using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace Blinky
{
    class Sensorbook
    {
        // "CRO" client read only 
        // "CROA" client read only with authentication
        // "CRW" client read/write (default) 
        // "CRWA" client read/write with authentication 
        public enum clientmode { ReadOnly, ReadOnlyAuthenticated, ReadWrite, ReadWriteAuthenticated };

        // "FOR" read on read (default) 
        // "NFOR" no read on read
        public enum readmode { FlushOnRead, NoFlushOnRead }

        // "ROW" Replace on Write 
        // "AOW" Append on write (default) 
        public enum writemode { ReplaceOnWrite, AppendOnWrite }

        private string pipename = null;
        private string masterkey = null;
        private string accesstoken = null;
        private string clientkey = null;
        private string clientname = null;
        private string clientAccessMode = null;
        private string clientReadMode = null;
        private string clientWriteMode = null;
        private string masterReadMode = null;
        private string masterWriteMode = null;

        HttpClient httpClient = null;

        private string _lasterror;
        public string lasterror { get { return _lasterror; } private set { _lasterror = value; } }


        private string server = "http://serialhub.azurewebsites.net/";

        /// <summary>
        /// SensorBook Constructor
        /// </summary>
        public Sensorbook()
        {
            httpClient = new HttpClient();
        }

        /// <summary>
        /// Set Options for the Open request
        /// </summary>
        /// <param name="ca"></param>
        /// <param name="cr"></param>
        /// <param name="mr"></param>
        /// <param name="cw"></param>
        /// <param name="mw"></param>
        /// <returns></returns>
        public bool SetOptions(clientmode ca, readmode cr, readmode mr, writemode cw, writemode mw)
        {
            if (ca == clientmode.ReadOnly) clientAccessMode = "CRO";
            if (ca == clientmode.ReadOnlyAuthenticated) clientAccessMode = "CROA";
            if (ca == clientmode.ReadWrite) clientAccessMode = "CRW";
            if (ca == clientmode.ReadWriteAuthenticated) clientAccessMode = "CRWA";

            if (cr == readmode.FlushOnRead) clientReadMode = "FOR";
            if (cr == readmode.NoFlushOnRead) clientReadMode = "NFOR";

            if (mr == readmode.FlushOnRead) masterReadMode = "FOR";
            if (mr == readmode.NoFlushOnRead) masterReadMode = "NFOR";

            if (cw == writemode.AppendOnWrite) clientWriteMode = "AOW";
            if (cw == writemode.ReplaceOnWrite) clientWriteMode = "ROW";

            if (mw == writemode.AppendOnWrite) masterWriteMode = "AOW";
            if (mw == writemode.ReplaceOnWrite) masterWriteMode = "ROW";
            return true;
        }

        public async Task<string> MasterAddClient(string cn)
        {
            if (masterkey == null)
            {
                return null;
            }
            if (cn == null) return null;
            string ecn = Uri.EscapeUriString(cn);
            try
            {
                string serverstring = server + "masteraddclient.php?p=" + pipename + "&m=" + masterkey + "&cn=" + ecn;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return x[1];
                }
                else
                {
                    lasterror = response;
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>
        /// Opens a client pipe and sets required parameters 
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="ck"></param>
        /// <param name="at"></param>
        /// <returns></returns>
        public async Task<bool> OpenClient(string pn, string cn, string ck, string at)
        {
            if (pn == null) return false;
            string epn = Uri.EscapeUriString(pn);
            string serverstring = server + "openclient.php?p=" + epn;
            if (ck != null)
                serverstring += "&c=" + Uri.EscapeUriString(ck);
            if (at != null)
                serverstring += "&t=" + Uri.EscapeUriString(at);
            if (cn != null)
                serverstring += "&cn=" + Uri.EscapeUriString(cn);

            string response = await httpClient.GetStringAsync(serverstring);
            string[] x = response.Split(':');
            if (x[0].ToUpper().Contains("OK"))
            {
                pipename = epn;
                if (cn != null) { clientname = Uri.EscapeUriString(cn); }
                if (at != null) { accesstoken = Uri.EscapeUriString(at); }
                if (ck != null) { clientkey = Uri.EscapeUriString(ck); }
            }
            else
            {
                lasterror = response;
                accesstoken = null;
                pipename = null;
                masterkey = null;
                clientname = null;
                clientkey = null;
                return false;
            }


            return true;
        }


        /// <summary>
        /// Open a new Pipe; 
        /// </summary>
        /// <param name="pn"></param>
        /// <returns></returns>
        public async Task<string> MasterOpen(string pn, string mk, string at)
        {
            try
            {
                if (pn == null) return null;
                string epn = Uri.EscapeUriString(pn);
                string serverstring = server + "Open.php?p=" + epn;
                //if masterkey is set this will be a reopen of the master pipe. 
                if (mk != null)
                {
                    string emk = Uri.EscapeUriString(mk);
                    masterkey = emk;
                    serverstring += "&m=" + mk;
                }
                // add access token if not null
                if (at != null)
                {
                    string eat = Uri.EscapeUriString(at);
                    accesstoken = eat;
                    serverstring += "&t=" + at;
                }
                if (clientAccessMode != null)
                {
                    serverstring += "&ca=" + clientAccessMode;
                }
                if (clientReadMode != null)
                {
                    serverstring += "&cr=" + clientReadMode;
                }
                if (clientWriteMode != null)
                {
                    serverstring += "&cw=" + clientWriteMode;
                }
                if (masterReadMode != null)
                {
                    serverstring += "&mr=" + masterReadMode;
                }
                if (masterWriteMode != null)
                {
                    serverstring += "&mw=" + masterWriteMode;
                }

                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    pipename = epn;
                    masterkey = x[1];
                }
                else
                {
                    lasterror = response;
                    accesstoken = null;
                    pipename = null;
                    masterkey = null;
                    return null;
                }
            }
            catch (Exception e)
            {
                accesstoken = null;
                pipename = null;
                masterkey = null;
                return null;
            }
            return Uri.UnescapeDataString(masterkey);
        }

        /// <summary>
        /// Close the pipe and tries to delete the Pipe on the server!; 
        /// </summary>
        /// <param name="pn"></param>
        /// <returns></returns>
        public async Task<bool> MasterClose()
        {
            if (masterkey == null)
            {
                return false;
            }
            try
            {
                string serverstring = server + "Close.php?p=" + pipename + "&m=" + masterkey;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    accesstoken = null;
                    pipename = null;
                    masterkey = null;
                    return true;
                }
                else
                {
                    lasterror = response;
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private int requestcounter = 0;

        public async Task<bool> MasterWrite(string mo)
        {
            try
            {
                string emo = Uri.EscapeUriString(mo);
                string serverstring = server + "MasterWrite.php?p=" + pipename + "&m=" + masterkey + "&mo=" + emo + "&x=" + requestcounter.ToString();
                requestcounter++;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return true;
                }
                else
                {
                    lasterror = response;
                    return false;
                }
            }
            catch (Exception)
            {
                // Handle exception.
            }
            return false;
        }

        public async Task<string> MasterRead()
        {
            try
            {
                string serverstring = server + "MasterRead.php?p=" + pipename + "&m=" + masterkey;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return Uri.UnescapeDataString(response.Substring(response.IndexOf(':') + 1));
                }
                else
                {
                    lasterror = response;
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<bool> MasterSetTrigger(string channel)
        {
            try
            {
                if (channel == null) return false;
                string echannel = Uri.EscapeDataString(channel);
                string serverstring = server + "MasterSetTrigger.php?p=" + pipename + "&m=" + masterkey + "&ch=" + echannel;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return true;
                }
                else
                {
                    lasterror = response;
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Trigger()
        {
            try
            {
                string serverstring = server + "Trigger.php?p=" + pipename;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                if (clientkey != null)
                {
                    serverstring += "&c=" + clientkey;
                }
                if (clientname != null)
                {
                    serverstring += "&cn=" + clientname;
                }

                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return true;
                }
                else
                {
                    lasterror = response;
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Write(string mi)
        {
            try
            {
                string emi = Uri.EscapeUriString(mi);
                string serverstring = server + "Write.php?p=" + pipename + "&m=" + masterkey + "&mi=" + emi;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                if (clientkey != null)
                {
                    serverstring += "&c=" + clientkey;
                }
                if (clientname != null)
                {
                    serverstring += "&cn=" + clientname;
                }
                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return true;
                }
                else
                {
                    lasterror = response;
                    return false;
                }
            }
            catch (Exception)
            {
                // Handle exception.
            }
            return false;
        }

        public async Task<string> Read()
        {
            try
            {
                string serverstring = server + "Read.php?p=" + pipename;
                if (accesstoken != null)
                {
                    serverstring += "&t=" + accesstoken;
                }
                if (clientkey != null)
                {
                    serverstring += "&c=" + clientkey;
                }
                if (clientname != null)
                {
                    serverstring += "&cn=" + clientname;
                }

                string response = await httpClient.GetStringAsync(serverstring);
                string[] x = response.Split(':');
                if (x[0].ToUpper().Contains("OK"))
                {
                    return Uri.UnescapeDataString(response.Substring(response.IndexOf(':') + 1));
                }
                else
                {
                    lasterror = response;
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
