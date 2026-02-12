using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace FlightDynamics.Plugins
{
    /// <summary>
    /// MAVLink TCP -> Unity Transform
    /// Fixes "slow/jerky" motion by:
    /// 1) Sending requests ONLY after real connection is established
    /// 2) Sending periodic Heartbeat
    /// 3) Requesting higher message rates (tries MAV_CMD_SET_MESSAGE_INTERVAL + fallback REQUEST_DATA_STREAM)
    /// 4) Smoothing (interpolation) of position/rotation in Update()
    /// </summary>
    public class MavlinkManager : MonoBehaviour
    {
        [Header("TCP Connection Settings")]
        public string host = "127.0.0.1";
        public int port = 5762;

        [Header("MAVLink Target (Autopilot)")]
        [Tooltip("Autopilot SYSID (ArduPilot default often 1)")]
        public byte targetSystemId = 1;

        [Tooltip("Autopilot COMPID (ArduPilot default often 1)")]
        public byte targetComponentId = 1;

        [Header("This GCS (Unity) IDs")]
        public byte gcsSystemId = 255;
        public byte gcsComponentId = 190;

        [Header("Target UAV Model")]
        public Transform uavModel;
        public bool usePosition = true;

        [Header("Requested Rates (Hz)")]
        [Range(1, 200)] public int attitudeHz = 50;
        [Range(1, 50)] public int globalPosHz = 15;

        [Header("Smoothing")]
        [Tooltip("Bigger = follows faster, smaller = smoother")]
        public float positionSmooth = 12f;
        [Tooltip("Bigger = follows faster, smaller = smoother")]
        public float rotationSmooth = 14f;

        [Header("Coordinate Mapping")]
        [Tooltip("Use Transform.position (world). If false, uses localPosition.")]
        public bool useWorldPosition = true;

        [Header("Telemetry (Debug)")]
        public string connectionStatus = "Disconnected";
        public float roll, pitch, yaw;     // degrees
        public float altitude;             // meters (relative_alt)
        public double currentLat, currentLon;

        [Header("Debug Options")]
        public bool logRates = false;

        // Networking
        private TcpClient tcpClient;
        private NetworkStream netStream;
        private Thread receiveThread;
        private volatile bool isRunning;

        // Home / position
        private double homeLat, homeLon;
        private bool isHomeSet = false;

        // Thread sync
        private readonly object stateLock = new object();

        // Main-thread init flag (set by receive thread)
        private volatile bool initAfterConnect = false;
        private volatile bool heartbeatStarted = false;

        // Targets for smoothing
        private Vector3 targetPos;
        private Quaternion targetRot = Quaternion.identity;

        // Parser
        private readonly MAVLink.MavlinkParse mavlinkParser = new MAVLink.MavlinkParse();

        // Simple rate monitor
        private int gpsCount = 0;
        private int attCount = 0;
        private float rateTimer = 0f;

        // MAV_CMD_SET_MESSAGE_INTERVAL numeric id (Common.xml)
        private const ushort MAV_CMD_SET_MESSAGE_INTERVAL = 511;
        private const uint MSG_ID_ATTITUDE = 30;
        private const uint MSG_ID_GLOBAL_POSITION_INT = 33;

        private void Start()
        {
            StartConnection();
        }

        private void OnDestroy()
        {
            StopConnection();
        }

        private void StartConnection()
        {
            isRunning = true;
            receiveThread = new Thread(ConnectAndReceive);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void StopConnection()
        {
            isRunning = false;

            try { CancelInvoke(); } catch { }

            try { netStream?.Close(); } catch { }
            netStream = null;

            try { tcpClient?.Close(); } catch { }
            tcpClient = null;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                try { receiveThread.Join(500); } catch { }
            }

            connectionStatus = "Disconnected";
        }

        private void ConnectAndReceive()
        {
            while (isRunning)
            {
                try
                {
                    connectionStatus = "Connecting...";

                    tcpClient = new TcpClient();
                    tcpClient.NoDelay = true;              // lower latency
                    tcpClient.ReceiveTimeout = 1000;       // allow loop to exit/reconnect
                    tcpClient.SendTimeout = 1000;

                    tcpClient.Connect(host, port);

                    if (!tcpClient.Connected)
                    {
                        connectionStatus = "Failed to connect";
                        Thread.Sleep(250);
                        continue;
                    }

                    connectionStatus = "Connected";
                    netStream = tcpClient.GetStream();
                    netStream.ReadTimeout = 1000;
                    netStream.WriteTimeout = 1000;

                    // Let main thread do InvokeRepeating + request streams safely
                    initAfterConnect = true;

                    // Receive loop
                    while (isRunning && tcpClient.Connected)
                    {
                        MAVLink.MAVLinkMessage message = null;

                        try
                        {
                            message = mavlinkParser.ReadPacket(netStream);
                        }
                        catch (IOException)
                        {
                            // timeout / stream hiccup -> allow loop to continue
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }

                        if (message != null && message.data != null)
                            HandleMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    connectionStatus = "Error: " + ex.Message;
                }

                // Cleanup before reconnect
                try { netStream?.Close(); } catch { }
                netStream = null;

                try { tcpClient?.Close(); } catch { }
                tcpClient = null;

                Thread.Sleep(250);
            }
        }

        private void HandleMessage(MAVLink.MAVLinkMessage message)
        {
            switch (message.msgid)
            {
                case (uint)MAVLink.MAVLINK_MSG_ID.ATTITUDE:
                    {
                        var att = (MAVLink.mavlink_attitude_t)message.data;

                        float r = att.roll * Mathf.Rad2Deg;
                        float p = att.pitch * Mathf.Rad2Deg;
                        float y = att.yaw * Mathf.Rad2Deg;

                        lock (stateLock)
                        {
                            roll = r;
                            pitch = p;
                            yaw = y;

                            // target rot for smoothing
                            targetRot = Quaternion.Euler(-pitch, yaw, -roll);
                        }

                        attCount++;
                        break;
                    }

                case (uint)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT:
                    {
                        var pos = (MAVLink.mavlink_global_position_int_t)message.data;

                        double lat = pos.lat / 10000000.0;
                        double lon = pos.lon / 10000000.0;
                        float alt = pos.relative_alt / 1000.0f;

                        lock (stateLock)
                        {
                            currentLat = lat;
                            currentLon = lon;
                            altitude = alt;

                            if (!isHomeSet && Math.Abs(lat) > 1e-9 && Math.Abs(lon) > 1e-9)
                            {
                                homeLat = lat;
                                homeLon = lon;
                                isHomeSet = true;
                                Debug.Log($"[MAVLink] Home set to: {homeLat}, {homeLon}");
                            }

                            if (usePosition && isHomeSet)
                            {
                                // rough meters per degree conversion
                                float z = (float)((currentLat - homeLat) * 111319.9);
                                float x = (float)((currentLon - homeLon) * 111319.9 * Math.Cos(homeLat * Math.PI / 180.0));

                                targetPos = new Vector3(x, altitude, z);
                            }
                        }

                        gpsCount++;
                        break;
                    }

                case (uint)MAVLink.MAVLINK_MSG_ID.HEARTBEAT:
                    // ok
                    break;
            }
        }

        private void Update()
        {
            if (initAfterConnect)
            {
                // Runs on main thread (Unity-safe)
                initAfterConnect = false;

                if (!heartbeatStarted)
                {
                    heartbeatStarted = true;
                    InvokeRepeating(nameof(SendHeartbeat), 0f, 1f);
                }

                // Try precise per-message rates + fallback
                RequestRates();
            }

            if (uavModel == null) return;

            // Smooth rotation
            Quaternion desiredRot;
            lock (stateLock) { desiredRot = targetRot; }

            float rotAlpha = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            uavModel.localRotation = Quaternion.Slerp(uavModel.localRotation, desiredRot, rotAlpha);

            // Smooth position
            if (usePosition)
            {
                Vector3 desiredPos;
                bool canMove;
                lock (stateLock)
                {
                    desiredPos = targetPos;
                    canMove = isHomeSet;
                }

                if (canMove)
                {
                    float posAlpha = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);

                    if (useWorldPosition)
                        uavModel.position = Vector3.Lerp(uavModel.position, desiredPos, posAlpha);
                    else
                        uavModel.localPosition = Vector3.Lerp(uavModel.localPosition, desiredPos, posAlpha);
                }
            }

            // Optional rate logs
            if (logRates)
            {
                rateTimer += Time.deltaTime;
                if (rateTimer >= 1f)
                {
                    Debug.Log($"[MAVLink] ATTITUDE: {attCount} Hz, GLOBAL_POSITION_INT: {gpsCount} Hz");
                    attCount = 0;
                    gpsCount = 0;
                    rateTimer = 0f;
                }
            }
        }

        // ---- Sending ----

        public void SendPacket(MAVLink.MAVLINK_MSG_ID msgId, object data)
        {
            // thread-safe minimal check
            var client = tcpClient;
            var stream = netStream;

            if (client == null || !client.Connected || stream == null) return;

            try
            {
                byte[] packet = mavlinkParser.GenerateMAVLinkPacket10(msgId, data, gcsSystemId, gcsComponentId);
                if (packet != null)
                    stream.Write(packet, 0, packet.Length);
            }
            catch
            {
                // ignore send errors (reconnect loop will handle)
            }
        }

        private void SendHeartbeat()
        {
            var hb = new MAVLink.mavlink_heartbeat_t
            {
                type = (byte)MAVLink.MAV_TYPE.GCS,
                autopilot = (byte)MAVLink.MAV_AUTOPILOT.INVALID,
                system_status = (byte)MAVLink.MAV_STATE.ACTIVE,
                base_mode = 0,
                custom_mode = 0,
                mavlink_version = 3
            };

            SendPacket(MAVLink.MAVLINK_MSG_ID.HEARTBEAT, hb);
        }

        /// <summary>
        /// Tries to request higher update rates.
        /// 1) COMMAND_LONG (MAV_CMD_SET_MESSAGE_INTERVAL) for exact msg rates
        /// 2) REQUEST_DATA_STREAM fallback (some stacks still accept it)
        /// </summary>
        private void RequestRates()
        {
            // 1) Exact intervals (microseconds)
            TrySetMessageInterval(MSG_ID_ATTITUDE, attitudeHz);
            TrySetMessageInterval(MSG_ID_GLOBAL_POSITION_INT, globalPosHz);

            // 2) Fallback stream request
            RequestDataStreamFallback();
        }

        private void TrySetMessageInterval(uint messageId, int hz)
        {
            if (hz <= 0) return;

            // interval in microseconds
            float intervalUs = 1_000_000f / Mathf.Max(1, hz);

            var cmd = new MAVLink.mavlink_command_long_t
            {
                target_system = targetSystemId,
                target_component = targetComponentId,
                command = MAV_CMD_SET_MESSAGE_INTERVAL,
                confirmation = 0,

                // param1 = message_id, param2 = interval_us
                param1 = messageId,
                param2 = intervalUs,
                param3 = 0,
                param4 = 0,
                param5 = 0,
                param6 = 0,
                param7 = 0
            };

            SendPacket(MAVLink.MAVLINK_MSG_ID.COMMAND_LONG, cmd);
        }

        private void RequestDataStreamFallback()
        {
            var req = new MAVLink.mavlink_request_data_stream_t
            {
                target_system = targetSystemId,
                target_component = targetComponentId,
                req_stream_id = (byte)MAVLink.MAV_DATA_STREAM.ALL,
                req_message_rate = (ushort)Mathf.Clamp(Mathf.Max(attitudeHz, globalPosHz), 1, 200),
                start_stop = 1
            };

            SendPacket(MAVLink.MAVLINK_MSG_ID.REQUEST_DATA_STREAM, req);
        }
    }
}
