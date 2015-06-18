/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.SerialCommunication;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Devices.HumanInterfaceDevice;
using System.Diagnostics;
using System.Collections.Generic;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using Amqp;
using Amqp.Framing;
using Newtonsoft.Json;
using System.Threading;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Blinky
{
    public sealed partial class MainPage : Page
    {
        Sensorbook sb = null; 
        public MainPage()
        {
            InitializeComponent();

            UpdateCharts();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();

            Unloaded += MainPage_Unloaded;

            InitGPIO();

        }
        public class NameValueItem
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        private const string I2C_CONTROLLER_NAME = "I2C1"; //specific to RPI2
        private const byte MD23_I2C_ADDRESS = 0x58; // 7-bit I2C address of the MD23
        private const byte MD23_SPEED1_REGISTER_ADDRESS = 0x00; 
        private const byte MD23_SPEED2_REGISTER_ADDRESS = 0x01;
        private const byte MD23_ENC1A_REGISTER_ADDRESS = 0x02;
        private const byte MD23_ENC1B_REGISTER_ADDRESS = 0x03;
        private const byte MD23_ENC1C_REGISTER_ADDRESS = 0x04;
        private const byte MD23_ENC1D_REGISTER_ADDRESS = 0x05;
        private const byte MD23_ENC2A_REGISTER_ADDRESS = 0x06;
        private const byte MD23_ENC2B_REGISTER_ADDRESS = 0x07;
        private const byte MD23_ENC2C_REGISTER_ADDRESS = 0x08;
        private const byte MD23_ENC2D_REGISTER_ADDRESS = 0x09; 
        private const byte MD23_BATTERY_REGISTER_ADDRESS = 0x0A;
        private const byte MD23_MOTOR1_CURRENT_REGISTER_ADDRESS = 0x0B;
        private const byte MD23_MOTOR2_CURRENT_REGISTER_ADDRESS = 0x0C;
        private const byte MD23_VERSION_REGISTER_ADDRESS = 0x0D;
        private const byte MD23_ACCEL_RATE_REGISTER_ADDRESS = 0x0E;
        private const byte MD23_MODE_REGISTER_ADDRESS = 0x0F;
        private const byte MD23_COMMAND_REGISTER_ADDRESS = 0x10;

        private I2cDevice i2cMD23;

        private byte[] i2cReadBuffer = new byte[3];

        private Random _random = new Random();

        private List<NameValueItem> items = new List<NameValueItem>();

        private void UpdateCharts()
        {
            ((LineSeries)this.RateChart.Series[0]).ItemsSource = items;
        }


        private async void InitI2C()
        {
            // initialize I2C communications
            try
            {
                var i2cSettings = new I2cConnectionSettings(MD23_I2C_ADDRESS);
                i2cSettings.BusSpeed = I2cBusSpeed.StandardMode;
                string deviceSelector = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                i2cMD23 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception: {0}", e.Message);
                return;
            }
            i2cMD23.WriteRead(new byte[] { MD23_BATTERY_REGISTER_ADDRESS }, i2cReadBuffer);
            byte x = i2cReadBuffer[0];

            I2CStatus.Text = "MD23 initalized ("+(Convert.ToDouble(x)/10).ToString()+"V)";

            byte[] i2cWriteBuffer = new byte[2];

            i2cWriteBuffer[0] = MD23_MODE_REGISTER_ADDRESS;
            i2cWriteBuffer[1] = 0x02;
            i2cMD23.WriteRead(i2cWriteBuffer, i2cReadBuffer);
            byte x1 = i2cReadBuffer[0];

            i2cWriteBuffer[0] = MD23_SPEED1_REGISTER_ADDRESS;
            i2cWriteBuffer[1] = 255;
            i2cMD23.WriteRead(i2cWriteBuffer, i2cReadBuffer);
            byte x2 = i2cReadBuffer[0];


        }

        private static XboxHidController controller;
        private static int lastControllerCount = 0;

        private async void InitController()
        {
            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);
            if (deviceInformationCollection.Count == 0)
            {
                Debug.WriteLine("No Xbox360 controller found!");
            }
            lastControllerCount = deviceInformationCollection.Count;
            foreach (DeviceInformation d in deviceInformationCollection)
            {
                Debug.WriteLine("Device ID: " + d.Id);
                HidDevice hidDevice = await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read);
                if (hidDevice == null)
                {
                    try
                    {
                        var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;

                        if (!deviceAccessStatus.Equals(DeviceAccessStatus.Allowed))
                        {
                            Debug.WriteLine("DeviceAccess: " + deviceAccessStatus.ToString());
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Xbox init - " + e.Message);
                    }
                    Debug.WriteLine("Failed to connect to the controller!");
                    return; 
                }
                hidDevice.InputReportReceived += HidDevice_InputReportReceived;

                //controller = new XboxHidController(hidDevice);
                //controller.DirectionChanged += Controller_DirectionChanged;
            }

        }

        private void HidDevice_InputReportReceived(HidDevice sender, HidInputReportReceivedEventArgs args)
        {
            // Adjust X/Y so (0,0) is neutral position
            double stickX = args.Report.GetNumericControl(0x01, 0x30).Value - 32768;
            double stickY = args.Report.GetNumericControl(0x01, 0x31).Value - 32768;

            Debug.WriteLine("x:" + stickX.ToString() + "y:" + stickY.ToString());
        }



        private void InitGPIO()
        {

            //IReadOnlyDictionary retailInfo = Windows.System.Profile.RetailInfo.Properties;
            //var x1 = Windows.System.Profile.RetailInfo.Properties[Windows.System.Profile.KnownRetailInfoProperties.WindowsEdition];
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(LED_PIN);

            // Show an error if the pin wasn't initialized properly
            if (pin == null)
            {
                GpioStatus.Text = "There were problems initializing the GPIO pin.";
                return;
            }

            pin.Write(GpioPinValue.High);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly.";
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            pin.Dispose();
        }

        private void FlipLED()
        {
            if (LEDStatus == 0)
            {
                LEDStatus = 1;
                if (pin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    pin.Write(GpioPinValue.Low);
                }
                LED.Fill = redBrush;
            }
            else
            {
                LEDStatus = 0;
                if (pin != null)
                {
                    pin.Write(GpioPinValue.High);
                }
                LED.Fill = grayBrush;
            }
        }

        private void TurnOffLED()
        {
            if (LEDStatus == 1)
            {
                FlipLED();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            FlipLED();
        }

        private void Delay_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {            
            if (timer == null)
            {
                return;
            }
            if (e.NewValue == Delay.Minimum)
            {
                DelayText.Text = "Stopped";
                timer.Stop();
                TurnOffLED();
            }
            else
            {
                DelayText.Text = e.NewValue + "ms";
                
                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                timer.Start();

            }
        }

        private int LEDStatus = 0;
        private const int LED_PIN = 5;
        private GpioPin pin;
        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        private DispatcherTimer MainTimer = null; 

        private ClientSensor c = null;
        private Grove10DoF sensor = null; 
    

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //InitController();

            sensor = new Grove10DoF();
            if (await sensor.InitSensor() == true)
            {
                I2CStatus.Text = "BMP180 found";
            }
            else
            {
                I2CStatus.Text = "I2C Error";
            }

            AppEdgeGateway = "Blaster";
            sAppAMQPAddress = "amqps://D1:Mk3ByiWAp%2FD1SnpR1O4vhZctGMS0rSowaicOLnQegfw%3D@mygunterltest-ns.servicebus.windows.net";
            AppAMQPAddress = new Address(sAppAMQPAddress);
            AppEHTarget = "ehdevices";

            InitAMQPConnection();



            MainTimer = new DispatcherTimer();
            MainTimer.Interval = new TimeSpan(0, 0, 0, 1,0);
            MainTimer.Tick += MainTimer_Tick;
            MainTimer.Start();

            //InitI2C();

            //c = new ClientSensor(null, null, "123", "X8f47ec8b56640469ba602a77716695b4", new TimeSpan(0, 0, 0, 1, 0));
            c = new ClientSensor(null, null, "123", "X5ec1d960158f2f534845863ff482076b", new TimeSpan(0, 0, 0, 1, 0));
            c.PropertyChanged += C_PropertyChanged;
            if (await c.StartSensor()) {
                SBStatus.Text = "connected"; 
            };


        }

        private int rate = 50; 

        private async void MainTimer_Tick(object sender, object e)
        {
            if (items.Count > 25)
            {
                items.RemoveAt(0);
            }
            items.Add(new NameValueItem { Name = x.ToString(), Value = rate });
            x++;

            RateChart.LegendItems.Clear();

            ((LineSeries)this.RateChart.Series[0]).Refresh();

            //uint utemp = await sensor.BMP180ReadUT();
            float temp = sensor.BMP180GetTemp(await sensor.BMP180ReadUT());
            //ulong upres = await sensor.BMP180ReadUP();
            long press = sensor.BMP180GetPressure(await sensor.BMP180ReadUP());

            float alt = sensor.BMP180CalcAltitude(press); 
            
            float atm = press * 1000 / 101325;
            

            I2CStatus.Text = "BMP180: Temp: " + temp.ToString() +" °C --- Pressure: " + atm.ToString() +" mbar ("+ press.ToString() + " Pa) --- Alt: "+ alt.ToString() + " m" ; 

        }

        private int x = 0;
        private int x1 = 0; 

        private void C_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            

            if (e.PropertyName == "Type")
            {

            }

            if (e.PropertyName == "heartrate")
            {
                double i = Convert.ToDouble(c.heartrate);
                i = (60 / i) * 1000 / 2;

                rate = Convert.ToInt32(c.heartrate);
                SBStatus.Text = "connected! (" + x1.ToString() +")";

                x1++;

            DelayText.Text = rate.ToString() + "bpm (" + i.ToString() + "ms)";
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(i);
                timer.Start();
            }


        }

        // Unique identifier for the gateway (will be generated when app starts)
        // You might want to hard code it or put this ID in the configuration file if you need the Gateway to not change ID at each reboot
        public static string deviceId;

        // Variables for AMQPs connection
        public static Connection connection = null;
        public static Session session = null;
        public static SenderLink sender = null;
        public static ReceiverLink receiver = null;
        // We have several threads that will use the same SenderLink object
        // we will protect the access using InterLock.Exchange 0 for false, 1 for true. 
        private static int sendingMessage = 0;

        public static string AppEdgeGateway;
        public static Address AppAMQPAddress;
        public static string sAppAMQPAddress;
        public static string AppEHTarget;


        /// <summary>
        /// Initialize AMQP connection
        /// we are using the connection to send data to Azure Event Hubs
        /// Connection information is retreived from the app configuration file
        /// </summary>
        /// <returns>
        /// true when successful
        /// false when unsuccessful
        /// </returns>
        public static bool InitAMQPConnection()
        {
            // Initialize AMQPS connection
            try
            {
                connection = new Connection(AppAMQPAddress);
                session = new Session(connection);
                sender = new SenderLink(session, "send-link", AppEHTarget);

                receiver = new ReceiverLink(session, "receive-link", AppEHTarget);
                receiver.SetCredit(100);
            }
            catch (Exception e)
            {
                if (sender != null) sender.Close();
                if (session != null) session.Close();
                if (connection != null) connection.Close();
                return false;
            }
            return true;
        }


        /// <summary>
        /// Send a string as an AMQP message to Azure Event Hub
        /// </summary>
        /// <param name="valuesJson">
        /// String to be sent as an AMQP message to Event Hub
        /// </param>
        public static void SendAmqpMessage(string valuesJson)
        {
            // If there is no value passed as parameter, do nothing
            if (valuesJson == null) return;

            // Deserialize Json message
            var sample = JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesJson);
            if (sample == null)
            {
                return;
            }
            // Convert JSON data in 'sample' into body of AMQP message
            // Only data added by gateway is time of message (since sensor may not have clock) 
            //deviceId = Convert.ToString(sample["DeviceGUID"]);      // Unique identifier from sensor, to group items in event hub

            Dictionary<string, object> s;
             

            deviceId = "RASPPI";

            Message message = new Message();
            message.Properties = new Properties()
            {
                //Subject = Convert.ToString(sample["Subject"]),              // Message type (e.g. "wthr") defined in sensor code, sent in JSON payload
                Subject = "Test",
                CreationTime = DateTime.UtcNow, // Time of data sampling
            };

            message.MessageAnnotations = new MessageAnnotations();
            // Event Hub partition key: device id - ensures that all messages from this device go to the same partition and thus preserve order/co-location at processing time
            //            message.MessageAnnotations[new Symbol("x-opt-partition-key")] = deviceId;
            message.MessageAnnotations["x-opt-partition-key"] = deviceId;
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["time"] = message.Properties.CreationTime;
            message.ApplicationProperties["from"] = deviceId; // Originating device
            message.ApplicationProperties["dspl"] = "Sensor1"; // sample["dspl"];      // Display name for originating device defined in sensor code, sent in JSON payload

            if (sample != null && sample.Count > 0)
            {
                var outDictionary = new Dictionary<string, object>(sample);
                outDictionary["Subject"] = message.Properties.Subject; // Message Type
                outDictionary["time"] = message.Properties.CreationTime;
                outDictionary["from"] = deviceId; // Originating device
                outDictionary["dspl"] = "Sensor1"; // sample["dspl"];      // Display name for originating device
                message.Properties.ContentType = "text/json";
                message.Body = new Data() { Binary = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outDictionary)) };

            }
            else
            {
                // No data: send an empty message with message type "weather error" to help diagnose problems "from the cloud"
                message.Properties.Subject = "wthrerr";
            }

            // Send to the cloud asynchronously
            // Obtain handle on AMQP sender-link object
            if (0 == Interlocked.Exchange(ref sendingMessage, 1))
            {
                sender.Send(message, SendOutcome, null);
                //sender.Send(message, SendOutcome, null);
                Interlocked.Exchange(ref sendingMessage, 0);
            }
        }

        /// <summary>
        /// Callback function used to report on AMQP message send 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="outcome"></param>
        /// <param name="state"></param>
        public async static void SendOutcome(Message message, Outcome outcome, object state)
        {
            if (outcome is Accepted)
            {
                //await App.ViewModel.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //{
                //    App.ViewModel.StatusText = "Sensor Update (AMQP)" + App.ViewModel.counter.ToString();
                //    App.ViewModel.counter++;
                //    App.ViewModel.mainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                //    App.ViewModel.mainTimer.Start();
                //});
            }
            else
            {

            }
        }

        private class PositionData
        {
            [DataMember]
            public string name;
            [DataMember]
            public string type;
            [DataMember]
            public string version;
            [DataMember]
            public string timestamp;
            [DataMember]
            public string longitude = "0.0";
            [DataMember]
            public string latitude = "0.0";
            [DataMember]
            public string speed = "unknown";
            [DataMember]
            public string heading = "unknown";
            [DataMember]
            public string source = "unknown";
            [DataMember]
            public string accuracy = "unknown";
            [DataMember]
            public string altitude = "unknown";
            [DataMember]
            public string altitudeaccuracy = "unknown";
            [DataMember]
            public String contact = "unknown";
            [DataMember]
            public String ultraviolet = "unknown";
            [DataMember]
            public String skinTemperature = "unknown";
            [DataMember]
            public String pedometer = "unknown";
            [DataMember]
            public String distanceCurrentMotion = "unknown";
            [DataMember]
            public String distancePace = "unknown";
            [DataMember]
            public String distanceSpeed = "unknown";
            [DataMember]
            public String distanceTotal = "unknown";
            [DataMember]
            public String heartrate = "unknown";
            [DataMember]
            public String heartrate_status = "unknow";
        }

        private PositionData positionData = new PositionData();

        private string _DataString = "no Data";
        public string DataString { get { return _DataString; } set { _DataString = value; } }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            //MemoryStream stream1 = new MemoryStream();
            //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(PositionData));

            //positionData.type = "position";
            //positionData.version = "1.0";
            //positionData.name = "Sensor";

            //ser.WriteObject(stream1, positionData);
            //stream1.Position = 0;
            //StreamReader sr = new StreamReader(stream1);
            //string s = sr.ReadToEnd();

            string s = "Hallo Data";
            //if (Mode == false)
            //{
            //    bool result = await sb.MasterWrite(s);
            //    if (!result)
            //    {
            //        StatusText = "Sensor Update (error) " + counter.ToString();
            //    }
            //    else
            //    {
            //        StatusText = "Sensor Update " + counter.ToString();
            //        DataString = s;
            //        mainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            //        mainTimer.Start();
            //    }
            //    counter++;

            //}
            //else
            //{

                string msgout = Uri.EscapeUriString(s);
                Random r = new Random();

                string valuesJson = String.Format("{{ \"pipename\" : \"{0}\", \"masterkey\" : \"{1}\", \"lght\" : {2}, \"messageout\" : \"{3}\"}}",
                "Hallo",
                "HalloKey",
                (r.NextDouble() * 100),
                msgout);
                try
                {
                    // Send JSON message to the Cloud
                    SendAmqpMessage(valuesJson);
                }
                catch (Exception ex)
                {
                }
           // }

        }
    }
}
