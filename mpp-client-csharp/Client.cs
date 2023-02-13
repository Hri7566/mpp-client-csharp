using Microsoft.VisualBasic.FileIO;
using WebSocketSharp;
using System.Text.Json;

namespace mpp_client_csharp
{
    public class Client : EventEmitter
    {
        public string uri;
        private string token;
        public WebSocket? ws;
        public int serverTimeOffset;
        public User? user;
        public string? participantId;
        public Channel.Channel? channel;
        public Dictionary<string, Participant> ppl = new Dictionary<string, Participant>();
        public int? connectionTime;
        public int connectionAttempts = 0;
        public string? desiredChannelId;
        public Channel.ChannelSettings? desiredChannelSettings;
        private bool canConnect = false;
        public List<dynamic> noteBuffer = new List<dynamic>();
        public int noteBufferTime = 0;
        public bool shouldStopPinging = false;
        public bool shouldStopFlushingNotes = false;

        public Client(string uri, string token)
        {
            this.uri = uri;
            this.token = token;
            serverTimeOffset = 0;
            
            bindEventListeners();

            Emit("status", "(Offline mode)");
        }

        public bool isSupported()
        {
            return true;
        }

        public bool isConnected()
        {
            return isSupported() && ws != null && ws.ReadyState == WebSocketState.Open;
        }

        public bool isConnecting()
        {
            return isSupported() && ws != null && ws.ReadyState == WebSocketState.Connecting;
        }

        public void start()
        {
            canConnect = true;
            connect();
        }

        public void stop()
        {
            canConnect = false;
            ws.Close();
        }

        public void connect()
        {
            if (!canConnect || !isSupported() || isConnected() || isConnecting()) return;
            Emit("status", "Connecting...");
            ws = new WebSocket(uri);
            ws.Connect();

            ws.OnClose += delegate (object? sender, CloseEventArgs e)
            {
                user = null;
                participantId = null;
                channel = null;
                setParticipants(new List<Participant>());
                shouldStopPinging = true;
                shouldStopFlushingNotes = true;

                Emit("disconnect");
                Emit("status", "Offline mode");

                if (connectionTime != null)
                {
                    connectionTime = null;
                    connectionAttempts = 0;
                } else
                {
                    ++connectionAttempts;
                }
                List<int> ms_lut = new List<int> { 50, 2950, 7000, 10000 };
                int idx = connectionAttempts;
                if (idx >= ms_lut.Count()) idx = ms_lut.Count() - 1;
                int ms = ms_lut[idx];

                Task.Run(async () =>
                {
                    await Task.Delay(ms);
                    connect();
                });
            };

            ws.OnError += delegate (object? sender, WebSocketSharp.ErrorEventArgs e)
            {
                ws.Close();
            };

            ws.OnOpen += delegate (object? sender, EventArgs e)
            {
                connectionTime = DateTime.Now.Millisecond;
                sendArray(new List<dynamic> { new { m = "hi", token = token } });
                Task.Run(async () =>
                {
                    for (; ; )
                    {
                        if (shouldStopPinging) break;
                        await Task.Delay(20000);
                        sendArray(new List<dynamic> { new { m = "t", e = DateTime.Now.Millisecond } });
                    }
                });
                noteBuffer = new List<dynamic>();
                noteBufferTime = 0;
                Task.Run(async () =>
                {
                    for (; ; )
                    {
                        if (shouldStopFlushingNotes) break;
                        await Task.Delay(200);
                        if (noteBufferTime != 0 && noteBuffer.Count() > 0)
                        {
                            sendArray(new List<dynamic> { new { m = "n", t = noteBufferTime + serverTimeOffset, n = noteBuffer } });
                            noteBufferTime = 0;
                            noteBuffer = new List<dynamic>();
                        }
                    }
                });

                Emit("connect");
                Emit("status", "Joining channel...");
            };

            ws.OnMessage += delegate (object? sender, MessageEventArgs e)
            {
                Console.WriteLine(e.Data);
                dynamic transmission = JsonSerializer.Deserialize<List<dynamic>>(e.Data);
                if (transmission == null) return;
                for (int i = 0; i < transmission.Count; i++)
                {
                    dynamic msg = transmission[i];
                    Console.WriteLine(JsonSerializer.Serialize(msg));
                    Emit(msg.m, msg);
                }
            };
        }

