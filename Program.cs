using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using ELO;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace WsChatServer
{
    class ChatSession : WsSession
    {
        public ChatSession(WsServer server) : base(server) { }

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from WebSocket chat! Please send a message or '!' to disconnect the client!";
            SendTextAsync(message);

            //Retrieve the pseudo-singleton db instance
            IDatabase db = ((WsServer)Server)._redis.GetDatabase();

            //refresh and un-stale the Redis db on a new user connection
            int keyValue;
            int i = 1;
            while (i < 8)
            {
                //refresh array object 
                keyValue = i;

                var hashEntries = db.HashGetAll("user:" + keyValue);

                foreach (var item in hashEntries)
                {
                    SendTextAsync($"Key: {item.Name}, Value:{item.Value}");
                }

                i++;
            }
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {

            string requestIP = this.Request.XRealIP();
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            //Console.WriteLine($"Incoming {theirIP}: " + message);

            // Multicast message to all connected sessions
            if (!message.Contains("/cc"))
            { 
                ((WsServer)Server).MulticastText(message, this.Id);
            }
            else
            {
                //find the command 
                //catan is used as an example game to target
                if (message.Contains("/cc elo catan "))
                {
                    //update the stats of the users in the database
                    //first parse the data

                    List<string> parseMessage = message.Trim().Remove(0, 13).Trim().Split(' ').ToList();
                    List<ELOPlayerObject> currentPlayers = new List<ELOPlayerObject>();
                    int i = 1;

                    foreach (var v in parseMessage)
                    {

                        List<string> tempList = v.Trim().Split(':').ToList();
                        
                        currentPlayers.Add(new ELOPlayerObject(tempList[0], i, Int32.Parse(tempList[1])));
                        
                        i++;
                    }

                    //now get the remaining information from the database for each player
                    IDatabase db = ((WsServer)Server)._redis.GetDatabase();
                    
                    //int keyValue;
                    i=1;
                    while (i < 5)
                    {
                        //get the users hash list from the given name
                        //no await since await overhead is higher than redis access on same server
                        //if redis was serverless then await this
                        var hashEntries = db.HashGetAll(currentPlayers[i - 1].Name);

                        //get the users ID
                        foreach(var item in hashEntries)
                        {
                            currentPlayers[i-1].Id = Int32.Parse(item.Value);
                        }
                        
                        //now with their id, we can get the real data
                        hashEntries = db.HashGetAll("user:" + currentPlayers[i - 1].Id);

                        //now to parse their info into the currentPlayers object
                        currentPlayers[i - 1].ELO = Int32.Parse(hashEntries[1].Value);
                        currentPlayers[i - 1].Games = Int32.Parse(hashEntries[2].Value);
                        currentPlayers[i - 1].Wins = Int32.Parse(hashEntries[3].Value);
                        currentPlayers[i - 1].Losses = Int32.Parse(hashEntries[4].Value);
                        currentPlayers[i - 1].Top2 = Int32.Parse(hashEntries[5].Value);
                        currentPlayers[i - 1].Top3 = Int32.Parse(hashEntries[6].Value);
                        currentPlayers[i - 1].Avgvp = Int32.Parse(hashEntries[7].Value); 

                        i++;
                    }
                    //end loop
                    

                    //process it with elo
                    
                    ELOMatch match = new ELOMatch();
                    match.addPlayer(currentPlayers[0].Name, currentPlayers[0].Position, currentPlayers[0].ELO);
                    match.addPlayer(currentPlayers[1].Name, currentPlayers[1].Position, currentPlayers[1].ELO);
                    match.addPlayer(currentPlayers[2].Name, currentPlayers[2].Position, currentPlayers[2].ELO);
                    match.addPlayer(currentPlayers[3].Name, currentPlayers[3].Position, currentPlayers[3].ELO);
                    match.calculateELOs();
                    

                    
                    
                    //save and update 
                    i = 1;
                    while(i<5)
                    {
                        //put the new calculated values into the currentPlayers object
                        currentPlayers[i-1].ELO = match.getELO(currentPlayers[i-1].Name);
                        currentPlayers[i-1].Games = currentPlayers[i - 1].Games + 1;

                        if (currentPlayers[i-1].Position == 1)
                        {
                            currentPlayers[i-1].Wins = currentPlayers[i-1].Wins + 1;
                        }

                        if (currentPlayers[i-1].Position != 1)
                        {
                            currentPlayers[i-1].Losses = currentPlayers[i-1].Losses + 1;
                        }

                        if (currentPlayers[i - 1].Position == 2 || currentPlayers[i - 1].Position == 1)
                        {
                            currentPlayers[i - 1].Top2 = currentPlayers[i - 1].Top2 + 1;
                        }

                        if (currentPlayers[i - 1].Position == 3 || currentPlayers[i - 1].Position == 2 || currentPlayers[i - 1].Position == 1)
                        {
                            currentPlayers[i - 1].Top3 = currentPlayers[i - 1].Top3 + 1;
                        }

                        currentPlayers[i-1].Avgvp = ((currentPlayers[i-1].Avgvp * (currentPlayers[i-1].Games-1)) + currentPlayers[i-1].VictoryPoints) / (currentPlayers[i-1].Games);

                        
                        //re-add to redis now
                        HashEntry[] hashEntries2 =
                        {
                            new HashEntry("elo", currentPlayers[i-1].ELO),
                            new HashEntry("games", currentPlayers[i-1].Games),
                            new HashEntry("wins", currentPlayers[i-1].Wins),
                            new HashEntry("losses", currentPlayers[i-1].Losses),
                            new HashEntry("top2", currentPlayers[i-1].Top2),
                            new HashEntry("top3", currentPlayers[i-1].Top3),
                            new HashEntry("avgvp", currentPlayers[i-1].Avgvp)

                        };
                        db.HashSet("user:" + currentPlayers[i-1].Id.ToString(), hashEntries2);
                        i++;
                    }

                } //endiff /cc elo catan
                
            } //end else 
            

            
            // If the buffer starts with '!' then disconnect the current session
            if (message == "!")
                Close(1000);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket session caught an error with code {error}");
        }
    }

    class ChatServer : WsServer
    {
        public ChatServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new ChatSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            // WebSocket server port
            int port = 7990;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            // WebSocket server content path
            string www = "../../../../../www/ws";
            if (args.Length > 1)
                www = args[1];

            Console.WriteLine($"WebSocket server port: {port}");
            Console.WriteLine($"WebSocket server static content path: {www}");
            Console.WriteLine($"WebSocket server website: http://localhost:{port}/chat/index.html");

            Console.WriteLine();

            // Create a new WebSocket server
            
            var server = new ChatServer(IPAddress.Parse("127.0.0.1"), port);
            server.AddStaticContent(www, "/chat");

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.MulticastText(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }

        
    }
}
