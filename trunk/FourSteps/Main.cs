using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using gma.System.Windows;
using ZoomMouse.Properties;
using System.Threading;

namespace ZoomMouse
{
    public partial class Main : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        enum FourStepStage
        {
            FIRST_TIME_RUN = 0, //When first time message appears
            ON_SCAN = 1, //When ZoomMouse has not initiated the scan
            ON_ZOOM = 2, //On scan
            ON_FINAL_CLICK = 3, //On click acition selection
        }

        FourStepStage currentStage = FourStepStage.FIRST_TIME_RUN;
        FirstTimeRun firstTimeRunDialog;
        RangeWindow rangeWindow;
        Point zoomedCursorPoint;
        bool disableClickCapture = false;
        UserActivityHook actHook;
        Screen primaryScreen;
        Rectangle brightRectangle;
        int actionToPerform;
        float zoomLevel;
        BrightnessCorrection filterBright = new BrightnessCorrection();
        Bitmap dimmedScreen;
        //Bitmap cursorImage;
        
        public Main()
        {
            InitializeComponent();
            //typeof(Panel).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, splitContainer1.Panel1, new object[] { true });            
        }
        
        #region Internals
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.screenUpdater = new System.Windows.Forms.Timer(this.components);
            this.afterActionDelay = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.splitContainer1.Panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.splitContainer1_Panel1_MouseDown);
            this.splitContainer1.Panel1MinSize = 100;
            this.splitContainer1.Panel2Collapsed = true;
            this.splitContainer1.Panel2MinSize = 70;
            this.splitContainer1.Size = new System.Drawing.Size(751, 527);
            this.splitContainer1.SplitterDistance = 100;
            this.splitContainer1.TabIndex = 0;
            // 
            // screenUpdater
            // 
            this.screenUpdater.Enabled = true;
            this.screenUpdater.Interval = 20;
            this.screenUpdater.Tick += new System.EventHandler(this.screenUpdater_Tick);
            // 
            // afterActionDelay
            // 
            this.afterActionDelay.Interval = 200;
            this.afterActionDelay.Tick += new System.EventHandler(this.afterActionDelay_Tick);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(751, 527);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        } 
        #endregion

        #region LoadAndClosingEvents
        private void Main_Load(object sender, EventArgs e)
        {
            this.Hide();

            currentStage = FourStepStage.FIRST_TIME_RUN;
            primaryScreen = Screen.PrimaryScreen;
            zoomLevel = 4.0f;

            actHook = new UserActivityHook(true, true); //Create an instance with global hooks
            actHook.OnMouseActivity += new MouseEventHandler(MouseActivity); //Currently there is no mouse activity
            actHook.Start();

            //this.DoubleBuffered = true;
            //this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            //this.UpdateStyles();

            firstTimeRunDialog = new FirstTimeRun();
            firstTimeRunDialog.Show();

            rangeWindow = new RangeWindow();
            rangeWindow.Size = new Size((int)(primaryScreen.Bounds.Width / zoomLevel), (int)(primaryScreen.Bounds.Height / zoomLevel));
            //rangeWindow.Show();

            //cursorImage = Resources.cursor;

            //Uncomment to make clickable trough
            //int initialStyle = GetWindowLong(this.Handle, -20);
            //SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            //TakeScreenshot(); //Uncomment to take screeshot when program starts
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            actHook.Stop();
            Cursor.Show();
        }
        #endregion

        #region Keyboard and Mouse events
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            } else if (keyData == Keys.F12)
            {                
                disableClickCapture = true;                

                //Show the options panel
                this.Hide();
                OptionsMenu options = new OptionsMenu(this);
                if (options.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Set the values and start/stop the timer
                    zoomLevel = (float)options.numericUpDown1.Value;
                }
                disableClickCapture = false;
                ResetFourSteps();
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void ResetFourSteps()
        {            
            currentStage = FourStepStage.ON_ZOOM;
            primaryScreen = Screen.PrimaryScreen;
            rangeWindow = new RangeWindow();
            rangeWindow.Size = new Size((int)(primaryScreen.Bounds.Width / zoomLevel), (int)(primaryScreen.Bounds.Height / zoomLevel));
            rangeWindow.Show();
        }

        public void MouseActivity(object sender, MouseEventArgs e)
        {
            if ((e.Button.ToString().CompareTo("Left") == 0) && (disableClickCapture == false))
            {
                if (currentStage == FourStepStage.FIRST_TIME_RUN)
                {
                    currentStage = FourStepStage.ON_SCAN;
                    if (firstTimeRunDialog != null)
                    {
                        firstTimeRunDialog.Close();
                    }
                }
                if (currentStage == FourStepStage.ON_SCAN)
                {
                    currentStage = FourStepStage.ON_ZOOM;
                    rangeWindow.Show();
                } else if (currentStage == FourStepStage.ON_ZOOM)
                {
                    currentStage = FourStepStage.ON_FINAL_CLICK;                    
                    ZoomScreenshot();
                } else if (currentStage == FourStepStage.ON_FINAL_CLICK)
                {
                    disableClickCapture = true;
                    actionToPerform = 1;
                    zoomedCursorPoint = new Point((int)(Cursor.Position.X / zoomLevel), (int)(Cursor.Position.Y / zoomLevel));
                    afterActionDelay.Start();                    
                }
            }
        }

        private void afterActionDelay_Tick(object sender, EventArgs e)
        {
            afterActionDelay.Enabled = false;
            afterActionDelay.Stop();            
            this.Hide();
            currentStage = FourStepStage.ON_ZOOM;

            Cursor.Position = new Point(rangeWindow.Location.X + zoomedCursorPoint.X, rangeWindow.Location.Y + zoomedCursorPoint.Y);            
            if (actionToPerform == 1)
            {
                mouse_event((int)(MouseEventFlags.LEFTDOWN | MouseEventFlags.LEFTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);
                //mouse_event((int)(MouseEventFlags.LEFTDOWN), Cursor.Position.X, Cursor.Position.Y, 0, 0);
                //mouse_event((int)(MouseEventFlags.LEFTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);
            }
            else if (actionToPerform == 2)
            {
                mouse_event((int)(MouseEventFlags.LEFTDOWN | MouseEventFlags.LEFTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);
                Thread.Sleep(100);
                mouse_event((int)(MouseEventFlags.LEFTDOWN | MouseEventFlags.LEFTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);
            }
            else if (actionToPerform == 3)
            {
                mouse_event((int)(MouseEventFlags.RIGHTDOWN | MouseEventFlags.RIGHTUP), Cursor.Position.X, Cursor.Position.Y, 0, 0);
            }
            else if (actionToPerform == 4)
            {
                //Nothing to do
            }

            Thread.Sleep(200); //Needed so Windows captures the click
            rangeWindow.Show();
            disableClickCapture = false;
        }
        #endregion
        
        private void screenUpdater_Tick(object sender, EventArgs e)
        {            
            if (currentStage == FourStepStage.ON_ZOOM)
            {
                Point newPosition = new Point(Cursor.Position.X - rangeWindow.Size.Width / 2, Cursor.Position.Y - rangeWindow.Size.Height / 2);
                if (newPosition.X < primaryScreen.Bounds.X)
                {
                    newPosition.X = primaryScreen.Bounds.X;
                } else if (newPosition.X > primaryScreen.Bounds.Width - rangeWindow.Width)
                {
                    newPosition.X = primaryScreen.Bounds.Width - rangeWindow.Width;
                }

                if (newPosition.Y < primaryScreen.Bounds.Y)
                {
                    newPosition.Y = primaryScreen.Bounds.Y;
                }
                else if (newPosition.Y > primaryScreen.Bounds.Height - rangeWindow.Height)
                {
                    newPosition.Y = primaryScreen.Bounds.Height - rangeWindow.Height;
                }
                rangeWindow.Location = newPosition;
            }
        }

        public void ZoomScreenshot()
        {
            Bitmap zoomedScreen;

            rangeWindow.Hide();
            using (var bitmap = new Bitmap(rangeWindow.Width, rangeWindow.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rangeWindow.Location.X, rangeWindow.Location.Y, 0, 0, rangeWindow.Size);
                }
                zoomedScreen = (Bitmap)bitmap.Clone();
            }


            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            Bitmap newImage = new Bitmap(primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(zoomedScreen, new Rectangle(0, 0, newImage.Width, newImage.Height));
                gr.DrawString("Presione F12 para ver las opciones. Presione Escape para salir del programa.", this.Font, Brushes.Black, new PointF(0, 0));
            }
            zoomedScreen = (Bitmap)newImage.Clone();

            Cursor.Position = new Point(primaryScreen.Bounds.Width / 2, primaryScreen.Bounds.Height / 2);

            splitContainer1.Panel1.BackgroundImage = zoomedScreen;
            this.Show();
            this.Focus();
        }

        private void splitContainer1_Panel1_MouseDown(object sender, MouseEventArgs e)
        {

        }
    }
}
