using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 256;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}
namespace TestWebSocket_3
{
    
    public class Program
    {
        

        // The port number for the remote device.  
        private const int port = 8080;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;
       
        private static System.Timers.Timer timer;

        //private readonly sTimer timer = new TimeSpan();


        private static void StartClient()
        {
            
            // Connect to  a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".  
                
                IPHostEntry ipHostInfo = Dns.GetHostEntry("10.58.149.204");
                //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                
                //21/06/2021 16:31h - COMENTADO!
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                //IPAddress ipAddress = "10.58.149.204";
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();


                // 23/06/2021 13:09h
                // ENVIAR UMA RESPOSTA COM A PESAGEM REALIZADA PELA BALANÇA

                //string docPath = $"C:\\ZPLPrint\\ZPLcode01.txt";
                //string caminho = "";
                //string sValorPath = "";

                //// Ler o Map File fornecido
                //caminho = docPath;
                //sValorPath = caminho.Substring(0, 12);
                //var path = @sValorPath.Trim();
                //string sArquivo = caminho.Substring(12, 13);
                //var filePath = @sArquivo;

                ////Abrir arquivo de insert
                //string text = System.IO.File.ReadAllText(path + filePath);

                //// Ler arquivo e fazer as inserções
                //string[] lines = System.IO.File.ReadAllLines(path + filePath);
                string lines = "PESAGEM: 23,56 Kg; Data: " + DateTime.Today.ToString() + "PESO MINIMO: 20 KG; PESO MÁXIMO: 25 Kg;";

                // Send test data to the remote device.  
                //Send(client, "WSC01 CLIENTE: ENVIANDO DADOS PARA O SERVIDOR WEB SOCKET...<EOF>. |" + lines[0].ToString() + Environment.NewLine +  DateTime.Today.ToString() + "|");
                Send(client, "WSC02 CLIENTE: ENVIANDO PESAGEM PARA O SERVIDOR WEB SOCKET...<EOF>. |" + lines +  Environment.NewLine + DateTime.Today.ToString() + "|");
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.  

                Console.WriteLine("Resposta recebida : {0}", response + " | " + DateTime.Today.ToString() + "|");
                Console.WriteLine("=========== ** FIM  ** ================ \n ");

                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket conectado com {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
           
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Enviado {0} bytes para o servidor...", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
       }
        
        

        public static void Main(String[] args)
        {

            //Random y = new Random();

            //// Gerar um número aleatório entre 0 e 99
            //int num01 = y.Next(99);


            //// Executado 3 vezes
            //for (int z=0; z <= 100; z++)
            //{

            //    // Só executa se o número aleatório gerado for o mesmo 
            //    if (z.ToString() == num01.ToString())
            //    {

            //        StartClient();
            //        // Gerar um número aleatório entre 0 e 99
            //        num01 = y.Next(99);
            //    }

            //}

            //Console.WriteLine("\nPressione ENTER para continuar...");
            //var x = Console.Read();

            //return 0;

            timer = new System.Timers.Timer();

            // Setting up Timer
            timer.Interval = 15000;  // 30 segundos 
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.WriteLine("|" + "WEBSOCKET CLIENTE 02 Iniciado... " + DateTime.Now.ToString() + "|");
            Console.WriteLine("**** ENVIO DE ARQUIVOS DE PESAGEM PARA O WEBSOCKET *** ");
            Console.WriteLine(" ");            
            Console.WriteLine("*** PRESSIONE ENTER PARA FECHAR O CLIENT CONSOLE 02  *** ");
            Console.ReadLine();

            // Releasing Timer resources once done
            timer.Stop();
            timer.Dispose();
            
            
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("|" + "CLIENTE 02: INICIADO ENVIO DE DADOS... " + DateTime.Today.ToString() + "|");
            Console.WriteLine(" ");
            StartClient();
            Console.WriteLine("ENVIO FINALIZADO... " + DateTime.Now.ToString() + "|");
            Console.WriteLine("***********************************************" + DateTime.Now.ToString() + "|");
            
        }
    }
}