        private void bindEventListeners()
        {
            On("hi", delegate (dynamic msg)
            {
                user = msg.u;
                receiveServerTime(msg.t, msg.e);
                if (desiredChannelId != null) setChannel(desiredChannelId, desiredChannelSettings);
            });

            On("t", delegate (dynamic msg)
            {
                receiveServerTime(msg.t, msg.e);
            });

            On("ch", delegate (dynamic msg)
            {
                desiredChannelId = msg.ch._id;
                channel = msg.ch;
                if (msg.p != null) participantId = msg.p;
                setParticipants(msg.ppl);
            });

            On("p", delegate (dynamic msg)
            {
                participantUpdate(msg);
                Emit("participant update", ppl[msg.id]);
            });

            On("bye", delegate (dynamic msg)
            {
                removeParticipant(msg.p);
            });
        }

        public void sendArray(List<dynamic> arr)
        {
            string json = JsonSerializer.Serialize(arr);
            send(json);
        }

        public void send(string data)
        {
            if (!isConnected()) return;
            ws.Send(data);
        }

        public void setChannel(string id, Channel.ChannelSettings? set)
        {
            desiredChannelId = id;
            if (set != null)
            {
                desiredChannelSettings = set;
            } else
            {
                set = offlineChannelSettings;
            }

            sendArray(new List<dynamic> { new { m = "ch", _id = desiredChannelId, set = desiredChannelSettings } });
        }

        public Channel.ChannelSettings offlineChannelSettings = new Channel.ChannelSettings
        {
            lobby = true,
            visible = false,
            chat = false,
            crownsolo = false,
            color = "#ecfaed"
        };

        public Channel.ChannelSettings getChannelSettings()
        {
            if (!isConnected() || channel == null || channel.settings == null)
            {
                return offlineChannelSettings;
            }
            return channel.settings;
        }

        public Participant offlineParticipant = new Participant("", "", "#777", "", 0.0f, 0.0f);
        
        public void setParticipants(List<Participant> ppl)
        {
            foreach (string id in this.ppl.Keys)
            {
                if (this.ppl[id] == null) continue;
                bool found = false;
                for (int j = 0; j < ppl.Count(); j++)
                {
                    if (ppl[j].id == id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    removeParticipant(id);
                }
            }
        }

        public int countParticipants()
        {
            return ppl.Count();
        }

        public void participantUpdate(Participant update)
        {
            Participant part = ppl[update.id];
            if (part == null)
            {
                part = update;
                ppl[part.id] = part;
                Emit("participant added", part);
            } else
            {
                if (update.x != null) part.x = update.x;
                if (update.y != null) part.y = update.y;
                if (update.color != null) part.color = update.color;
                if (update.name != null) part.name = update.name;
            }
        }

        public void removeParticipant(string id)
        {
            if (ppl.ContainsKey(id))
            {
                Participant part = ppl[id];
                ppl.Remove(id);
                Emit("participant removed", part);
                Emit("count", countParticipants());
            }
        }

        public bool isOwner()
        {
            return channel != null && channel.crown != null && channel.crown.participantId == participantId;
        }

        public bool preventsPlaying()
        {
            return isConnected() && !isOwner() && getChannelSettings().crownsolo == true;
        }

        public void receiveServerTime(int time, float? echo)
        {
            TimeSpan target = new DateTime(time) - DateTime.Now;
            int duration = 1000;
            int step = 0;
            int steps = 50;
            int step_ms = duration / steps;
            int difference = target.Milliseconds - serverTimeOffset;
            int inc = difference / steps;

            Task task = Task.Run(async () =>
            {
                for (; ; )
                {
                    await Task.Delay(step_ms);
                    serverTimeOffset += inc;

                    if (++step >= steps)
                    {
                        serverTimeOffset = target.Milliseconds;
                        break;
                    }
                }
            });

            task.Start();
        }
    }
}
