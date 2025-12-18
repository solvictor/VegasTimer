using System;
using System.Media;
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
        private bool IsPausedByFocus;
        private DateTime StartTime;
        private readonly Label Time;
        private readonly Button Start;
        private readonly Button Reset;
        private readonly CheckBox Sounds;
        private readonly Timer Timer;
        private readonly Config Config;
        private readonly Vegas MyVegas;

        public Chronometer(Vegas vegas, ref Config config) : base("Timer")
        {
            MyVegas = vegas;
            DefaultDockWindowStyle = DockWindowStyle.Floating;
            PersistDockWindowState = true;
            Text = "Timer";
            Config = config;
            BackColor = Color.FromArgb(34, 34, 34);
            Loaded += OnLoaded;
            AppWindowClosing += OnClose;
            Closing += OnClose;
            DefaultFloatingSize = new Size(200, 175);

            MyVegas.AppActivated += OnAppActivate;
            MyVegas.AppDeactivate += OnAppDeactivate;

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

            Sounds = new CheckBox
            {
                Text = "Sounds",
                Location = Point.Add(Start.Location, new Size(0, 30)),
                ForeColor = Color.FromArgb(220, 220, 220),
                Checked = Config.Sounds
            };
            Sounds.CheckedChanged += ToggleSounds;
            Controls.Add(Sounds);

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

        private void OnAppDeactivate(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                Timer.Stop();
                Config.Elapsed += DateTime.Now - StartTime;
                IsPausedByFocus = true;
            }
        }

        private void OnAppActivate(object sender, EventArgs e)
        {
            if (IsRunning && IsPausedByFocus)
            {
                StartTime = DateTime.Now;
                Timer.Start();
                IsPausedByFocus = false;
            }
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            if (!Config.IsLoaded)
                Config.Load();
            Time.Text = Config.Elapsed.ToString(@"hh\:mm\:ss\:fff");
        }

        public void ToggleTimer(bool sound)
        {
            if (IsRunning)
            {
                Timer.Stop();
                if (!IsPausedByFocus) 
                {
                   Config.Elapsed += DateTime.Now - StartTime;
                }
                
                Start.Text = "Start";
                if (sound)
                    PlaySound("stop");
                
                IsPausedByFocus = false;
            }
            else
            {
                Timer.Start();
                StartTime = DateTime.Now;
                Start.Text = "Pause";
                if (sound)
                    PlaySound("start");
            }
            Config.Save();
            IsRunning = !IsRunning;
        }

        private void PlaySound(string type)
        {
            try
            {
                SoundPlayer player = new SoundPlayer(Config.Directory + $"\\timer_{type}_sound.wav");
                player.Play();
            }
            catch (Exception)
            {
                SystemSounds.Beep.Play();
            }
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            MyVegas.AppActivated -= OnAppActivate;
            MyVegas.AppDeactivate -= OnAppDeactivate;

            if (IsRunning)
                ToggleTimer(false);
        }

        private void ClickStart(object sender, EventArgs e)
        {
            ToggleTimer(Config.Sounds);
        }

        private void ClickReset(object sender, EventArgs e)
        {
            Timer.Stop();
            Config.Elapsed = TimeSpan.Zero;
            IsRunning = false;
            IsPausedByFocus = false;
            Time.Text = "00:00:00:000";
            Start.Text = "Start";
            Config.Save();
            if (Config.Sounds)
                PlaySound("reset");
        }
        
        private void ToggleSounds(object sender, EventArgs e)
        {
            Config.Sounds = Sounds.Checked;
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
        public bool Sounds { get; set; } = true;
        [JsonIgnore]
        public bool IsLoaded { get; set; } = false;
        [JsonIgnore]
        public const string Title = "VegasTimer";
        [JsonIgnore]
        public string Directory;
        [JsonIgnore]
        public string Path;

        public Config(Vegas vegas)
        {
            if (vegas == null)
                return;

            Directory = vegas.GetApplicationDataPath(Environment.SpecialFolder.ApplicationData) + Title;
            Path = Directory + "\\config.json";
        }

        public void Load()
        {
            FileInfo configFile = new FileInfo(Path);
            
            if (configFile.Exists)
            {
                try
                {
                    string content = File.ReadAllText(Path);
                    Config serealized = JsonConvert.DeserializeObject<Config>(content);

                    if (serealized == null)
                        throw new Exception("Deserialization returned null:\n" + content);

                    Elapsed = serealized.Elapsed;
                    Sounds = serealized.Sounds;
                    IsLoaded = true;
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
                IsLoaded = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Cannot create config file.\n" + e.Message, Title);
            }
        }

        public bool IsValid() { return true; }

        public void Save()
        {
            if (!IsLoaded) return;

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            JsonSerializer serializer = JsonSerializer.Create(settings);

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
        private Config Config = null;
        private Chronometer Chronometer = null;

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
                    Chronometer = new Chronometer(Vegas, ref Config)
                    {
                        AutoLoadCommand = timer
                    };

                    Vegas.LoadDockView(Chronometer);
                }
            };

            CustomCommand toggle = new CustomCommand(CommandCategory.Tools, "ToggleTimer")
            {
                DisplayName = "Toggle Timer",
                MenuSelectMessage = "Start/Stop the current timer.",
                CanAddToKeybindings = true
            };

            toggle.Invoked += (s, a) =>
            {
                if (Chronometer != null) 
                {
                    Chronometer.ToggleTimer(Config.Sounds);
                }
            };

            return new CustomCommand[] { timer, toggle };
        }

        public void InitializeModule(Vegas vegas)
        {
            Vegas = vegas;

            vegas.AppInitialized += (v, args) => {
                Config = new Config(vegas);
                Config.Load();
            };
            vegas.AppDeactivate += (v, args) => Config.Save();
        }
    }
}