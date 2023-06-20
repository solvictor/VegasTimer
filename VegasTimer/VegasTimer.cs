using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using ScriptPortal.Vegas;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace VegasTimer
{
    public class Chronometer : DockableControl
    {
        private bool IsRunning;
        private DateTime StartTime;
        private readonly Label Time;
        private readonly Button Start;
        private readonly Button Reset;
        private readonly Timer Timer;
        private readonly Config Config;

        public Chronometer(ref Config config) : base("Timer")
        {
            DefaultDockWindowStyle = DockWindowStyle.Floating;
            PersistDockWindowState = true;
            Text = "Timer";
            Config = config;
            BackColor = Color.FromArgb(34, 34, 34);
            Loaded += OnLoaded;
            AppWindowClosing += OnClose;
            Closing += OnClose;
            DefaultFloatingSize = new Size(200, 120);

            Timer = new Timer()
            {
                Interval = 1
            };
            Timer.Tick += OnTick;

            Start = new Button
            {
                Text = "Start",
                Location = new Point(10, 50),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Start.Click += ClickStart;
            Controls.Add(Start);

            Reset = new Button
            {
                Text = "Reset",
                Location = Point.Add(Start.Location, new Size(90, 0)),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Reset.Click += ClickReset;
            Controls.Add(Reset);

            Time = new Label
            {
                Text = Config.Elapsed.ToString(@"hh\:mm\:ss\:fff"),
                Location = new Point(10, 10),
                Font = new Font("Arial", 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            Controls.Add(Time);
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            if (!Config.IsLoaded)
                Config.Load();
            Time.Text = Config.Elapsed.ToString(@"hh\:mm\:ss\:fff");
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            if (IsRunning)
                ClickStart(this, EventArgs.Empty);
        }

        private void ClickStart(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                Timer.Stop();
                Config.Elapsed += DateTime.Now - StartTime;
                Start.Text = "Start";
            }
            else
            {
                Timer.Start();
                StartTime = DateTime.Now;
                Start.Text = "Pause";
            }
            Config.Save();
            IsRunning = !IsRunning;
        }

        private void ClickReset(object sender, EventArgs e)
        {
            Timer.Stop();
            Config.Elapsed = TimeSpan.Zero;
            IsRunning = false;
            Time.Text = "00:00:00:000";
            Config.Save();
        }

        private void OnTick(object sender, EventArgs e)
        {
            TimeSpan currentTime = DateTime.Now - StartTime + Config.Elapsed;
            Time.Text = currentTime.ToString(@"hh\:mm\:ss\:fff");
        }
    }

    public class Config
    {
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
        public bool IsLoaded { get; set; } = false;
        [JsonIgnore]
        public const string Title = "VegasTimer";
        [JsonIgnore]
        public static string Directory = Environment.CurrentDirectory + "\\VegasTimer";
        [JsonIgnore]
        public static string Path = Directory + "\\config.json";

        public void Load()
        {
            FileInfo configFile = new FileInfo(Path);
            
            if (configFile.Exists)
            {
                try
                {
                    string content = File.ReadAllText(Path);
                    Config serealized = JsonConvert.DeserializeObject<Config>(content);
                    this.Elapsed = serealized.Elapsed;
                    this.IsLoaded = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error: Failed to read config, try deleting it and restarting vegas pro.\n" + e.Message, Title);
                }
                return;
            }

            try
            {
                if (!configFile.Directory.Exists)
                    System.IO.Directory.CreateDirectory(Directory);
                File.Create(Path).Close();
                Save();
                this.IsLoaded = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Cannot create config file.\n" + e.Message, Title);
            }
        }

        public bool IsValid()
        {
            return true;
        }

        public void Save()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            };
            JsonSerializer serializer = JsonSerializer.Create(settings);
            serializer.NullValueHandling = NullValueHandling.Ignore;

            try
            {
                StreamWriter sw = new StreamWriter(Path);
                JsonWriter writer = new JsonTextWriter(sw);
                serializer.Serialize(writer, this);
                writer.Close();
                sw.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Failed to save configuration.\n" + e.Message, Title);
            }
        }
    }

    public class VegasTimer : ICustomCommandModule
    {
        public Vegas Vegas = null;
        private Config Config = new Config();

        public ICollection GetCustomCommands()
        {
            CustomCommand timer = new CustomCommand(CommandCategory.Tools, "VegasTimer")
            {
                DisplayName = "Timer",
                MenuSelectMessage = "Open a timer."
            };
            
            timer.Invoked += (s, a) =>
            {
                if (!Vegas.ActivateDockView("TimerView"))
                {
                    Chronometer chrono = new Chronometer(ref Config)
                    {
                        AutoLoadCommand = timer
                    };

                    Vegas.LoadDockView(chrono);
                }
            };

            return new CustomCommand[] { timer };
        }

        public void InitializeModule(Vegas vegas)
        {
            Vegas = vegas;

            vegas.AppInitialized += (v, args) => { 
                if (!Config.IsLoaded)
                    Config.Load();
            };

            vegas.AppDeactivate += (v, args) => Config.Save();
        }
    }
}
