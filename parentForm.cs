using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GoldFishPet
{
    public partial class parentForm : Form
    {
        object fishieLock = new object();
        List<FishForm> Fishies = new List<FishForm>();
        int maxFishies = 25;
        public static System.Threading.ManualResetEvent exitHandle = new System.Threading.ManualResetEvent(false);
        private bool HasClosed = false;

        public readonly TimeSpan MIN_TIME_BETWEEN_BREEDING = TimeSpan.FromMinutes(20);
        public readonly TimeSpan MIN_TIME_BETWEEN_BREED_ATTEMPTS = TimeSpan.FromSeconds(30);
        
        public parentForm()
        {
            InitializeComponent();
            SpawnFishies();

            this.FormClosing += ParentForm_FormClosing;
        }

        private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            HasClosed = true;
            exitHandle.Set();
            lock(fishieLock)
            {
                foreach (var fish in Fishies)
                {
                    fish.Dispose();
                }
            }
        }

        void SpawnFishies()
        {
            var mommyFish = new FishForm(true, new Point(-100, (Screen.PrimaryScreen.Bounds.Height / 2) - 50), 0.5);
            var daddyFish = new FishForm(false, new Point(Screen.AllScreens.Sum(x => x.Bounds.Width) + 100, (Screen.PrimaryScreen.Bounds.Height / 2) - 50), 0.5);

            var random = new Random();
            mommyFish.BirthDate = DateTime.Now.AddMinutes(-1 * random.Next(2, 12));
            daddyFish.BirthDate = DateTime.Now.AddMinutes(-1 * new Random(random.Next()).Next(2, 12));

            mommyFish.LocationChanged += Fishie_Moved;
            mommyFish.FishEvent += Fishie_Event;
            daddyFish.LocationChanged += Fishie_Moved;
            daddyFish.FishEvent += Fishie_Event;

            Fishies.Add(mommyFish);
            Fishies.Add(daddyFish);

            mommyFish.Show();
            daddyFish.Show();
        }

        void Fishie_Event(object sender, FishForm.FishEventArgs e)
        {
            switch (e.Action)
            {
                case FishForm.FishEventEnum.Born:
                    Log("Fishy was born");
                    break;
                case FishForm.FishEventEnum.Matured:
                    Log("Fishy matured");
                    break;
                case FishForm.FishEventEnum.Died:
                    Fishie_Died_Sadface(sender, e);
                    break;
                case FishForm.FishEventEnum.Resurrected:
                    Log("Fishy {0}", e.Reason);
                    break;
                case FishForm.FishEventEnum.Flushed:
                    Log("Fishy {0}", e.Reason);
                    break;
                default:
                    break;
            }
        }

        void Fishie_Died_Sadface(object sender, FishForm.FishEventArgs e)
        {
            Log("Fishy {0}", e.Reason);
            var t = new System.Threading.Thread(() => 
            {
                exitHandle.WaitOne(1000 * 60 * 5);
                if (e.Fish.IsDead)
                    e.Fish.Flush();
            });
            t.Start();

            if (!Fishies.Any(fishie => !fishie.IsDead))
                SpawnFishies();
        }

        bool FishiesIntersect(FishForm mommy, FishForm daddy)
        {
            if (daddy.DesktopBounds.Contains(mommy.DesktopLocation) ||
                mommy.DesktopBounds.Contains(daddy.DesktopLocation))
            {
                return true;
            }
            return false;
        }

        void Fishie_Moved(object sender, EventArgs e)
        {
            var mommy = sender as FishForm;
            lock(fishieLock)
            {
                if (mommy != null)
                {
                    if (mommy.IsMature && !mommy.mouseDown)
                    {
                        var daddy = Fishies.FirstOrDefault(dad => dad != mommy && FishiesIntersect(mommy, dad) && dad.IsMature && !dad.mouseDown);
                        if (daddy != null)
                        {
                            if (DateTime.Now.Subtract(mommy.LastBreedAttempt.GetValueOrDefault(new DateTime())) > MIN_TIME_BETWEEN_BREED_ATTEMPTS &&
                                DateTime.Now.Subtract(daddy.LastBreedAttempt.GetValueOrDefault(new DateTime())) > MIN_TIME_BETWEEN_BREED_ATTEMPTS)
                            {
                                mommy.LastBreedAttempt = DateTime.Now;
                                daddy.LastBreedAttempt = DateTime.Now;

                                // Ensure we have enough fishy quota left.
                                if (maxFishies > Fishies.Count(x => !x.IsDead))
                                {
                                    if (DateTime.Now.Subtract(mommy.LastBred.GetValueOrDefault(new DateTime())) > MIN_TIME_BETWEEN_BREEDING &&
                                        DateTime.Now.Subtract(daddy.LastBred.GetValueOrDefault(new DateTime())) > MIN_TIME_BETWEEN_BREEDING)
                                    {
                                        // Make a baby!
                                        Log("Attempting to breed");
                                        var babies = mommy.CreateBaby(daddy);
                                        foreach (var baby in babies)
                                        {
                                            if (Fishies.Count(x => !x.IsDead) + 1 == maxFishies)
                                            {
                                                var deadFish = Fishies.First(x => x.IsDead);
                                                Fishies.Remove(deadFish);
                                                deadFish.Dispose();
                                            }

                                            baby.FishEvent += Fishie_Event;
                                            baby.LocationChanged += Fishie_Moved;
                                            Fishies.Add(baby);
                                        }
                                    }
                                    else
                                    {
                                        mommy.RunAway();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void Log(string data)
        {
            Console.WriteLine(data);
            WriteToText(data);
        }

        void Log(string data, params object[] stuff)
        {
            Console.WriteLine(data, stuff);
            WriteToText(string.Format(data, stuff));
        }

        void WriteToText(string data)
        {
            if (HasClosed)
                return;

            this.textBox1.Invoke(
                new Action(
                () =>
                {
                    this.textBox1.AppendText(data + "\r\n");
                    this.textBox1.SelectionStart = this.textBox1.TextLength;
                    this.textBox1.ScrollToCaret();
                }));
        }

    }
}
