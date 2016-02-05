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
    public partial class FishForm : Form
    {
        public static RandomNameGeneratorLibrary.PersonNameGenerator NameGenerator = new RandomNameGeneratorLibrary.PersonNameGenerator();
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get { return string.Format("{0} {1}", FirstName, LastName); } }
        public bool IsMature { get; set; }
        public bool IsDead { get; set; }
        public DateTime? LastBred = null;
        public DateTime? LastBreedAttempt = DateTime.Now;
        public double SizeScale { get; set; }
        public float left = 0f, top = 0f;
        public bool toRight = true;
        public bool speedMode = false;
        public double MaxSizeScale = 1f + ((double)(new Random().Next(-10, 10)) / 80);
        public Point Destination { get; set; }
        public FishGender Gender { get; set; }

        Point oldPoint = new Point(0, 0);
        public bool mouseDown = false;
        bool haveHandle = false;
        Timer timerSpeed = new Timer();
        int MaxCount = 50;
        float stepX = 2f;
        float stepY = 0f;
        int count = 0;
        bool stopIt = false;
        float speedMultiplier = (float)new Random().NextDouble() * 10f;

        int frameCount = 20;
        int frame = 0; 
        int frameWidth = 100;
        int frameHeight = 100;
        public int timerInterval = 25;
        List<double> heightValues = new List<double>();


        public DateTime BirthDate = DateTime.Now;

        public delegate void FishEventHandler(object sender, FishEventArgs e);

        public event FishEventHandler FishEvent;

        public FishForm()
        {
            InitializeComponent();
            SizeScale = 1;
            InitFish();
        }

        public FishForm(bool movingRight, Point location, double scale = 1, FishGender gender = FishGender.None)
        {
            InitializeComponent();
            toRight = movingRight;
            left = location.X;
            top = location.Y;
            SizeScale = scale;
            Gender = gender;
            InitFish();
        }

        private void InitFish()
        {
            this.TopMost = true;
            toRight = true;
            frame = 20;
            frame = 0;
            frameWidth = FullImage.Width / 20;
            frameHeight = FullImage.Height;
            //left = -frameWidth;
            //top = Screen.PrimaryScreen.WorkingArea.Height / 2f;

            timerSpeed.Interval = timerInterval;
            timerSpeed.Enabled = true;
            timerSpeed.Tick += new EventHandler(timerSpeed_Tick);

            this.DoubleClick += new EventHandler(Form2_DoubleClick);
            this.MouseDown += new MouseEventHandler(Form2_MouseDown);
            this.MouseUp += new MouseEventHandler(Form2_MouseUp);
            this.MouseMove += new MouseEventHandler(Form2_MouseMove);

            if (this.Gender == FishGender.None)
                this.Gender = (FishGender)(new Random().Next(1, 3));

            if (string.IsNullOrEmpty(this.FirstName) && string.IsNullOrEmpty(this.LastName))
                GenerateName();
            
            var t = new System.Threading.Thread(() => { parentForm.exitHandle.WaitOne(new Random().Next(20000 * 60, 80000 * 60)); Kill("died of old age"); });
            t.Start();
        }

        #region Override
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (FishEvent != null)
                FishEvent(this, new FishEventArgs(this, FishEventEnum.Born));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
            haveHandle = false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            InitializeStyles();
            base.OnHandleCreated(e);
            haveHandle = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cParms = base.CreateParams;
                cParms.ExStyle |= 0x00080000; // WS_EX_LAYERED
                return cParms;
            }
        }

        #endregion

        void Form2_MouseUp(object sender, MouseEventArgs e)
        {
            count = 0;
            MaxCount = new Random().Next(70) + 40;
            timerSpeed.Interval = new Random().Next(timerInterval) + 2;
            speedMode = true;
            mouseDown = false;
            stopIt = false;
        }

        private void InitializeStyles()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        void timerSpeed_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed)
                return;

            if (!this.TopMost)
                this.TopMost = true;

            if (!IsDead)
            {
                // calculate scale if is a baby
                if (SizeScale < MaxSizeScale)
                {
                    double totalMins = DateTime.Now.Subtract(BirthDate).TotalMinutes;
                    SizeScale = Math.Min(MaxSizeScale, ((totalMins + 5) / 20));
                }

                if (!IsMature && DateTime.Now.Subtract(BirthDate).TotalMinutes > 15)
                {
                    IsMature = true;
                    if (FishEvent != null)
                        FishEvent(this, new FishEventArgs(this, FishEventEnum.Matured, "became of age"));
                }
            }

            if (!mouseDown)
            {
                count++;
                if (count > MaxCount)
                {
                    MaxCount = new Random().Next(70) + 20;
                    if (IsDead)
                    {
                        stepX = 0;
                        if (top > 0)
                            stepY = -1;
                    }
                    else
                    {
                        count = 0;
                        var yVal = (float)new Random().NextDouble() - 0.65f;
                        stepX = ((float)new Random().NextDouble() * speedMultiplier + 0.5f);
                        stepY = ((yVal) * ((float)new Random().NextDouble() * 8f));

                        if (top > Screen.PrimaryScreen.Bounds.Height * .75 && stepY > 0)
                            stepY *= -1;
                        else if (top < Screen.PrimaryScreen.Bounds.Height * .25 && stepY < 0)
                            stepY *= -1;


                        heightValues.Add(top + stepY);
                        if (heightValues.Count > 100)
                            heightValues.RemoveAt(0);

                        if ((int)(MaxCount / 2) == new Random().Next(MaxCount / 2))
                            toRight = !toRight;
                    }

                    Destination = new Point((int)(stepX * MaxCount), (int)(stepY * MaxCount));
                    Console.WriteLine("{0} {1}'s Destination : {2}", FirstName, LastName, Destination);
                }

                left = (left + (toRight ? 1 : -1) * stepX);
                top = (top + stepY);
                FixLeftTop();
                this.Left = (int)left;
                this.Top = (int)top;
            }

            if(!IsDead)
                frame++;

            if (frame >= frameCount) frame = 0;

            if (this.Size != FrameImage.Size)
                this.Size = FrameImage.Size;

            SetBits(FrameImage);
        }

        private void FixLeftTop()
        {
            if (toRight && left > Screen.AllScreens.Sum(x => x.WorkingArea.Width))
            {
                toRight = false;
                frame = 0;
                count = 0;
            }
            else if (!toRight && left < -frameWidth)
            {
                toRight = true;
                frame = 0;
                count = 0;
            }
            if (top < -frameHeight)
            {
                stepY = 1f;
                count = 0;
            }
            else if (top > Screen.PrimaryScreen.WorkingArea.Height)
            {
                stepY = -1f;
                count = 0;
            }
        }

        private Image FullImage
        {
            get
            {
                if (toRight)
                    return GoldFishPet.Properties.Resources.Right;
                else
                    return GoldFishPet.Properties.Resources.Left;
            }
        }

        private object cacheLock = new object();
        private Dictionary<int, Dictionary<Size, Dictionary<bool, Dictionary<bool,Bitmap>>>> cachedImages = new Dictionary<int, Dictionary<Size, Dictionary<bool, Dictionary<bool, Bitmap>>>>();

        public Bitmap FrameImage
        {
            get
            {
                var size = new Size((int)(frameWidth * SizeScale), (int)(frameHeight * SizeScale));
                lock (cacheLock)
                {
                    if (!cachedImages.ContainsKey(frame))
                        cachedImages.Add(frame, new Dictionary<Size, Dictionary<bool, Dictionary<bool, Bitmap>>>());

                    var sizeCache = cachedImages[frame];
                    if (!sizeCache.ContainsKey(size))
                        sizeCache.Add(size, new Dictionary<bool, Dictionary<bool, Bitmap>>());

                    var flipCache = sizeCache[size];
                    if (!flipCache.ContainsKey(IsDead))
                        flipCache.Add(IsDead, new Dictionary<bool, Bitmap>());

                    var directionCache = flipCache[IsDead];
                    if (!directionCache.ContainsKey(toRight))
                    {
                        Bitmap bitmap = new Bitmap(frameWidth, frameHeight);
                        Graphics g = Graphics.FromImage(bitmap);
                        g.DrawImage(FullImage, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(frameWidth * frame, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                        if (SizeScale != 1)
                        {
                            bitmap = ScaleImage(bitmap, (int)(bitmap.Width * SizeScale), (int)(bitmap.Height * SizeScale));
                        }
                        if (IsDead)
                        {
                            bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        }
                        directionCache.Add(toRight, bitmap);
                    }
                }
                
                return cachedImages[frame][size][IsDead][toRight];
            }
        }

        void Form2_DoubleClick(object sender, EventArgs e)
        {
            if (!IsDead)
                Kill("was killed by eating bad fish food.");
            else
                Resurrect("was given divine fish food and resurrected.");
            //this.Dispose();
        }

        void Form2_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Left += (e.X - oldPoint.X);
                this.Top += (e.Y - oldPoint.Y);
                left = this.Left;
                top = this.Top;
                FixLeftTop();
            }
        }

        void Form2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (!IsDead)
                    Kill("was killed by tapping on the tank.");
                else
                    Resurrect("was actually an immortal, and as such is no longer dead.");
            }
            oldPoint = e.Location;
            mouseDown = true;
            stopIt = true;
        }

        public static Bitmap ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * Math.Max(ratio, 0.1));
            var newHeight = (int)(image.Height * Math.Max(ratio, 0.1));

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        private void GenerateName()
        {
            var name = "";
            var index = new Random().Next(1, 2);
            if (Gender == FishGender.Male || (Gender == FishGender.Hermy && index == 1))
                name = NameGenerator.GenerateRandomMaleFirstAndLastName();
            else if (Gender == FishGender.Female || Gender == FishGender.Hermy)
                name = NameGenerator.GenerateRandomFemaleFirstAndLastName();

            var chars = name.ToCharArray().ToList();
            var first = chars[0].ToString();
            chars.RemoveAt(0);

            for (int i = 1; i < name.Length; i++)
            {
                // if it is lowercase
                if (chars[0].ToString() == chars[0].ToString().ToLower())
                {
                    // add it to the first name
                    first += chars[0];
                    // and remove it from the list
                    chars.RemoveAt(0);
                }
                else
                    break;
            }

            var last = "";
            foreach (var c in chars)
            {
                last += c;
            }

            this.FirstName = first;
            this.LastName = last;
        }

        public void SetBits(Bitmap bitmap)
        {
            if (!haveHandle) return;

            if (!Bitmap.IsCanonicalPixelFormat(bitmap.PixelFormat) || !Bitmap.IsAlphaPixelFormat(bitmap.PixelFormat))
                throw new ApplicationException("The picture must be 32bit picture with alpha channel.");
            
            IntPtr oldBits = IntPtr.Zero;
            IntPtr screenDC = Win32.GetDC(IntPtr.Zero);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr memDc = Win32.CreateCompatibleDC(screenDC);

            try
            {
                Win32.Point topLoc = new Win32.Point(Left, Top);
                Win32.Size bitMapSize = new Win32.Size(bitmap.Width, bitmap.Height);
                Win32.BLENDFUNCTION blendFunc = new Win32.BLENDFUNCTION();
                Win32.Point srcLoc = new Win32.Point(0, 0);

                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBits = Win32.SelectObject(memDc, hBitmap);

                blendFunc.BlendOp = Win32.AC_SRC_OVER;
                blendFunc.SourceConstantAlpha = 255;
                blendFunc.AlphaFormat = Win32.AC_SRC_ALPHA;
                blendFunc.BlendFlags = 0;

                Win32.UpdateLayeredWindow(Handle, screenDC, ref topLoc, ref bitMapSize, memDc, ref srcLoc, 0, ref blendFunc, Win32.ULW_ALPHA);
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBits);
                    Win32.DeleteObject(hBitmap);
                }
                Win32.ReleaseDC(IntPtr.Zero, screenDC);
                Win32.DeleteDC(memDc);
            }
        }

        public void RunAway()
        {
            var ex = new MouseEventArgs(MouseButtons.Left, 1, (int)left, (int)top, 0);
            Form2_MouseDown(this, ex);
            Form2_MouseUp(this, ex);
        }

        public FishForm[] CreateBaby(FishForm daddy)
        {
            var babies = new List<FishForm>();

            if (new Random().NextDouble() > 0.8)
            {
                this.LastBred = DateTime.Now;
                daddy.LastBred = DateTime.Now;

                var location = new Point(this.Location.X - this.Width, this.Location.Y);

                var babyFish = new FishForm(this.toRight, location, 0.4);
                babyFish.Show();
                babies.Add(babyFish);

                var seed = new Random().NextDouble();
                if (seed < 0.2)
                {
                    // Twins
                    babyFish = new FishForm(this.toRight, location, 0.4);
                    babyFish.Show();
                    babies.Add(babyFish);
                }
                if (seed < 0.05)
                {
                    // Triplets
                    babyFish = new FishForm(this.toRight, location, 0.4);
                    babyFish.Show();
                    babies.Add(babyFish);
                }

                if ((Math.Round(new Random((int)(seed * 10)).NextDouble(), 2) < 0.1))
                {
                    Kill("died in child birth.");
                }
            }

            return babies.ToArray();
        }

        public void Kill(string reason)
        {
            if (!this.IsDead)
            {
                this.IsDead = true;

                if (FishEvent != null)
                    FishEvent(this, new FishEventArgs(this, FishEventEnum.Died, reason));
            }
        }

        public void Resurrect(string reason)
        {
            if (this.IsDead)
            {
                this.IsDead = false;

                if (FishEvent != null)
                    FishEvent(this, new FishEventArgs(this, FishEventEnum.Resurrected, reason));
            }
        }

        public void Flush()
        {
            if (this.FishEvent != null)
                FishEvent(this, new FishEventArgs(this, FishEventEnum.Flushed, "was flushed."));

            if (!this.IsDisposed && this.IsHandleCreated)
                this.Invoke(new Action(() =>
                {
                    this.timerSpeed.Stop();
                    this.Dispose();
                }));
            
        }

        private string getGenderThing()
        {
            switch (this.Gender)
            {
                case FishGender.Hermy:
                    return "H";
                case FishGender.Male:
                    return "M";
                case FishGender.Female:
                    return "F";
                default:
                    return "N";
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", getGenderThing(), this.FullName);
        }

        public enum FishEventEnum
        {
            Born,
            Matured,
            Died,
            Resurrected,
            Flushed
        }

        public enum FishGender
        {
            None,
            Hermy,
            Male,
            Female
        }

        public class FishEventArgs
        {
            public FishForm Fish { get; private set; }
            public FishEventEnum Action { get; private set; }
            public string Reason { get; set; }

            public FishEventArgs(FishForm fish, FishEventEnum action, string reason = "")
            {
                this.Fish = fish;
                this.Action = action;
                this.Reason = reason;
            }
        }
    }
}