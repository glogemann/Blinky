using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.UI.Xaml;

namespace Blinky
{
    public class ClientSensor : INotifyPropertyChanged
    {

        private string _name = "unkonwn";
        public string name { get { return _name; } set { _name = value; OnPropertyChanged(); } }

        private string _type = "unknown";
        public string type { get { return _type; } set { _type = value; OnPropertyChanged(); } }

        private string _info = "no info";
        public string info { get { return _info; } set { _info = value; OnPropertyChanged(); } }

        private string _accuarcy = "";
        public string accuarcy { get { return _accuarcy + "m"; } set { _accuarcy = value; OnPropertyChanged(); } }

        private string _altitude = "";
        public string altitude { get { return _altitude + "m"; } set { _altitude = value; OnPropertyChanged(); } }

        private string _source = "";
        public string source { get { return _source; } set { _source = value; OnPropertyChanged(); } }

        private string _altitudeaccuracy = "";
        public string altitudeaccuracy { get { return _altitudeaccuracy + "m"; } set { _altitudeaccuracy = value; OnPropertyChanged(); } }

        private string _description = "";
        public string description { get { return _description; } set { _description = value; OnPropertyChanged(); } }

        private string _gustdesc = "";
        public string gustdesc { get { return _gustdesc; } set { _gustdesc = value; OnPropertyChanged(); } }

        private string _gustspeed = "";
        public string gustspeed { get { return _gustspeed + " m/s"; } set { _gustspeed = value; OnPropertyChanged(); } }

        private string _heading = "";
        public string heading { get { return _heading + "°"; } set { _heading = value; OnPropertyChanged(); } }

        private string _idstation = "";
        public string idstation { get { return _idstation; } set { _idstation = value; OnPropertyChanged(); } }

        private string _latitude = "";
        public string latitude { get { return _latitude; } set { _latitude = value; OnPropertyChanged(); } }

        private string _longitude = "";
        public string longitude { get { return _longitude; } set { _longitude = value; OnPropertyChanged(); } }

        private string _pressure = "";
        public string pressure { get { return _pressure + "mbar"; } set { _pressure = value; OnPropertyChanged(); } }

        private string _speed = "";
        public string speed { get { return _speed + " m/s"; } set { _speed = value; OnPropertyChanged(); } }

        private string _temperature = "";
        public string temperature { get { return _temperature + "°C"; } set { _temperature = value; OnPropertyChanged(); } }

        private string _winddirection = "";
        public string winddirection { get { return _winddirection; } set { _winddirection = value; OnPropertyChanged(); } }

        private string _windspeed = "";
        public string windspeed { get { return _windspeed + "km/h"; } set { _windspeed = value; OnPropertyChanged(); } }

        private string _timestamp = "";
        public string timestamp { get { return _timestamp; } set { _timestamp = value; OnPropertyChanged(); } }

        private string _imageurl = "";
        public string imageurl { get { return _imageurl; } set { _imageurl = value; OnPropertyChanged(); } }

        private string _humidity = "";
        public string humidity { get { return _humidity + "%"; } set { _humidity = value; OnPropertyChanged(); } }

        private Windows.UI.Xaml.Visibility _isMapinfoVisible;
        public Windows.UI.Xaml.Visibility isMapinfoVisible
        {
            get { return _isMapinfoVisible; }
            set { _isMapinfoVisible = value; OnPropertyChanged(); }
        }

        //contact
        //distanceCurrentMotion
        //distancePace
        //distanceSpeed
        //distanceTotal
        //heartrate
        //heartrate_status
        //pedometer
        //skinTemperature
        //ultraviolet

        private string _contact = "";
        public string contact { get { return _contact; } set { _contact = value; OnPropertyChanged(); } }

        private string _distanceCurrentMotion = "";
        public string distanceCurrentMotion { get { return _distanceCurrentMotion; } set { _distanceCurrentMotion = value; OnPropertyChanged(); } }

        private string _distancePace = "";
        public string distancePace { get { return _distancePace; } set { _distancePace = value; OnPropertyChanged(); } }

