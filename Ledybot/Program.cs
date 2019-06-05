using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using LedyLib;

namespace Ledybot
{
    static class Program
    {
        public static NTR ntrClient;
        public static Data data;
        public static GTSBot7 gtsBot;
        public static EggBot eggBot;
        public static ScriptHelper scriptHelper;
        public static RemoteControl helper;
        public static MainForm f1;
        public static LookupTable PKTable;
        public static PKHeX pkhex;
        public static GiveawayDetails gd;
        public static BanlistDetails bld;
        public static List<KeyValuePair<string, ArrayList>> ServerList = new List<KeyValuePair<string, ArrayList>>();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ntrClient = new NTR();
            ntrClient.DataReady += NTR.handleDataReady;
            PKTable = new LookupTable();
            pkhex = new PKHeX();
            data = new Data();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            f1 = new MainForm();
            gd = new GiveawayDetails();
            bld = new BanlistDetails();
            scriptHelper = new ScriptHelper(ntrClient);
            scriptHelper.onAutoDisconnect += f1.startAutoDisconnect;
            helper = new RemoteControl(scriptHelper, ntrClient);
            helper.onDumpedPKHeXData += setDumpedData;


            Application.Run(f1);

        }

        public static void createGTSBot(string szIP, int iP, int iPtF, int iPtFGender, int iPtFLevel, bool bBlacklist, bool bReddit, int iSearchDirection, string waittime, string consoleName, bool useLedySync, string ledySyncIp, string ledySyncPort, int game, bool tradeQueue)
        {
            gtsBot = new GTSBot7(ntrClient, szIP, iP, iPtF, iPtFGender, iPtFLevel, bBlacklist, bReddit, iSearchDirection, waittime, consoleName, useLedySync, ledySyncIp, ledySyncPort, game, tradeQueue, helper, PKTable, data, scriptHelper);
            gtsBot.onChangeStatus += f1.ChangeStatus;
            gtsBot.onItemDetails += f1.ReceiveItemDetails;
            Data.GtsBot7 = gtsBot;
        }

        public static void createEggBot(int iP, int game)
        {
            eggBot = new EggBot(iP, game, helper);
        }

        static void setDumpedData(byte[] data)
        {
            f1.dumpedPKHeX.Data = data;
        }


        public static async void createTcpClient(Int32 port)
        {
            string host = "127.0.0.1";
            int timeout = 5000;

            while (true) //continuously trys to reconnect
            {
                try //catches any errors that may occur
                {
                    TcpClient client = new TcpClient();

                    NetworkStream netstream;
                    StreamReader reader;
                    StreamWriter writer;

                    await client.ConnectAsync(host, port); //connects to the host on port specified
                    netstream = client.GetStream();

                    //gets the stream reader and writer
                    reader = new StreamReader(netstream);
                    writer = new StreamWriter(netstream);


                    //sets details for the connection
                    writer.AutoFlush = true;

                    netstream.ReadTimeout = timeout;

                    String serverName = host + ":" + port.ToString(); //create the name of the server for the serverList
                    
                    foreach (var pair in ServerList)
                    {
                        if (pair.Key == serverName)
                        {
                            pair.Value.Add(writer);
                            return;
                        }
                    }

                    ArrayList newClient = new ArrayList
                    {
                        writer
                    };

                    ServerList.Add(new KeyValuePair<string, ArrayList>(serverName, newClient));


                    f1.SendConsoleMessage("Connection Received."); 
                    while (true) //start reading for messages
                    {
                        String response = await reader.ReadLineAsync();

                        f1.ExecuteCommand(response, false, writer);
                        f1.SendConsoleMessage("Message Received: " + response);
                    }


                }
                catch (Exception e)
                {
                    f1.SendConsoleMessage(e.StackTrace);
                    f1.SendConsoleMessage("Reconnecting");
                }
            }
        }

    }

}
