using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ZedGraph;
using System.IO.Ports;
using System.IO;
using PCComm;
using System.Text.RegularExpressions;
using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using ILNumerics.Toolboxes;
using System.Globalization;


using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Cameras;
using SharpGL.SceneGraph.Collections;
using SharpGL.SceneGraph.Primitives;
using SharpGL.Serialization;
using SharpGL.SceneGraph.Core;
using SharpGL.Enumerations;
using SharpGL.SceneGraph.Assets;



using System.IO;
using System.Drawing.Drawing2D;


using System.Net;
using System.Net.NetworkInformation;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Threading;

using System.Drawing.Imaging;
//using System.Drawing.Bitmap;
using System.Diagnostics;
using System.Data.OleDb;


using GMap;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;






namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private SerialPort comPort = new SerialPort();
        CommunicationManager comm = new CommunicationManager();
        CommunicationManager commAT = new CommunicationManager();
        static double xTimeStamp = 0;

        // awal mapss
        
       static GUI_settings gui_settings;


        //public GMapControl gmap;

        static Pen drawPen;
        static System.Drawing.SolidBrush drawBrush;
        static System.Drawing.Font drawFont;

        static Int16 nav_lat, nav_lon;
        static int GPS_lat_old, GPS_lon_old;
        static bool GPSPresent = true;
        static int iWindLat = 0;
        static int iWindLon = 0;
        static int iAngleLat = 0;
        static int iAngleLon = 0;
        static double SpeedLat = 0;
        static double SpeedLon = 0;

        //Routes on Map
        static GMapRoute GMRouteFlightPath;
        //static GMapRoute GMRouteMission;

        //Map Overlays
        static GMapOverlay GMOverlayFlightPath;// static so can update from gcs
        static GMapOverlay GMOverlayWaypoints;
        static GMapOverlay GMOverlayMission;
        static GMapOverlay GMOverlayLiveData;
        //static GMapOverlay GMOverlayPOI;

        static GMapProvider[] mapProviders;
        //static PointLatLng copterPos = new PointLatLng(47.402489, 19.071558);       //Just the corrds of my flying place
        static PointLatLng copterPos = new PointLatLng(-6.976916, 107.630210);
        //static PointLatLng copterPos;
        static bool isMouseDown = false;
        static bool isMouseDraging = false;

        static bool bPosholdRecorded = false;
        static bool bHomeRecorded = false;

        // markers
        GMarkerGoogle currentMarker;
        GMapMarkerRect CurentRectMarker = null;
        GMapMarker center;
        //GMapMarker markerGoToClick = new GMarkerGoogle(new PointLatLng(0.0, 0.0), GMarkerGoogleType.lightblue);

        List<PointLatLng> points = new List<PointLatLng>();

        PointLatLng GPS_pos, GPS_pos_old;
        PointLatLng Home;
        PointLatLng end;
        PointLatLng start;

        //static GMapControl gmap ;


        int countx = 15;

        bool antena = false;

        public Form1()
        {
            InitializeComponent();
            timer1.Tick += new EventHandler(timer1_Tick);
            

            try
            {
                System.Net.IPHostEntry e =
                     System.Net.Dns.GetHostEntry("www.google.com");
            }
            catch
            {
                gmap.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection avaible, going to CacheOnly mode.",
                "GMap.NET - Demo.WindowsForms", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            }


            gmap.MinZoom = 1;
            gmap.MaxZoom = 50;
            gmap.CacheLocation = Path.GetDirectoryName(Application.ExecutablePath) + "/mapcache/";
            gmap.Position = new PointLatLng(GPS_pos.Lat, GPS_pos.Lng);

            mapProviders = new GMapProvider[7];


            gmap.MapProvider = GMapProviders.GoogleMap;
            mapProviders[0] = GMapProviders.BingHybridMap;
            mapProviders[1] = GMapProviders.BingSatelliteMap;
            mapProviders[2] = GMapProviders.GoogleSatelliteMap;
            mapProviders[3] = GMapProviders.GoogleHybridMap;
            mapProviders[4] = GMapProviders.OviSatelliteMap;
            mapProviders[5] = GMapProviders.OviHybridMap;

            //gmap.OnPositionChanged += new PositionChanged(gmap_OnPositionChanged);

            for (int i = 0; i < 6; i++)
            {
                comboBox3.Items.Add(mapProviders[i]);
            }
        }


        public class GUI_settings
        {
            public int iMapProviderSelectedIndex { get; set; }
            public GUI_settings()
            {
                iMapProviderSelectedIndex = 1;  //Bing Map
            }
        }


        public class Stuff
        {
            public static bool PingNetwork(string hostNameOrAddress)
            {
                bool pingStatus = false;

                /*using (Ping p = new Ping())
                {
                    byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    int timeout = 4444; // 4s

                    try
                    {
                        PingReply reply = p.Send(hostNameOrAddress, timeout, buffer);
                        pingStatus = (reply.Status == IPStatus.Success);
                    }
                    catch (Exception)
                    {
                        pingStatus = false;
                    }
                }*/

                return pingStatus;
            }
        }

        // akhir maps

        List<float> tempX = new List<float>();
        List<float> tempY = new List<float>();
        List<float> tempZ = new List<float>();

        string header, Accx, Accy, Accz, Gyrox = "0", Gyroy = "0", Gyroz = "0", Suh, Tek, Rol, Pit, Yaw, ZZ, Alt, Lat, Lng;

        /*public Form1()
        {
            InitializeComponent();
            timer1.Tick += new EventHandler(timer1_Tick);
        }*/

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            string[] port = SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            foreach (String sambung in port)
            {
                comboBox1.Items.Add(sambung);
            }
        }
        private void button1_Click(object sender, EventArgs e)//ini untuk neken tombol connect
        {
            if (comm.isOpen() == true)
            {
                System.Threading.Thread.Sleep(100);
                comm.ClosePort();
            }
            else
            {
                if (comboBox1.Text == "") { return; }
                comm.Parity = "None";
                comm.StopBits = "One";
                comm.DataBits = "8";
                comm.BaudRate = comboBox2.Text;
                comm.DisplayWindow = richTextBox1;
                comm.PortName = comboBox1.Text;
                comm.OpenPort();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            comm.WriteData("1");
            timer1.Interval = 100;
            timer1.Enabled = true;
            timer1.Start();
            //DiscardInBuffer();
            comm.gambar = 0;
            timer1.Tick += new EventHandler(FastTimer);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Dispose();
            richTextBox1.Enabled = false;

            button1.Enabled = true;
            button3.Enabled = true;


        }
       

        private void button4_Click(object sender, EventArgs e)
        {
            comm.WriteData("2");
            timer1.Interval = 100;
            timer1.Enabled = true;
            timer1.Start();
            //DiscardInBuffer();
            comm.gambar = 0;
            timer1.Tick += new EventHandler(FastTimer);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
       
        



        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void zedGraphControl1_Load(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

      
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            gui_settings = new GUI_settings();

            /*gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;*/
            //gmap.Position = GPS_pos;


            /*mapmap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            mapmap.Position = new PointLatLng(ln, lt);*/

            /*GMapOverlay markersOverlay = new GMapOverlay("markers");
            GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(lt, ln), GMarkerGoogleType.green);
            markersOverlay.Markers.Add(marker);
            mapmap.Overlays.Add(markersOverlay);*/

            //label14.Text = lat;
            //label15.Text = lon;

            comboBox3.SelectedIndex = gui_settings.iMapProviderSelectedIndex;
            gmap.MapProvider = mapProviders[gui_settings.iMapProviderSelectedIndex];
            gmap.Zoom = 18;
            gmap.Invalidate(false);

            int w = gmap.Size.Width;
            gmap.Width = w + 1;
            gmap.Width = w;
            gmap.ShowCenter = false;

            //jamku = DateTime.Now;
            //timer3.Enabled = true;
            //  backgroundWorker1.RunWorkerAsync();

            /*if (label26.Text == "00:00:12.0000000")
            {
                timer1.Stop();
                timer1.Enabled = false;
                timerr.Stop();
                label26.Text = "00:00:00.0";
                comm.WriteData("x");
            }*/


            GraphPane myPane = zedGraphControl1.GraphPane;

            Lax = new RollingPointPairList(300);
            Kax = myPane.AddCurve("acc_roll", Lax, Color.Pink, SymbolType.None);
            Lay = new RollingPointPairList(300);
            Kay = myPane.AddCurve("acc_roll", Lay, Color.Blue, SymbolType.None);
            Laz = new RollingPointPairList(300);
            Kaz = myPane.AddCurve("acc_roll", Laz, Color.Purple, SymbolType.None);
            buat1 = new RollingPointPairList(300);
            test1 = myPane.AddCurve("acc_roll", buat1, Color.Red, SymbolType.None);
            buat2 = new RollingPointPairList(300);
            test2 = myPane.AddCurve("acc_roll", buat1, Color.Green, SymbolType.None);
            buat3 = new RollingPointPairList(300);
            test3 = myPane.AddCurve("acc_roll", buat1, Color.Green, SymbolType.None);

            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 30;
            myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Scale.MajorStep = 5;
            zedGraphControl1.AxisChange();
        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void openGLControl1_Load(object sender, EventArgs e)
        {

        }

        string[] data;
        int yaw;
        float roll;
        float r, p, h;

        public delegate void myDelegate();

        private void UpdateData()
        {
            richTextBox1.Invoke(new EventHandler(delegate
            {
                textBox1.Text = header;
                textBox2.Text = Accx;
                textBox3.Text = Accy;
                textBox4.Text = Accz;
                textBox5.Text = Lng;
                textBox6.Text = Lat;
                textBox7.Text = Gyroz;


            }));
            richTextBox1.ScrollToCaret();
            xTimeStamp = xTimeStamp + 1;

            headingIndicatorInstrumentControl1.SetHeadingIndicatorParameters(yaw);
            attitudeIndicatorInstrumentControl1.SetAttitudeIndicatorParameters(Convert.ToDouble(Gyroy), Convert.ToDouble(Gyrox));
            turnCoordinatorInstrumentControl4.SetTurnCoordinatorParameters(roll, roll);

            //artifical_horizon1.SetArtificalHorizon(roll, pitch);
            //turnCoordinatorInstrumentControl1.SetTurnCoordinatorParameters(r, r);

            //artifical_horizon1.SetArtificalHorizon(roll, pitch);
            //turnCoordinatorInstrumentControl1.SetTurnCoordinatorParameters(r, r);

            //Dari Sini Bagian Odometry
            var inrows = 0;
            inrows = inrows + 1;

            Array<float> datasamples = ILMath.zeros<float>(3, (int)inrows);
            datasamples["0;:"] = float.Parse(Gyrox);
            datasamples["1;:"] = float.Parse(Gyroy);
            datasamples["2;:"] = float.Parse(Gyroz);

            panel2.Scene.First<PlotCube>().Add(new Points { Positions = datasamples });

            var styles = Enum.GetValues(typeof(MarkerStyle));
            var lp = panel2.Scene.First<Points>();
            lp.Positions.Update(datasamples);
            lp.Configure();
            panel2.Refresh();
            panel2.ResetText();

            //Akhir Bagian Dari Odometry

            //Dari Sini Bagian dari checkbox zedgraph
            //if (checkBox1.Checked) { Lax.Add(xTimeStamp, Convert.ToDouble(Gyrox)); }
            //if (checkBox2.Checked) { Lay.Add(xTimeStamp, Convert.ToDouble(Gyroy)); }
            //if (checkBox3.Checked) { Laz.Add(xTimeStamp, Convert.ToDouble(Gyroz)); }
            //if (checkBox4.Checked) { Lcx.Add(xTimeStamp, Convert.ToDouble(Accx)); }
            //if (checkBox5.Checked) { Lcy.Add(xTimeStamp, Convert.ToDouble(Accy)); }
            //if (checkBox6.Checked) { Lcz.Add(xTimeStamp, Convert.ToDouble(Accz)); }

            //Kax.IsVisible = checkBox1.Checked;
            //Kay.IsVisible = checkBox2.Checked;
            //Kaz.IsVisible = checkBox3.Checked;
            //Kcx.IsVisible = checkBox4.Checked;
            //Kcy.IsVisible = checkBox5.Checked;
            //Kcz.IsVisible = checkBox6.Checked;
            //akhir dari checkbox zedgraph

            foreach (Polygon polygon in polygons)
            {
                polygon.Transformation.RotateX = r;
                polygon.Transformation.RotateY = p;
                polygon.Transformation.RotateZ = h;
            }
        }

        private void gMapControl1_Load(object sender, EventArgs e)
        {

        }


        public class GMapMarkerRect : GMapMarker
        {
            public Pen Pen = new Pen(Brushes.White, 2);

            public Color Color { get { return Pen.Color; } set { Pen.Color = value; } }
            public GMapMarker InnerMarker;
            public int wprad = 0;
            public GMapControl gmap;

            public GMapMarkerRect(PointLatLng p)
                : base(p)
            {
                Pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                Size = new System.Drawing.Size(50, 50);
                Offset = new System.Drawing.Point(-Size.Width / 2, -Size.Height / 2 - 20);
            }
            public override void OnRender(Graphics g)
            {
                base.OnRender(g);
                if (wprad == 0 || gmap == null)
                    return;
                if (Pen.Color == Color.Blue)
                    Pen.Color = Color.White;
                double width = (gmap.MapProvider.Projection.GetDistance(gmap.FromLocalToLatLng(0, 0), gmap.FromLocalToLatLng(gmap.Width, 0)) * 1000.0);
                double height = (gmap.MapProvider.Projection.GetDistance(gmap.FromLocalToLatLng(0, 0), gmap.FromLocalToLatLng(gmap.Height, 0)) * 1000.0);
                double m2pixelwidth = gmap.Width / width;
                double m2pixelheight = gmap.Height / height;

                GPoint loc = new GPoint((int)(LocalPosition.X - (m2pixelwidth * wprad * 2)), LocalPosition.Y);
                g.DrawArc(Pen, new System.Drawing.Rectangle((int)(LocalPosition.X - Offset.X - (Math.Abs(loc.X - LocalPosition.X) / 2)), (int)(LocalPosition.Y - Offset.Y - Math.Abs(loc.X - LocalPosition.X) / 2), (int)(Math.Abs(loc.X - LocalPosition.X)), (int)(Math.Abs(loc.X - LocalPosition.X))), 0, 360);
            }
        }

        public class GMapMarkerCopter : GMapMarker
        {
            const float rad2deg = (float)(180 / Math.PI);
            const float deg2rad = (float)(1.0 / rad2deg);

            //static readonly System.Drawing.Size SizeSt = new System.Drawing.Size(global::my_GUI.Properties.Resources.LongNeedleAltimeter.Width, global::my_GUI.Properties.Resources.LongNeedleAltimeter.Height);
            float heading = 0;
            float cog = -1;
            float target = -1;
            //byte coptertype;

            //public GMapMarkerCopter(PointLatLng p, float heading, float cog, float target, byte coptertype)
            public GMapMarkerCopter(PointLatLng p, float heading, float cog, float target)
                : base(p)
            {
                this.heading = heading;
                this.cog = cog;
                this.target = target;
                //this.coptertype = coptertype;
                //Size = SizeSt;
            }

            public override void OnRender(Graphics g)
            {
                System.Drawing.Drawing2D.Matrix temp = g.Transform;
                g.TranslateTransform(LocalPosition.X, LocalPosition.Y);

                //Image pic = global::my_GUI.Properties.Resources.LongNeedleAltimeter;


                int length = 100;
                // anti NaN
                g.DrawLine(new Pen(Color.Red, 2), 0.0f, 0.0f, (float)Math.Cos((heading - 90) * deg2rad) * length, (float)Math.Sin((heading - 90) * deg2rad) * length);
                //g.DrawLine(new Pen(Color.Black, 2), 0.0f, 0.0f, (float)Math.Cos((cog - 90) * deg2rad) * length, (float)Math.Sin((cog - 90) * deg2rad) * length);
                g.DrawLine(new Pen(Color.Orange, 2), 0.0f, 0.0f, (float)Math.Cos((target - 90) * deg2rad) * length, (float)Math.Sin((target - 90) * deg2rad) * length);
                // anti NaN
                g.RotateTransform(heading);
                //g.DrawImageUnscaled(pic, pic.Width / -2, pic.Height / -2);
                g.Transform = temp;
            }
        }   //if the marker is a copter


        public class GMapMarkerLain : GMapMarker
        {
            const float rad2deg = (float)(180 / Math.PI);
            const float deg2rad = (float)(1.0 / rad2deg);

            //static readonly System.Drawing.Size SizeSt = new System.Drawing.Size(global::my_GUI.Properties.Resources.home.Width, global::my_GUI.Properties.Resources.home.Height);
            float heading = 0;
            float cog = -1;
            float target = -1;

            public GMapMarkerLain(PointLatLng p, float heading, float cog, float target)
                : base(p)
            {
                this.heading = heading;
                this.cog = cog;
                this.target = target;
                //Size = SizeSt;
            }
            public override void OnRender(Graphics g)
            {
                System.Drawing.Drawing2D.Matrix temp = g.Transform;
                g.TranslateTransform(LocalPosition.X, LocalPosition.Y);

                //g.DrawImageUnscaled(p)
                g.Transform = temp;
            }
        }
        void gmap_OnCurrentPositionChanged(PointLatLng point)
        {
            if (point.Lat > 90) { point.Lat = 90; }
            if (point.Lat < 90) { point.Lat = -90; }
            if (point.Lng > 180) { point.Lng = 180; }
            if (point.Lng < -180) { point.Lng = -180; }
            center.Position = point;
        }

        void gmap_OnMarkerLeave(GMapMarker item)
        {
            if (!isMouseDown)
            {
                if (item is GMapMarkerRect)
                {
                    CurentRectMarker = null;
                    GMapMarkerRect rc = item as GMapMarkerRect;
                    rc.Pen.Color = Color.Blue;
                    gmap.Invalidate(false);
                }
            }
        }

        void gmap_OnMarkerEnter(GMapMarker item)
        {
            if (!isMouseDown)
            {
                if (item is GMapMarkerRect)
                {
                    GMapMarkerRect rc = item as GMapMarkerRect;
                    rc.Pen.Color = Color.Red;
                    gmap.Invalidate(false);

                    CurentRectMarker = rc;
                }
            }
        }

        void gmap_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            end = gmap.FromLocalToLatLng(e.X, e.Y);

            if (isMouseDown) // mouse down on some other object and dragged to here.
            {
                if (e.Button == MouseButtons.Left)
                {
                    isMouseDown = false;
                }
                if (!isMouseDraging)
                {
                    if (CurentRectMarker != null)
                    {
                        // cant add WP in existing rect
                    }
                    else
                    {
                        //addWP("WAYPOINT", 0, currentMarker.Position.Lat, currentMarker.Position.Lng, iDefAlt);
                    }
                }
                else
                {
                    if (CurentRectMarker != null)
                    {
                        //update existing point in datagrid
                    }
                }
            }
            if (comm.isOpen() == true) timer1.Start();
            isMouseDraging = false;
        }

        void gmap_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            start = gmap.FromLocalToLatLng(e.X, e.Y);

            if (e.Button == MouseButtons.Left && Control.ModifierKeys != Keys.Alt)
            {
                isMouseDown = true;
                isMouseDraging = false;

                if (currentMarker.IsVisible)
                {
                    currentMarker.Position = gmap.FromLocalToLatLng(e.X, e.Y);
                }
            }
        }

        // move current marker with left holding
        void gmap_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            PointLatLng point = gmap.FromLocalToLatLng(e.X, e.Y);

            currentMarker.Position = point;
            label3.Text = "Lat:" + String.Format("{0:0.000000}", point.Lat) + " Lon:" + String.Format("{0:0.000000}", point.Lng);

            if (!isMouseDown)
            {

            }


            //draging
            if (e.Button == MouseButtons.Left && isMouseDown)
            {
                isMouseDraging = true;
                if (CurentRectMarker == null) // left click pan
                {
                    double latdif = start.Lat - point.Lat;
                    double lngdif = start.Lng - point.Lng;
                    gmap.Position = new PointLatLng(center.Position.Lat + latdif, center.Position.Lng + lngdif);
                }
                else
                {
                    if (comm.isOpen() == true) timer1.Stop();
                    PointLatLng pnew = gmap.FromLocalToLatLng(e.X, e.Y);
                    if (currentMarker.IsVisible)
                    {
                        currentMarker.Position = pnew;
                    }
                    CurentRectMarker.Position = pnew;

                    if (CurentRectMarker.InnerMarker != null)
                    {
                        CurentRectMarker.InnerMarker.Position = pnew;
                        GPS_pos = pnew;
                    }
                }
            }
        }   //when mouse move on map




        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            //gmap.MapProvider = GMapProviders.GoogleSatelliteMap;
            gmap.MapProvider = (GMapProvider)comboBox3.SelectedItem;
            gmap.MinZoom = 5;
            gmap.MaxZoom = 20;
            gmap.Zoom = 18;
            gmap.Invalidate(false);
            gui_settings.iMapProviderSelectedIndex = comboBox3.SelectedIndex;
            //gui_settings.save_to_xml(sGuiSettingsFilename);


            this.Cursor = Cursors.Default;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RectLatLng area = gmap.SelectedArea;
            if (area.IsEmpty)
            {
                DialogResult res = MessageBox.Show("No ripp area defined, ripp displayed on screen?", "Rip", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    area = gmap.ViewArea;
                }

            }

            if (!area.IsEmpty)
            {
                DialogResult res = MessageBox.Show("Ready ripp at Zoom = " + (int)gmap.Zoom + " ?", "GMap.NET", MessageBoxButtons.YesNo);

                for (int i = 1; i <= gmap.MaxZoom; i++)
                {
                    if (res == DialogResult.Yes)
                    {
                        TilePrefetcher obj = new TilePrefetcher();
                        obj.ShowCompleteMessage = false;
                        obj.Start(area, i, gmap.MapProvider, 100, 0);

                    }
                    else if (res == DialogResult.No)
                    {
                        continue;
                    }
                    else if (res == DialogResult.Cancel)
                    {
                        break;
                    }
                }
            }
            else
            {
                //MessageBox.Show("Select map area holding ALT", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateData();
        }


        static LineItem Kax, Kay, Kaz, test1,test2,test3;

        private void importPolygonToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            //  Show a file open dialog.
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = SerializationEngine.Instance.Filter;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                SharpGL.SceneGraph.Scene scene = SerializationEngine.Instance.LoadScene(openDialog.FileName);
                if (scene != null)
                {
                    foreach (var polygon in scene.SceneContainer.Traverse<Polygon>())
                    {
                        //  Get the bounds of the polygon.
                        BoundingVolume boundingVolume = polygon.BoundingVolume;
                        float[] extent = new float[3];
                        polygon.BoundingVolume.GetBoundDimensions(out extent[0], out extent[1], out extent[2]);

                        //  Get the max extent.
                        float maxExtent = extent.Max();

                        //  Scale so that we are at most 10 units in size.
                        float scaleFactor = maxExtent > 10 ? 10.0f / maxExtent : 1;
                        polygon.Transformation.ScaleX = scaleFactor;
                        polygon.Transformation.ScaleY = scaleFactor;
                        polygon.Transformation.ScaleZ = scaleFactor;
                        polygon.Freeze(openGLControl1.OpenGL);
                        polygons.Add(polygon);
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void wirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wirToolStripMenuItem.Checked = true;
            lighterToolStripMenuItem.Checked = false;
            openGLControl1.OpenGL.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Lines);
            openGLControl1.OpenGL.Disable(OpenGL.GL_LIGHTING);
        }

        private void lighterToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            wirToolStripMenuItem.Checked = false;
            lighterToolStripMenuItem.Checked = true;
            openGLControl1.OpenGL.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Filled);
            openGLControl1.OpenGL.Enable(OpenGL.GL_LIGHTING);
            openGLControl1.OpenGL.Enable(OpenGL.GL_LIGHT0);
            openGLControl1.OpenGL.Enable(OpenGL.GL_COLOR_MATERIAL);

        }

        private void panel2_Load(object sender, EventArgs e)
        {

            var inrows = 0;
            inrows = inrows + 1;

            tempX.Add(float.Parse(Gyrox, CultureInfo.InvariantCulture));
            tempY.Add(float.Parse(Gyroy, CultureInfo.InvariantCulture));
            tempZ.Add(float.Parse(Gyroz, CultureInfo.InvariantCulture));

            Array<float> datasamples = ILMath.zeros<float>(3, (int)inrows);

            datasamples["0;:"] = tempX.ToArray();
            datasamples["1;:"] = tempY.ToArray();
            datasamples["2;:"] = tempZ.ToArray();

            tempX.Clear();
            tempY.Clear();
            tempZ.Clear();

            var Scene = new ILNumerics.Drawing.Scene
            {
                new PlotCube(twoDMode: false)
               {
                   new Points
                   {
                       Positions = datasamples,
                       Color = Color.Blue,
                       Size = 10
                   }
               }
            };
            panel2.Scene = Scene;

        }

        private void renderToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        static RollingPointPairList Lax, Lay, Laz, buat1,buat2,buat3;


        private void FastTimer(object sender, EventArgs myEventArgs)
        {
            string[] tempArray = comm.DisplayWindow.Lines;
            string line = tempArray[tempArray.Length - 2];
            if (line == "") return;

            data = Regex.Split(line, " ");
            header = data[0];
            if (comm.gambar == 0)
            {
                if (header == "005" && data.Length == 13)
                {
                    header = data[0];
                    Accx = data[1];
                    Accy = data[2];
                    Accz = data[3];
                    Gyrox = data[4];
                    Gyroy = data[5];
                    Gyroz = data[6];
                    roll = (float)Convert.ToDouble(data[7]);
                    yaw = Convert.ToInt16(data[8]);
                    label1.Text = Convert.ToString(line.Length);

                    Lax.Add(xTimeStamp, Convert.ToDouble(data[1]));
                    Lay.Add(xTimeStamp, Convert.ToDouble(data[2]));
                    Laz.Add(xTimeStamp, Convert.ToDouble(data[3]));
                    buat1.Add(xTimeStamp, Convert.ToDouble(data[4]));
                    buat2.Add(xTimeStamp, Convert.ToDouble(data[5]));
                    buat3.Add(xTimeStamp, Convert.ToDouble(data[6]));

                    xTimeStamp = xTimeStamp + 1;

                    Scale xScale = zedGraphControl1.GraphPane.XAxis.Scale;
                    if (xTimeStamp > xScale.Max - xScale.MajorStep)
                    {
                        xScale.Max = xTimeStamp + xScale.MajorStep;
                        xScale.Min = xScale.Max - 30.0;

                    }
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Invalidate();


                    r = float.Parse(data[4]);
                    p = float.Parse(data[5]);
                    h = float.Parse(data[6]);

                    foreach (Polygon polygon in polygons)
                    {
                        polygon.Transformation.RotateX = r;
                        polygon.Transformation.RotateY = p;
                        polygon.Transformation.RotateZ = h;
                    }
                }
            }
        }
        //----------------------------------------3D MODEL---------------------------------------// 

        float rotate = 0;
        private void openGLControl1_OpenGLDraw_1(object sender, PaintEventArgs e)
        {
            //  The texture identifier.
            /*Texture texture = new Texture(); */

            //  Get the OpenGL object, for quick access.
            SharpGL.OpenGL gl = this.openGLControl1.OpenGL;

            //  Clear and load the identity.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.LoadIdentity();

            //  Bind the texture.
            texture.Bind(gl);

            //  View from a bit away the y axis and a few units above the ground.
            gl.LookAt(-10, -15, 0, 0, 0, 0, 0, 1, 0);


            //  Rotate the objects every cycle.
            // gl.Rotate(rotate, 0.0f, 0.0f, 1.0f);
            //gl.Rotate(float.Parse(data_r), float.Parse(data_p), float.Parse(data_h));

            //  Move the objects down a bit so that they fit in the screen better.
            gl.Translate(0, 0, 0);

            //  Draw every polygon in the collection.
            foreach (Polygon polygon in polygons)
            {
                polygon.PushObjectSpace(gl);
                polygon.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                polygon.PopObjectSpace(gl);
            }
            //  Rotate a bit more each cycle.
            rotate += 1.0f;
        }

        //  A set of polygons to draw.
        List<Polygon> polygons = new List<Polygon>();

        //  The camera.
        SharpGL.SceneGraph.Cameras.PerspectiveCamera camera = new SharpGL.SceneGraph.Cameras.PerspectiveCamera();

        /// <summary>
        /// Handles the Click event of the importPolygonToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>

        Texture texture = new Texture();
        private void importPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //  Show a file open dialog.
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = SerializationEngine.Instance.Filter;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                SharpGL.SceneGraph.Scene scene = SerializationEngine.Instance.LoadScene(openDialog.FileName);
                if (scene != null)
                {
                    foreach (var polygon in scene.SceneContainer.Traverse<Polygon>())
                    {
                        //  Get the bounds of the polygon.
                        BoundingVolume boundingVolume = polygon.BoundingVolume;
                        float[] extent = new float[3];
                        polygon.BoundingVolume.GetBoundDimensions(out extent[0], out extent[1], out extent[2]);

                        //  Get the max extent.
                        float maxExtent = extent.Max();

                        //  Scale so that we are at most 10 units in size.
                        float scaleFactor = maxExtent > 10 ? 10.0f / maxExtent : 1;
                        polygon.Transformation.ScaleX = scaleFactor;
                        polygon.Transformation.ScaleY = scaleFactor;
                        polygon.Transformation.ScaleZ = scaleFactor;
                        polygon.Freeze(openGLControl1.OpenGL);
                        polygons.Add(polygon);
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wirToolStripMenuItem.Checked = true;
            lighterToolStripMenuItem.Checked = false;
            openGLControl1.OpenGL.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Lines);
            openGLControl1.OpenGL.Disable(OpenGL.GL_LIGHTING);
        }

        private void lighterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wirToolStripMenuItem.Checked = false;
            lighterToolStripMenuItem.Checked = true;
            openGLControl1.OpenGL.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Filled);
            openGLControl1.OpenGL.Enable(OpenGL.GL_LIGHTING);
            openGLControl1.OpenGL.Enable(OpenGL.GL_LIGHT0);
            openGLControl1.OpenGL.Enable(OpenGL.GL_COLOR_MATERIAL);
        }
        //--------------------------------------END 3D MODEL-------------------------------------//


    }
}