        private string _distanceSpeed = "";
        public string distanceSpeed { get { return _distanceSpeed; } set { _distanceSpeed = value; OnPropertyChanged(); } }

        private string _distanceTotal = "";
        public string distanceTotal { get { return _distanceTotal; } set { _distanceTotal = value; OnPropertyChanged(); } }

        private string _heartrate = "";
        public string heartrate { get { return _heartrate; } set { _heartrate = value; OnPropertyChanged(); } }

        private string _heartrate_status = "";
        public string heartrate_status { get { return _heartrate_status; } set { _heartrate_status = value; OnPropertyChanged(); } }

        private string _skinTemperature = "";
        public string skinTemperature { get { return _skinTemperature; } set { _skinTemperature = value; OnPropertyChanged(); } }

        private string _pedometer = "";
        public string pedometer { get { return _pedometer; } set { _pedometer = value; OnPropertyChanged(); } }

        private string _ultraviolet = "";
        public string ultraviolet { get { return _ultraviolet; } set { _ultraviolet = value; OnPropertyChanged(); } }



#if WINDOWS_PHONE_APP
        MapIcon pin = null;

#elif WINDOWS_APP
        public Pushpin pin = null;
#endif

        DispatcherTimer sensorTimer = null;

        private string clientName = null;
        private string clientKey = null;
        private string accessToken = null;
        private string pipeName = null;
        private TimeSpan interval;
        private TimeSpan oldinterval;

        private Sensorbook sb = null;

        public ClientSensor(string cn, string ck, string at, string pn, TimeSpan t)
        {
            clientName = cn;
            clientKey = ck;
            accessToken = at;
            pipeName = pn;
            interval = t;
            oldinterval = t;
            if (interval == null) { interval = new TimeSpan(0, 0, 1); }
            sb = new Sensorbook();
        }

        public async Task<bool> StartSensor()
        {
            bool result = await sb.OpenClient(pipeName, clientName, clientKey, accessToken);
            if (!result)
            {
                return false;
            }
            sensorTimer = new DispatcherTimer();
            sensorTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);// interval;
            sensorTimer.Tick += sensorTimer_Tick;
            sensorTimer.Start();

            return true;
        }

        public class sensor
        {
            [JsonProperty("type")]
            public string type { get; set; }

            [JsonProperty("name")]
            public string name { get; set; }

            [JsonProperty("longitude")]
            public string longitude { get; set; }

            [JsonProperty("latitude")]
            public string latitude { get; set; }

            [JsonProperty("speed")]
            public string speed { get; set; }

            [JsonProperty("accuracy")]
            public string accuracy { get; set; }

            [JsonProperty("altitude")]
            public string altitude { get; set; }

            [JsonProperty("source")]
            public string source { get; set; }

            [JsonProperty("altitudeaccuracy")]
            public string altitudeaccuracy { get; set; }

            [JsonProperty("heading")]
            public string heading { get; set; }

            [JsonProperty("windspeed")]
            public string windspeed { get; set; }

            [JsonProperty("pressure")]
            public string pressure { get; set; }

            [JsonProperty("gustdesc")]
            public string gustdesc { get; set; }

            [JsonProperty("gustspeed")]
            public string gustspeed { get; set; }

            [JsonProperty("description")]
            public string description { get; set; }

            [JsonProperty("idstation")]
            public string idstation { get; set; }

            [JsonProperty("winddirection")]
            public string winddirection { get; set; }

            [JsonProperty("temperature")]
            public string temperature { get; set; }

            [JsonProperty("timestamp")]
            public string timestamp { get; set; }

            [JsonProperty("imageurl")]
            public string imageurl { get; set; }

            [JsonProperty("humidity")]
            public string humidity { get; set; }

            [JsonProperty("contact")]
            public string contact { get; set; }

            [JsonProperty("distanceCurrentMotion")]
            public string distanceCurrentMotion { get; set; }

            [JsonProperty("distancePace")]
            public string distancePace { get; set; }

            [JsonProperty("distanceSpeed")]
            public string distanceSpeed { get; set; }

