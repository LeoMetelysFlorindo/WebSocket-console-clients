using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Console_WebSocketTest
{
    // State object for reading client data asynchronously  


   

    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;

       
    }

    public class Program
    {

        public static string mensagem01 { get; set; }

        public static string mensagem_DADOS01 { get; set; }
        public static string mensagem_DADOS02 { get; set; }
        public static string datahora01 { get; set; }
        public static string mensagem02 { get; set; }
        public static string datahora02 { get; set; }


        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);



        public Program()
        {

        }

        public static void StartListening()
        {

            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            IPHostEntry ipHostInfo = Dns.GetHostEntry("10.58.149.204");

            IPAddress ipAddress = ipHostInfo.AddressList[0];

            //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8080);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("******* WEB SOCKET ON LINE as " + DateTime.Now.ToString() + "|");
                    Console.WriteLine("Aguardando por uma conexão...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPressione ENTER para continuar...");
            Console.Read();

        }


        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        //public async Task HandleMessages()
        //{
        //    try
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            while (ws.State == WebSocketState.Open)
        //            {
        //                WebSocketReceiveResult result;
        //                do
        //                {
        //                    var messageBuffer = WebSocket.CreateClientBuffer(1024, 16);
        //                    result = await ws.ReceiveAsync(messageBuffer, CancellationToken.None);
        //                    ms.Write(messageBuffer.Array, messageBuffer.Offset, result.Count);
        //                }
        //                while (!result.EndOfMessage);

        //                if (result.MessageType == WebSocketMessageType.Text)
        //                {
        //                    var msgString = Encoding.UTF8.GetString(ms.ToArray());
        //                    var message = JsonConvert.DeserializeObject<Dictionary<String, String>>(msgString);
        //                    if (message["roomCode"].ToLower() == RoomCode.ToLower())
        //                    {
        //                        Debug.Log("[WS] Got a message of type " + message["messageType"]);
        //                        // Message was intended for us!
        //                        switch (message["messageType"])
        //                        {
        //                            // handle messages here, unimportant to stackoverflow
        //                        }
        //                    }
        //                }
        //                ms.Seek(0, SeekOrigin.Begin);
        //                ms.Position = 0;
        //            }
        //        }
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        Debug.Log("[WS] Tried to receive message while already reading one.");
        //    }
        //}

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            string NomeCliente = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;            
            Socket handler = state.workSocket;

            var messageBuffer = WebSocket.CreateClientBuffer(1024, 16);
            //result = await ws.ReceiveAsync(messageBuffer, CancellationToken.None);
            //ms.Write(messageBuffer.Array, messageBuffer.Offset, result.Count);

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString() + "<EOF>";
                NomeCliente = content.Substring(0, 5);

                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("RECEBENDO  {0} bytes do cliente socket conectado. \n Data : {1}",
                        content.Length, content);
                    

                    // Gravar as informações recebidas localmente  para repassar posteriormente                    
                    // Envia para o cliente 01 o que recebeu do cliente 02
                    if (NomeCliente == "WSC01")
                    {
                        mensagem01 = "RECEBIDO DO WSC02 " + content.Length + " bytes do cliente socket conectado.";

                        mensagem_DADOS01 = content;

                        datahora01 = DateTime.Now.ToString();

                        content = mensagem01 + Environment.NewLine + Environment.NewLine + "| DADOS DO CLIENTE 02:" + mensagem_DADOS02 + Environment.NewLine + Environment.NewLine + "|" + Environment.NewLine + "<EOF>" + "|" + datahora01;

                        // Echo the data back to the client. Retorno de dados para o Cliente 01  
                        Send(handler, content);
                    }

                    // Envia para o cliente 02 o que recebeu do cliente 01
                    if (NomeCliente == "WSC02")
                    {
                        mensagem02 = "RECEBIDO DO WSC01 " + content.Length + " bytes do cliente socket conectado.";
                        datahora02 = DateTime.Now.ToString();

                        mensagem_DADOS02 = content;

                        content = mensagem02 + Environment.NewLine + Environment.NewLine +  "| DADOS DO CLIENTE 01:" + mensagem_DADOS01 + Environment.NewLine + Environment.NewLine + "|" + Environment.NewLine + "<EOF>" + "|" + datahora02;

                        // Echo the data back to the client. Retorno de dados para o Cliente 01  
                        Send(handler, content);
                    }

                    
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

       

        private static void Send(Socket handler, String data)
        {

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            var response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                        + "Upgrade: websocket" + Environment.NewLine
                        + "Connection: Upgrade" + Environment.NewLine
                        + "Sec-WebSocket-Accept: " + byteData.ToString() + Environment.NewLine + Environment.NewLine
                        //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                        //+ "Sec-WebSocket-Version: 13" + newLine
                        ;

            
            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
              new AsyncCallback(SendCallback), handler);

            //handler.BeginSend(System.Text.Encoding.UTF8.GetBytes(response), 0, byteData.Length, 0,
             //   new AsyncCallback(SendCallback), handler);
        }

        public static int Contador = 0;


        private static void SendCallback(IAsyncResult ar)
        {


            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;
                Contador++;
                Console.WriteLine("===== Um cliente conectado =====: {0}", Contador + " | " + DateTime.Now.ToString() + "|");

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("SOCKET SERVER: ENVIADOS DADOS {0} bytes para o cliente.", bytesSent + " | " + DateTime.Now.ToString() + "|");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }



        public static int Main(String[] args)
        {

            // 21/06/2021 16:25h - COMENTADO NESSA HORA
            StartListening();
            return 0;

            // fechado em 21/06/2021 16:52h
            // Define os caminhos de acesso para o webSocket
            //string ip = "10.58.149.204";
            //int port = 8080;
            //var server = new TcpListener(IPAddress.Parse(ip), port);
            //server.Start();
            //Console.WriteLine("Servidor foi iniciado em {0}:{1}, Aguardando por uma conexão...", ip, port);

            //TcpClient client = server.AcceptTcpClient();
            //Console.WriteLine("Um cliente conectado.");

            //NetworkStream stream = client.GetStream();

            //// enter to an infinite cycle to be able to handle every change in stream
            //while (true)
            //{
            //    while (!stream.DataAvailable) ;

            //    //while (client.Available < 3) ; // match against "get"

            //    byte[] bytes = new byte[client.Available];
            //    stream.Read(bytes, 0, client.Available);
            //    string s = Encoding.UTF8.GetString(bytes);

            //    //if (Regex.IsMatch(s, "^POST", RegexOptions.IgnoreCase))
            //    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            //    {
            //        Console.WriteLine("=====Handshaking from client=====\n{0}", s);

            //        // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            //        // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            //        // 3. Compute SHA-1 and Base64 hash of the new value
            //        // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            //        string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            //        string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            //        byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            //        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            //        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            //        byte[] response = Encoding.UTF8.GetBytes(
            //            "HTTP/1.1 101 Switching Protocols\r\n" +
            //            "Connection: Upgrade\r\n" +
            //            "Upgrade: websocket\r\n" +
            //            "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

            //        stream.Write(response, 0, response.Length);
            //    }
            //    else
            //    {
            //        bool fin = (bytes[0] & 0b10000000) != 0,
            //            mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            //        int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
            //            msglen = bytes[1] - 128, // & 0111 1111
            //            offset = 2;

            //        if (msglen == 126)
            //        {
            //            // was ToUInt16(bytes, offset) but the result is incorrect
            //            msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
            //            offset = 4;
            //        }
            //        else if (msglen == 127)
            //        {
            //            Console.WriteLine("TODO: msglen == 127, preciso do  qword para armazenar msglen");
            //            // i don't really know the byte order, please edit this
            //            // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
            //            // offset = 10;
            //        }

            //        if (msglen == 0)
            //            Console.WriteLine("msglen == 0");
            //        else if (mask)
            //        {
            //            byte[] decoded = new byte[msglen];
            //            byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
            //            offset += 4;

            //            for (int i = 0; i < msglen; ++i)
            //                decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

            //            string text = Encoding.UTF8.GetString(decoded);
            //            Console.WriteLine("{0}", text);
            //        }
            //        else
            //            Console.WriteLine("Bit de máscara não definido");

            //        Console.WriteLine();
            //    }

            //} // fim do while infinito

        } // fim do Main()

       
    }
}
