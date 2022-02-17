using System;
using System.Collections;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Servers
{
    /// <summary>
    /// Used to connect to and interact with a Sync v2 Game Server.
    /// This is a scriptable object so that it can live across
    /// scene changes and avoids having to implement this as a
    /// singleton.
    /// </summary>
    [CreateAssetMenu(fileName = "SyncClient", menuName = "SyncClient")]
    public sealed class SyncClient : ScriptableObject
    {
        private const int MaxMessageBytes = 65535;
        private const long InvalidUserId = 0;
        private int ReadTimeoutMs = 5000;

        public int Score = -1;

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public bool IsConnected
        {
            get
            {
                return
                    client != null &&
                    client.Client != null &&
                    client.Client.Connected;
            }
        }

        [Tooltip("Expected TLS certificate of the server being connected to.")]
        [SerializeField]
        private TLSConfiguration tlsConfiguration;

        private TcpClient client;
        private SslStream sslStream;

        private SafeQueue<byte[]> receivePacketsQueue = new SafeQueue<byte[]>();

        private volatile bool IsConnecting;
        private volatile bool IsReconnecting;
        private int attemptCount = 0;
        private bool initialConnectionAttempted = false;

        private long userId;
        private string matchId;

        byte[] currentReadData = new byte[MaxMessageBytes];

        private Timer readTimer;
        private int timeoutflag = 0;
        private ServerConnectionInfo connectArgs;

        private SafeQueue<byte[]> outboundPacketsQueue = new SafeQueue<byte[]>();
        private Timer writeTimer;
        private int writeTimeoutflag = 0;
        private volatile bool isWriting = false;

        public bool DidTimeOut = false;
        public bool SocketForceClosed = false;


        /// <summary>
        /// Connects to the game server with the given <see cref="ServerConnectionInfo"/>.
        /// This should be called from the main Unity thread.
        /// </summary>
        /// <param name="connectArgs"></param>
        public void Connect(ServerConnectionInfo connectArgs)
        {
            if (IsConnecting || IsConnected)
            {
                Debug.Log("Connect in SyncClient");
                Debug.Log($"IsConnecting: {IsConnecting}");
                Debug.Log($"IsConnected: {IsConnected}");

                return;
            }
            this.connectArgs = connectArgs;

            InitializeFields();

            ReadTimeoutMs = 5000;

            DidTimeOut = false;
            SocketForceClosed = false;
            IsConnecting = true;
            IsReconnecting = false;

            Debug.Log("Starting connection attempts...");
            
            AttemptConnect(true);
        }

        public void DisposeConnectArgs()
        {
            connectArgs.url = null;
            connectArgs.port = 0;
        }

        public void Reset()
        {
            Debug.Log("Resetting SyncClient...");
            DisposeConnectArgs();
            ClearQueues();
            DidTimeOut = false;
            SocketForceClosed = false;
            IsConnecting = false;
            IsReconnecting = false;
            Disconnect();
            Debug.Log("Completed reset of SyncClient...");
        }

        public void ClearQueues()
        {
            receivePacketsQueue?.Clear();
            outboundPacketsQueue?.Clear();
        }

        public void AttemptConnect(bool force = false)
        {   
            if (force)
            {
                IsConnecting = false;
                IsReconnecting = false;
                attemptCount = 0;
            }

            if (IsConnecting || IsConnected)
            {
                Debug.Log("Already connected / is connecting in AttemptConnect...");
                return;
            }
            
            bool isGameOver = UserData.Instance?.IsGameOver == true;
            if (isGameOver && !force)
            {
                Debug.Log("Attempting connect while game over...");
                Debug.Log($"UserData.Instance.IsGameOver {UserData.Instance.IsGameOver}");
                return;
            }
            IsConnecting = true;

            ClearQueues();
            sslStream?.Close();
            client?.Close();

            client = new TcpClient();
            client.Client.ReceiveBufferSize = 65535;
            client.Client.SendBufferSize = 65535;

            Debug.Log("Connecting and async packet reading...");
            ResetReadTimer();
            ReceivePacketsThread();
        }

        /// <summary>
        /// Disconnects from the game server.
        /// </summary>
        public void Disconnect(bool deleteConnectionInfo = false)
        {
            if (deleteConnectionInfo)
            {
                connectArgs.url = null;
                connectArgs.port = 0;
            }
            Debug.Log("Disconnecting...");

            sslStream?.Close();
            client?.Close();

            readTimer?.Dispose();
            writeTimer?.Dispose();

            DidTimeOut = false;
            SocketForceClosed = false;
            UserData.Instance.IsGameOver = true;
            IsConnecting = false;

            ClearQueues();
        }

        public void SendPlayerInput(int score)
        {
            if (!IsConnecting && IsConnected)
            {
                try
                {
                    SendCore(PacketFactory.MakePlayerInputBuffer(score));

                }
                catch (Exception e)
                {
                    Debug.LogWarning("SendPlayerInput exception, trying again: " + e.ToString());
                    SendPlayerInput(score);
                }
            }
        }

        public void SendChatMessage(int chatId)
        {
            if (!IsConnecting && IsConnected)
            {
                try
                {
                    SendCore(PacketFactory.MakeChatBuffer(chatId));

                }
                catch (Exception e)
                {
                    Debug.LogWarning("SendChatMessage exception, trying again: " + e.ToString());
                    SendChatMessage(chatId);
                }
            }
        }

        /// <summary>
        /// Sends a keep-alive message to the game server. This is non-blocking.
        /// </summary>
        public void SendKeepAlive()
        {
            if (!IsConnecting && IsConnected)
            {
                try
                {
                    SendCore(PacketFactory.MakeKeepAliveBuffer());

                }
                catch (Exception e)
                {
                    Debug.LogWarning("SendKeepAlive exception, trying again: " + e.ToString());
                    SendKeepAlive();
                }
            }
        }

        /// <summary>
        /// Tells the game server that the player is pausing the game. This is non-blocking.
        /// </summary>
        public void SendAppPaused()
        {
            try
            {
                SendCore(PacketFactory.MakeAppPauseedBuffer());
            }
            catch (Exception e)
            {
                Debug.LogWarning("SendPauseMessage exception, trying again: " + e.ToString());
                SendAppPaused();
            }
        }

        /// <summary>
        /// Tells the game server that the player is resuming the game. This is non-blocking.
        /// </summary>
        public void SendAppResumed()
        {
            try
            {
                SendCore(PacketFactory.MakeAppResumedBuffer());
            }
            catch (Exception e)
            {
                Debug.LogWarning("SendPauseMessage exception, trying again: " + e.ToString());
                SendAppResumed();
            }
        }

        /// <summary>
        /// Tells the game server that the player is ending the match. This is non-blocking.
        /// </summary>
        public void SendForfeitMatch()
        {
            try
            {
                SendCore(PacketFactory.MakeForfeitBuffer());
            }
            catch (Exception e)
            {
                Debug.LogWarning("QuitMatch exception, trying again: " + e.ToString());
                SendForfeitMatch();
            }
        }

        /// <summary>
        /// Gets the next Flat Buffer packet received from the game server.
        /// </summary>
        /// <param name="data">The received Flat Buffer formatted packet.</param>
        /// <returns><c>true</c> if there was a packet present</returns>
        public bool GetNextPacket(out byte[] data)
        {
            return receivePacketsQueue.TryDequeue(out data);
        }

        private void InitializeFields()
        {
            userId = connectArgs.userId;
            Debug.Log($"Your userId={userId}");

            matchId = connectArgs.matchId;
            Debug.Log($"Connect with matchId={matchId}");
        }

        public void Reconnect()
        {
            Debug.Log("Reconnect");
            if (IsReconnecting)
            {
                Debug.Log("Skipping reconnect attempt, reconnect in progress...");
                return;
            }
            if (UserData.Instance != null && UserData.Instance.IsGameOver)
            {
                Debug.Log("Skipping reconnect attempt game is over/aborted...");
                return;
            }
            Debug.Log("Doing the Reconnect");
            IsReconnecting = true;

            Debug.Log("Reconnect attempts: " + attemptCount);
            Disconnect();
            SyncGameController.Instance.AttemptReconnectOnNextUpdate();
            UserData.Instance.IsGameOver = false;

            if (attemptCount < 3) {
                Debug.Log("Actually attempting a reconnect");
                SetReadTimeout(5000);
                StopReadTimer();
                Debug.Log($"Connect attempt #{attemptCount} failed, sleeping for 5s...");
                attemptCount++;
                Thread.Sleep(5000);
                Debug.Log($"Retrying, connect attempt #{attemptCount} to ip={connectArgs.url} port={connectArgs.port}");
                AttemptConnect();
            } else {
                IsReconnecting = false;
                Debug.LogError("All connect attempts failed!");
                SyncGameController.Instance.AbortOnNextUpdate();
            }
        }

        private void ReceivePacketsThread()
        {
            // Thread exceptions are silent, so
            // catching is absolutely required.
            try
            {
                Debug.Log("Opening TCP socket...");
                TryOpenTcpSocket();
                DidTimeOut = false;
                SocketForceClosed = false;

                Debug.Log("Opening SSL Stream...");
                TryOpenSslStream();

                Debug.Log("Validating server...");
                TryValidateServer();

                Debug.Log("Attempting handshake...");
                TryHandshake();

                Debug.Log("Raising connected event, client is connected to server...");
                SyncGameController.Instance.DidConnectSuccessfully();
                attemptCount = 0;

                Debug.Log("Starting async packet reads...");
                ReadPacketAsync();
            }
            catch (SocketException exception)
            {
                Debug.LogWarning($"Failed to connect to ip={connectArgs.url} port={connectArgs.port}\nReason: {exception}");
                IsConnecting = false;
                IsReconnecting = false;
                Reconnect();
            }
            catch (TimeoutException)
            {
                Debug.LogWarning($"Timed out while connected to ip={connectArgs.url} port={connectArgs.port}, attempting reconnect...");
                IsConnecting = false;
                IsReconnecting = false;
                Reconnect();
            }
            catch (Exception exception)
            {
                // something went wrong. probably important.
                Debug.LogError($"Receive packets thread exception:\n{exception}");
                SyncGameController.Instance.AbortOnNextUpdate();
            }
        }
        
        private void TryOpenTcpSocket()
        {
            try
            {
                client.Connect(connectArgs.url, (int)connectArgs.port);
                client.NoDelay = true;
            }
            catch (Exception)
            {
                if (!initialConnectionAttempted)
                {
                    initialConnectionAttempted = true;
                }
                throw;
            }
        }

        private void TryOpenSslStream()
        {
            try
            {
                sslStream = new SslStream(
                    client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null
                );

                sslStream.ReadTimeout = ReadTimeoutMs;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TryValidateServer()
        {
            try
            {
                sslStream.AuthenticateAsClient(tlsConfiguration.TargetHost);
                IsConnecting = false;
                IsReconnecting = false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void TryHandshake()
        {
            try
            {
                SendCore(PacketFactory.MakeConnectBuffer(userId, connectArgs.matchId, connectArgs.matchToken));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SendCore(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Not sending data - no connection to the game server.");
                return;
            }

            outboundPacketsQueue.Enqueue(data);
            CheckSendMessages();
        }

        private void CheckSendMessages()
        {
            if (isWriting || outboundPacketsQueue.Count <= 0)
            {
                return;
            }
            SendPacketAsync();
        }

        private void OnSendMessage(IAsyncResult result)
        {
            ResetWriteTimer();
            sslStream?.EndWrite(result);
            isWriting = false;
            CheckSendMessages();
        }

        private void OnWriteTimer(object obj)
        {
            int prevVal = Interlocked.CompareExchange(ref writeTimeoutflag, 2, 0);
            if (prevVal != 0)// && UserData.Instance.IsGameStarted)
            {
                Debug.Log($"OnWriteTimer, flag was already set to {prevVal}, returning...");
                return;
            }
            // if we get here, we've set the flag to 2, indicating a timeout was hit.
            writeTimeoutflag = 0;
            Debug.LogError("Write timeout occurred!!!");

            isWriting = false;
            CheckSendMessages();
        }

        public void ResetWriteTimer()
        {
            StopWriteTimer();
            writeTimer = new Timer(OnWriteTimer, null, ReadTimeoutMs, Timeout.Infinite);
        }

        public void StopWriteTimer()
        {
            writeTimeoutflag = 0;
            if (writeTimer != null)
            {
                writeTimer.Dispose();
            }
        }

        public void StopReadTimer()
        {
            timeoutflag = 0;
            if(readTimer != null)
            {
                readTimer.Dispose();
            }
        }

        public void ResetReadTimer()
        {
            StopReadTimer();
            readTimer = new Timer(OnReadTimer, null, ReadTimeoutMs, Timeout.Infinite);
        }

        private void OnReadMessage(IAsyncResult result)
        {
            try
            {
                int? byteOptional = sslStream?.EndRead(result);
                int bytesRead = byteOptional.HasValue ? byteOptional.Value : 0;
            
                var content = new byte[bytesRead];
                if (bytesRead == 0)
                {
                    Debug.Log("read bytes of length 0");
                    SocketForceClosed = true;
                    content = new byte[0];
                    Debug.LogError("Socket closed before Game Over");
                    SyncGameController.Instance.AbortOnNextUpdate();
                    return;
                }
                SocketForceClosed = false;

                content = new byte[bytesRead];
                Array.Copy(currentReadData, content, bytesRead);

                receivePacketsQueue.Enqueue(content);

                currentReadData = new byte[MaxMessageBytes];
                timeoutflag = 0;
           
                var state = new System.Object();
                sslStream?.BeginRead(currentReadData, 0, MaxMessageBytes, OnReadMessage, state);
                ResetReadTimer();
            }
            catch (Exception exception)
            {
                // Log as regular message because servers do shut down sometimes
                Debug.Log($"OnReadMessage Exception: {exception}");
            }
        }

        public void SetReadTimeout(int ms)
        {
            ReadTimeoutMs = ms;
        }

        private void OnReadTimer(object obj)
        {
            if (UserData.Instance.IsGameOver || UserData.Instance.IsUnityAppPaused)
            {
                return;
            }

            int prevVal = Interlocked.CompareExchange(ref timeoutflag, 2, 0);
            if(prevVal != 0)// && UserData.Instance.IsGameStarted)
            {
                Debug.Log($"OnReadTimer, flag was already set to {prevVal}, returning...");
                return;
            }
            // if we get here, we've set the flag to 2, indicating a timeout was hit.

            DidTimeOut = true;
            IsConnecting = false;
            timeoutflag = 0;
            Debug.LogError("Read timeout occurred!!!");            
            Reconnect();
        }

        private void ReadPacketAsync()
        {
            Debug.Log("ReadPacketAsync");
            try
            {
                var state = new System.Object();
                sslStream?.BeginRead(currentReadData, 0, MaxMessageBytes, OnReadMessage, state);
                ResetReadTimer();
            }
            catch (SocketException exception)
            {
                // Log as regular message because servers do shut down sometimes
                Debug.Log($"ReadPacketsBlocking: stream.Read SocketException error code; {exception.ErrorCode}, exception: {exception}");
                throw exception;
            }
            catch (AggregateException aggregateException)
            {
                if (IsServerTimeout(aggregateException, out var socketException))
                {
                    throw new TimeoutException("Timed out waiting to receive data from the server", socketException);
                }
                Debug.LogError($"Some other aggregate exception...");

                throw;
            }
            catch (Exception)
            {
                Debug.LogError($"Some other exception...");
                throw;
            }
        }

        private void SendPacketAsync()
        {
            try
            {
                byte[] packet = { };
                
                if (!outboundPacketsQueue.TryDequeue(out packet))
                {
                    return;
                }
                ResetWriteTimer();
                isWriting = true;
                var state = new System.Object();
                sslStream?.BeginWrite(packet, 0, packet.Length, OnSendMessage, state);
            }
            catch (SocketException exception)
            {
                // Log as regular message because servers do shut down sometimes
                Debug.LogError($"SendPacketAsync: stream.Write SocketException error code; {exception.ErrorCode}, exception: {exception}");
                throw exception;
            }
            catch (Exception exception)
            {
                // Log as regular message because servers do shut down sometimes
                Debug.LogError($"SendPacketAsync: stream.Write exception: {exception}");
                throw exception;
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            Debug.Log("certificate.GetPublicKeyString()");
            Debug.Log(certificate.GetPublicKeyString());
            
            Debug.Log("certificate.GetRawCertDataString()");
            Debug.Log(certificate.GetRawCertDataString());
            
            Debug.Log("certificate.GetCertHashString()");
            Debug.Log(certificate.GetCertHashString());
            
            Debug.Log("certificate.ToString(true)");
            Debug.Log(certificate.ToString(true));

            byte[] keyBytes = certificate.GetPublicKey();
            string keyBytesStr = "";
            foreach (byte keyByte in keyBytes) {
                keyBytesStr += keyByte.ToString();
            }
            Debug.Log("key bytes");
            Debug.Log(keyBytesStr);

            if (string.CompareOrdinal(certificate.GetPublicKeyString(), tlsConfiguration.PublicKey) == 0)
            {
                return true;
            }

            Debug.LogError("The server does not have the expected public key.");

            return false;
        }

        private bool IsServerTimeout(AggregateException aggregateException, out SocketException socketException)
        {
            var ioException = aggregateException.InnerExceptions != null && aggregateException.InnerExceptions.Count == 1
                ? aggregateException.InnerExceptions[0] as IOException
                : null;

            socketException = ioException != null && ioException.InnerException != null
                ? ioException.InnerException as SocketException
                : null;

            return socketException != null && socketException.SocketErrorCode == SocketError.WouldBlock;
        }
    }
}

public struct ServerConnectionInfo {
    public string url { get; set; }
    public uint port { get; set; }
    public string matchId { get; set; }
    public string matchToken { get; set; }
    public long userId { get; set; }

    public ServerConnectionInfo(string url, uint port, string matchId, string matchToken, long userId) {
        this.url = url;
        this.port = port;
        this.matchId = matchId;
        this.matchToken = matchToken;
        this.userId = userId;
    }
}