            [JsonProperty("distanceTotal")]
            public string distanceTotal { get; set; }

            [JsonProperty("heartrate")]
            public string heartrate { get; set; }

            [JsonProperty("heartrate_status")]
            public string heartrate_status { get; set; }

            [JsonProperty("pedometer")]
            public string pedometer { get; set; }

            [JsonProperty("skinTemperature")]
            public string skinTemperature { get; set; }

            [JsonProperty("ultraviolet")]
            public string ultraviolet { get; set; }

            //contact
            //distanceCurrentMotion
            //distancePace
            //distanceSpeed
            //distanceTotal
            //heartrate
            //heartrate_status
            //pedometer
            //skinTemperature
            //ultraviolet

            //position Message:{"accuracy":"11","altitude":"466","altitudeaccuracy":"6","heading":"NaN","latitude":"48.3204138278961","longitude":"11.7410568147898","name":"GunterL Phone","source":"Satellite","speed":"0","timestamp":"635508178375832635","type":"position","version":"1.0"}</h3>  
            //dwdobservationsensor:{"windspeed":"---","pressure":"---","longitude":"-8.404722","version":"1.0","name":"LaCoruna","gustdesc":"---","gustspeed":"---","description":"---","idstation":"40","timestamp":1415221392,"latitude":"43.362778","type":"dwdobservationsensor","winddirection":"---","temperature":"---","altitude":"67"}</h3>  
        }

        public sensor Sensor = null;

        public async void sensorTimer_Tick(object sender, object e)
        {
            sensorTimer.Stop();

            string result = await sb.Read();


            try
            {
                Sensor = new sensor();

                if (result.Contains("heartrate"))
                {
                    int r = result.IndexOf("heartrate");
                    string s1 = result.Substring(r);
                    string s2 = s1.Substring(12, 2);

                    Sensor.heartrate = s2;

                    //int i = Convert.ToInt16(s2); 
                }
                else
                {
                    return; 
                }

                //Sensor = JsonConvert.DeserializeObject<sensor>(result);

                type = Sensor.type;
                if (type == "FS20Home")
                {

                }
                name = Sensor.name;
                accuarcy = Sensor.accuracy;
                altitude = Sensor.altitude;
                source = Sensor.source;
                altitudeaccuracy = Sensor.altitudeaccuracy;
                description = Sensor.description;
                gustdesc = Sensor.gustdesc;
                gustspeed = Sensor.gustspeed;
                heading = Sensor.heading;
                idstation = Sensor.idstation;
                latitude = Sensor.latitude;
                longitude = Sensor.longitude;
                pressure = Sensor.pressure;
                speed = Sensor.speed;
                temperature = Sensor.temperature;
                winddirection = Sensor.winddirection;
                windspeed = Sensor.windspeed;
                timestamp = Sensor.timestamp;
                imageurl = Sensor.imageurl;
                humidity = Sensor.humidity;

                contact = Sensor.contact;
                distanceCurrentMotion = Sensor.distanceCurrentMotion;
                distancePace = Sensor.distancePace;
                distanceSpeed = Sensor.distanceSpeed;
                distanceTotal = Sensor.distanceTotal;
                heartrate = Sensor.heartrate;
                heartrate_status = Sensor.heartrate_status;
                pedometer = Sensor.pedometer;
                skinTemperature = Sensor.skinTemperature;
                ultraviolet = Sensor.ultraviolet;

                info = result;



                OnPropertyChanged("pinChange");
            }
            catch
            {
            }

            sensorTimer.Interval = interval;
            sensorTimer.Start();

            //throw new NotImplementedException();
        }

        public bool StopSensor()
        {
            sensorTimer.Stop();
            return true;
        }

        public bool RunMax()
        {
            sensorTimer.Stop();
            interval = new TimeSpan(0, 0, 0, 0, 100);
            sensorTimer.Start();
            return true;
        }

        public bool RestoreInterval()
        {
            sensorTimer.Stop();
            interval = oldinterval;
            sensorTimer.Start();
            return true;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
