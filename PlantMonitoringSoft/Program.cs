using Gadgeteer.Modules.GHIElectronics;
using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Text;
using System.IO;
using System.Net;

namespace GadgeteerPlantMonitor
{
    public partial class Program
    {
        // This method is run when the mainboard is powered up or reset.   
        string progName;

        GT.Timer timer;

        /* measures taken */
        double light;

        /* Ethernet connection */
        static bool IsNetworkUp = false;

        //DigitalOutput waterPump;


        void ProgramStarted()
        {
            progName = "Plant Monitoring System";
            Debug.Print("Using name: " + progName);
            Debug.Print("Program Started");

            // Close water pump
            //waterPump = breakout.CreateDigitalOutput(GT.Socket.Pin.Three, true);

            ethernetJ11D.DebugPrintEnabled = true;
            ethernetJ11D.NetworkDown += new GTM.Module.NetworkModule.NetworkEventHandler(ethernet_NetworkDown);
            ethernetJ11D.NetworkUp += new GTM.Module.NetworkModule.NetworkEventHandler(ethernet_NetworkUp);
            initEthernetConnection();

            timer = new GT.Timer(10000); //5 seconds
            timer.Tick += new GT.Timer.TickEventHandler(timer_getSensorData);
            timer.Stop();

            /*
            displayTE35.SimpleGraphics.DisplayText(progName,
                Resources.GetFont(Resources.FontResources.NinaB), GT.Color.Red, 5, 5);

            light = lightSense.GetIlluminance();
            displayTE35.SimpleGraphics.DisplayRectangle(GT.Color.Black, 2, GT.Color.Black, 0, 25, 250, 20);
            displayTE35.SimpleGraphics.DisplayTextInRectangle("Light: " + light + " Lux", 5, 30, 250, 40, GT.Color.Orange, Resources.GetFont(Resources.FontResources.small));
            */

            timer.Start();
        }


        void timer_getSensorData(GT.Timer timer)
        {
            timer.Stop();

            if (IsNetworkUp)
                changePumpStatus("off");

            light = lightSense.GetIlluminance();
            Debug.Print("Light: " + light + " Lux");

            if (IsNetworkUp)
            {
                //SendSensorData("light", light.ToString());
            }

            timer.Start();
        }

        void ethernet_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            IsNetworkUp = false;
            Debug.Print("Network down.");
        }

        void ethernet_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            IsNetworkUp = true;
            Debug.Print("Network up.");


        }

        private void initEthernetConnection()
        {
            Debug.Print("Initializing Ethernet connection...");

            if (!ethernetJ11D.NetworkInterface.Opened)
                ethernetJ11D.NetworkInterface.Open();

            if (!ethernetJ11D.NetworkInterface.CableConnected)
                Debug.Print("Ethernet cable disconnected");

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in networkInterfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    //ni.EnableDhcp();
                    ni.EnableStaticIP("192.168.1.2", "255.255.255.0", "");
                    //ni.EnableStaticDns(new string[] { "8.8.8.8", "8.8.4.4" });
                }


            }

            var settings = ethernetJ11D.NetworkSettings;

            Debug.Print("IP Address: " + settings.IPAddress);
            Debug.Print("DHCP Enabled: " + settings.IsDhcpEnabled);
            Debug.Print("Subnet Mask: " + settings.SubnetMask);
            Debug.Print("Gateway: " + settings.GatewayAddress);

        }

        public void SendSensorData(string sensorID, string value)
        {
            string url = "http://192.168.1.200:8733/Service1.svc/";
            string values = sensorID + "/data?value=" + value;
            Debug.Print("HTTP POST to: " + url + values);
            POSTContent content = POSTContent.CreateTextBasedContent(values);
            var req = HttpHelper.CreateHttpPostRequest(url, content, "application/x-www-form-urlencoded");
            //POSTContent content = POSTContent.CreateTextBasedContent(values);

            req.ResponseReceived += new HttpRequest.ResponseHandler(req_ResponseReceived);
            req.SendRequest();
        }

        public void changePumpStatus(string newStatus)
        {
            string url = "http://192.168.1.200:8733/Service1.svc/";
            string status = "waterPump?open=" + newStatus;
            Debug.Print("HTTP PUT to: " + url + status);
            PUTContent content = PUTContent.CreateTextBasedContent(status);
            var req = HttpHelper.CreateHttpPutRequest(url, content, "application/x-www-form-urlencoded");

            req.ResponseReceived += new HttpRequest.ResponseHandler(req_ResponseReceived);
            req.SendRequest();
        }


        void req_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode != "200")
            {
                Debug.Print("HTTP request failed with status code: " + response.StatusCode);
            }
            else
            {
                Debug.Print("HTTP request ok!");
            }
        }
    }
}
