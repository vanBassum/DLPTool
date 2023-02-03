﻿using STDLib.Math;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FRMLib.Scope.Controls
{
    public partial class ScopeView : UserControl
    {
        //public ScopeViewSettings Settings { get; set; } = new ScopeViewSettings();
        private ScopeController dataSource = new ScopeController();
        public ScopeController DataSource
        {
            get { return dataSource; }
            set
            {
                dataSource = value;
                if (dataSource != null)
                {
                    dataSource.Settings.PropertyChanged += (a, b) => this.InvokeIfRequired(() => DrawBackground());
                    dataSource.Traces.ListChanged += Traces_ListChanged;
                    dataSource.Cursors.ListChanged += Markers_ListChanged;
                    dataSource.DoRedraw += (a, b) => DrawAll();
                }
            }
        }



        private ContextMenuStrip menu;
        private Point lastClick = Point.Empty;
        private Point lastClickDown = Point.Empty;
        private double horOffsetLastClick = 0;
        private Cursor dragMarker = null;
        private Cursor hoverMarker = null;
        Rectangle viewPort = new Rectangle(0, 0, 0, 0);
        //int pxPerColumn;
        //int pxPerRow;
        int zeroPos;    //Position in px that represents vertical zero


        PictureBox pictureBox1 = new PictureBox();
        PictureBox pictureBox2 = new PictureBox();
        PictureBox pictureBox3 = new PictureBox();

        public ScopeView()
        {
            InitializeComponent();
            DrawAll();

            this.Controls.Add(pictureBox1);
            pictureBox1.Controls.Add(pictureBox2);
            pictureBox2.Controls.Add(pictureBox3);

            pictureBox1.Dock = DockStyle.Fill;
            pictureBox2.Dock = DockStyle.Fill;
            pictureBox3.Dock = DockStyle.Fill;


            pictureBox1.BackColor = Color.Transparent;
            pictureBox2.BackColor = Color.Transparent;
            pictureBox3.BackColor = Color.Transparent;

            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox2.Paint += PictureBox2_Paint;
            pictureBox3.Paint += PictureBox3_Paint;

            pictureBox1.BringToFront();
            pictureBox2.BringToFront();
            pictureBox3.BringToFront();

            menu = new ContextMenuStrip();

            AddMenuItem("Add marker", () => dataSource.Cursors.Add(dragMarker = new Cursor() { X = -dataSource.Settings.HorOffset }));
            AddMenuItem("Zoom", () => Zoom_Click());

            AddMenuItem("Horizontal scale/Draw/None", () => dataSource.Settings.DrawScalePosHorizontal = DrawPosHorizontal.None);
            AddMenuItem("Horizontal scale/Draw/Top", () => dataSource.Settings.DrawScalePosHorizontal = DrawPosHorizontal.Top);
            AddMenuItem("Horizontal scale/Draw/Bottom", () => dataSource.Settings.DrawScalePosHorizontal = DrawPosHorizontal.Bottom);
            AddMenuItem("Horizontal scale/Fit", () => FitHorizontalInXDivs(dataSource.Settings.HorizontalDivisions));
            AddMenuItem("Horizontal scale/Day", () => AutoScaleHorizontalTime(TimeSpan.FromDays(1)));
            AddMenuItem("Horizontal scale/Hour", () => AutoScaleHorizontalTime(TimeSpan.FromHours(1)));

            AddMenuItem("Vertical scale/Draw/None", () => dataSource.Settings.DrawScalePosVertical = DrawPosVertical.None);
            AddMenuItem("Vertical scale/Draw/Left", () => dataSource.Settings.DrawScalePosVertical = DrawPosVertical.Left);
            AddMenuItem("Vertical scale/Draw/Right", () => dataSource.Settings.DrawScalePosVertical = DrawPosVertical.Right);
            AddMenuItem("Vertical scale/Zero position/Top", () => dataSource.Settings.ZeroPosition = VerticalZeroPosition.Top);
            AddMenuItem("Vertical scale/Zero position/Middle", () => dataSource.Settings.ZeroPosition = VerticalZeroPosition.Middle);
            AddMenuItem("Vertical scale/Zero position/Bottom", () => dataSource.Settings.ZeroPosition = VerticalZeroPosition.Bottom);
            AddMenuItem("Vertical scale/Auto", () => AutoScaleTracesKeepZero());

            AddMenuItem("Clear", () => dataSource.Clear());
            AddMenuItem("Screenshot/To clipboard", () => Screenshot_Click(true));
            AddMenuItem("Screenshot/To file", () => Screenshot_Click(false));
        }


        void AddMenuItem(string menuPath, Action action)
        {
            string[] split = menuPath.Split('/');

            ToolStripMenuItem item = null;


            if (menu.Items[split[0]] is ToolStripMenuItem tsi)
                item = tsi;
            else
            {
                item = new ToolStripMenuItem(split[0]);
                item.Name = split[0];
                menu.Items.Add(item);
            }

            for (int i = 1; i < split.Length; i++)
            {
                string name = split[i];

                if (item.DropDownItems[name] is ToolStripMenuItem tsii)
                    item = tsii;
                else
                {
                    ToolStripMenuItem newItem = new ToolStripMenuItem(name);
                    newItem.Name = name;
                    item.DropDownItems.Add(newItem);
                    item = newItem;
                }

            }

            if (action != null)
                item.Click += (a, b) => action.Invoke();


        }


        private void Screenshot_Click(bool toClipboard)
        {
            Image img = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            using (Graphics g = Graphics.FromImage(img))
            {
                DrawBackground(g);
                DrawData(g);
                DrawForeground(g);
            }

            if (toClipboard)
            {
                Clipboard.SetImage(img);
            }
            else
            {
                SaveFileDialog diag = new SaveFileDialog();
                diag.Filter = "PNG|*.PNG";
                diag.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                diag.FileName = "Untitled.png";
                diag.RestoreDirectory = true;
                if (diag.ShowDialog() == DialogResult.OK)
                {
                    img.Save(diag.FileName);
                }
            }
        }

        private void Zoom_Click()
        {
            if (DataSource.Cursors.Count == 0)
                return;

            Cursor left = null;
            Cursor right = null;

            GetMarkersAdjecentToX(lastClick.X, ref left, ref right);

            double x1 = 0;
            double x2 = 0;

            if (left != null)
                x1 = left.X;
            else
            {
                x1 = (from trace in DataSource.Traces
                      from pt in trace.Points
                      orderby pt.X ascending
                      select pt.X).FirstOrDefault();
            }

            if (right != null)
                x2 = right.X;
            else
            {
                x2 = (from trace in DataSource.Traces
                      from pt in trace.Points
                      orderby pt.X descending
                      select pt.X).FirstOrDefault();
            }

            dataSource.Settings.HorOffset = -x1;
            dataSource.Settings.HorScale = (x2 - x1) / dataSource.Settings.HorizontalDivisions;
            DrawAll();
        }

        void GetMarkersAdjecentToX(double xPos, ref Cursor left, ref Cursor right)
        {
            double pxPerUnits_hor = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
            double x = (xPos * dataSource.Settings.HorScale * dataSource.Settings.HorizontalDivisions / viewPort.Height) - dataSource.Settings.HorOffset + viewPort.X;

            int iLeft = -1;
            int iRight = -1;

            for (int i = 0; i < DataSource.Cursors.Count; i++)
            {
                if (DataSource.Cursors[i].X < x)
                {
                    if (iLeft == -1)
                        iLeft = i;
                    else
                    {
                        if (DataSource.Cursors[i].X > DataSource.Cursors[iLeft].X)
                            iLeft = i;
                    }
                }

                if (DataSource.Cursors[i].X > x)
                {
                    if (iRight == -1)
                        iRight = i;
                    else
                    {
                        if (DataSource.Cursors[i].X < DataSource.Cursors[iRight].X)
                            iRight = i;
                    }
                }
            }

            if (iLeft == -1)
                left = null;
            else
                left = DataSource.Cursors[iLeft];

            if (iRight == -1)
                right = null;
            else
                right = DataSource.Cursors[iRight];
        }

        private void ScopeView_Load(object sender, EventArgs e)
        {
            this.Resize += Form_ResizeEnd;

            DrawAll();


            pictureBox3.MouseClick += picBox_MouseClick;
            pictureBox3.MouseMove += picBox_MouseMove;
            pictureBox3.MouseDown += picBox_MouseDown;
            pictureBox3.MouseUp += picBox_MouseUp;
            pictureBox3.MouseWheel += PictureBox3_MouseWheel;

            //pictureBox1.Resize += PictureBox1_Resize;
        }

        private void PictureBox3_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                double scroll = (double)(e.Delta);
                double A = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
                double B = dataSource.Settings.HorOffset;
                double percent = (double)(e.X - viewPort.X) / (double)viewPort.Width;   //Relative mouse position.
                double x1px = percent * scroll;
                double x2px = viewPort.Width - (1 - percent) * scroll;

                //Find the actual value of x1 and x2
                double x1 = x1px / A - B;
                double x2 = x2px / A - B;
                double distance = x2 - x1;
                if (distance == 0)
                {
                    dataSource.Settings.HorScale = 1;
                    dataSource.Settings.HorOffset = -x1;
                    return;
                }
                dataSource.Settings.NotifyOnChange = false;
                dataSource.Settings.HorScale = (double)distance / (double)dataSource.Settings.HorizontalDivisions;
                dataSource.Settings.NotifyOnChange = true;
                dataSource.Settings.HorOffset = -(double)(x1);
            }
        }

        private void picBox_MouseUp(object sender, MouseEventArgs e)
        {
            dragMarker = null;
            lastClickDown = Point.Empty;
        }

        private void picBox_MouseDown(object sender, MouseEventArgs e)
        {
            dragMarker = hoverMarker;
            lastClickDown = e.Location;
            horOffsetLastClick = dataSource.Settings.HorOffset;
        }

        private void picBox_MouseClick(object sender, MouseEventArgs e)
        {
            lastClick = e.Location;
            if (e.Button == MouseButtons.Right)
            {
                if (dragMarker != null)
                    dragMarker = null;
                else
                    menu.Show(this, e.Location);
            }
        }

        private void picBox_MouseMove(object sender, MouseEventArgs e)
        {

            double pxPerUnits_hor = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
            if (dragMarker != null)
            {
                //Drag a marker.
                double x = ((e.X - viewPort.X) / pxPerUnits_hor) - dataSource.Settings.HorOffset;
                dragMarker.X = x;
                DrawForeground();
            }
            else
            {
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    //Drag all.
                    if (!lastClickDown.IsEmpty)
                    {
                        double xDif = e.X - lastClickDown.X;
                        double A = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
                        double offset = xDif / A + horOffsetLastClick;
                        dataSource.Settings.HorOffset = Math.Round(offset / dataSource.Settings.HorSnapSize) * dataSource.Settings.HorSnapSize;
                    }
                }
                else
                {
                    //Detect markers.
                    if (DataSource != null)
                    {
                        System.Windows.Forms.Cursor cur = System.Windows.Forms.Cursors.Default;
                        hoverMarker = null;
                        for (int i = 0; i < DataSource.Cursors.Count; i++)
                        {
                            Cursor cursor = DataSource.Cursors[i];
                            int cursorX = (int)((cursor.X + dataSource.Settings.HorOffset) * pxPerUnits_hor) + viewPort.X;
                            int xMin = cursorX - 4;
                            int xMax = cursorX + 4;


                            if (e.X > xMin && e.X < xMax)
                            {
                                cur = Cursors.VSplit;
                                hoverMarker = DataSource.Cursors[i];
                            }
                        }
                        System.Windows.Forms.Cursor.Current = cur;
                    }
                }
            }
        }



        #region Calculations

        public void AutoScaleTraces()
        {
            foreach (Trace t in dataSource.Traces)
                AutoScaleTrace(t);
        }

        public void AutoScaleTrace(Trace t)
        {
            if (double.IsNaN(t.Maximum.Y) || double.IsNaN(t.Maximum.X))
            {
                t.Scale = 1f;
                t.Offset = 0f;
                return;
            }

            double distance = t.Maximum.Y - t.Minimum.Y;
            double div = distance / ((double)dataSource.Settings.VerticalDivisions);
            double multiplier = 1f;

            while (div > 10)
            {
                multiplier *= 10;
                div /= 10;
            }

            while (div < 0.5)
            {
                multiplier /= 10;
                div *= 10;
            }


            if (div <= 1)
                t.Scale = (double)(1 * multiplier);
            else if (div <= 2)
                t.Scale = (double)(2 * multiplier);
            else if (div <= 5)
                t.Scale = (double)(5 * multiplier);
            else
                t.Scale = (double)(10 * multiplier);

            t.Offset = -(double)(distance / (dataSource.Settings.ZeroPosition == VerticalZeroPosition.Middle ? 2 : 1) + t.Minimum.Y);
        }

        public void AutoScaleTracesKeepZero()
        {
            foreach (Trace t in dataSource.Traces)
                AutoScaleTraceKeepZero(t);
        }

        public void AutoScaleTraceKeepZero(Trace t)
        {
            if (double.IsNaN(t.Maximum.Y) || double.IsNaN(t.Maximum.X))
            {
                t.Scale = 1f;
                t.Offset = 0f;
                return;
            }

            double distance = Math.Max(Math.Abs(t.Maximum.Y), Math.Abs(t.Minimum.Y));
            double div = distance * (dataSource.Settings.ZeroPosition == VerticalZeroPosition.Middle ? 2 : 1) / ((double)dataSource.Settings.VerticalDivisions);
            double multiplier = 1f;

            if (div == 0)
                return;

            while (div > 10)
            {
                multiplier *= 10;
                div /= 10;
            }

            while (div < 0.5)
            {
                multiplier /= 10;
                div *= 10;
            }


            if (div <= 1)
                t.Scale = (double)(1 * multiplier);
            else if (div <= 2)
                t.Scale = (double)(2 * multiplier);
            else if (div <= 5)
                t.Scale = (double)(5 * multiplier);
            else
                t.Scale = (double)(10 * multiplier);

            t.Offset = 0;
        }

        public void AutoScaleHorizontalTime(TimeSpan scale)
        {
            PointD min = PointD.Empty;
            PointD max = PointD.Empty;
            foreach (Trace t in DataSource.Traces)
            {
                min.KeepMinimum(t.Minimum);
                max.KeepMaximum(t.Maximum);
            }

            dataSource.Settings.HorScale = scale.Ticks;
            dataSource.Settings.HorOffset = -min.X;
        }

        public void SetHorizontalTime(DateTime start, DateTime end)
        {
            TimeSpan scale = end - start;
            dataSource.Settings.HorScale = scale.Ticks / dataSource.Settings.HorizontalDivisions;
            dataSource.Settings.HorOffset = -start.Ticks;
        }

        public void AutoScaleHorizontalTime()
        {
            PointD min = PointD.Empty;
            PointD max = PointD.Empty;

            foreach (Trace t in DataSource.Traces)
            {
                min.KeepMinimum(t.Minimum);
                max.KeepMaximum(t.Maximum);
            }


            DateTime start = new DateTime((long)min.X);
            DateTime end = new DateTime((long)max.X);
            TimeSpan span = end - start;
            if (span.TotalDays >= dataSource.Settings.HorizontalDivisions)
            {
                dataSource.Settings.HorScale = Math.Ceiling(span.TotalDays / dataSource.Settings.HorizontalDivisions) * TimeSpan.TicksPerDay;
                start.AddMilliseconds(-start.Millisecond);
                start.AddSeconds(-start.Second);
                start.AddMinutes(-start.Minute);
                start.AddHours(-start.Hour);
            }
            else if (span.TotalHours >= dataSource.Settings.HorizontalDivisions)
            {
                dataSource.Settings.HorScale = Math.Ceiling(span.TotalHours / dataSource.Settings.HorizontalDivisions) * TimeSpan.TicksPerHour;
                start.AddMilliseconds(-start.Millisecond);
                start.AddSeconds(-start.Second);
                start.AddMinutes(-start.Minute);
            }
            else if (span.TotalMinutes >= dataSource.Settings.HorizontalDivisions)
            {
                dataSource.Settings.HorScale = Math.Ceiling(span.TotalMinutes / dataSource.Settings.HorizontalDivisions) * TimeSpan.TicksPerMinute;
                start.AddMilliseconds(-start.Millisecond);
                start.AddSeconds(-start.Second);
            }
            else if (span.TotalSeconds >= dataSource.Settings.HorizontalDivisions)
            {
                dataSource.Settings.HorScale = Math.Ceiling(span.TotalSeconds / dataSource.Settings.HorizontalDivisions) * TimeSpan.TicksPerSecond;
                start.AddMilliseconds(-start.Millisecond);
                start.AddSeconds(-start.Second);
            }
            else
            {
                dataSource.Settings.HorScale = Math.Ceiling(span.TotalMilliseconds / dataSource.Settings.HorizontalDivisions) * TimeSpan.TicksPerMillisecond;
                start.AddMilliseconds(-start.Millisecond);
            }
            dataSource.Settings.HorOffset = -start.Ticks;
        }




        public void AutoScaleHorizontal()
        {
            PointD min = PointD.Empty;
            PointD max = PointD.Empty;

            foreach (Trace t in DataSource.Traces)
            {
                min.KeepMinimum(t.Minimum);
                max.KeepMaximum(t.Maximum);
            }

            double distance = max.X - min.X;
            if (distance == 0)
            {
                dataSource.Settings.HorScale = 1;
                dataSource.Settings.HorOffset = -min.X;
                return;
            }

            double div = distance / ((double)dataSource.Settings.HorizontalDivisions);
            double multiplier = 1f;

            while (div > 10)
            {
                multiplier *= 10;
                div /= 10;
            }

            while (div < 0.5)
            {
                multiplier /= 10;
                div *= 10;
            }


            if (div <= 1)
                dataSource.Settings.HorScale = (double)(1 * multiplier);
            else if (div <= 2)
                dataSource.Settings.HorScale = (double)(2 * multiplier);
            else if (div <= 5)
                dataSource.Settings.HorScale = (double)(5 * multiplier);
            else
                dataSource.Settings.HorScale = (double)(10 * multiplier);

            dataSource.Settings.HorOffset = -(double)(min.X);
        }

        public void FitHorizontalInXDivs(int divs)
        {
            PointD min = PointD.Empty;
            PointD max = PointD.Empty;

            foreach (Trace t in DataSource.Traces)
            {
                min.KeepMinimum(t.Minimum);
                max.KeepMaximum(t.Maximum);
            }

            double distance = max.X - min.X;
            if (distance == 0)
            {
                dataSource.Settings.HorScale = 1;
                dataSource.Settings.HorOffset = -min.X;
                return;
            }

            dataSource.Settings.HorScale = (double)distance / (double)divs;
            dataSource.Settings.HorOffset = -(double)(min.X);
        }



        #endregion

        #region Drawing

        private void DrawBackground()
        {
            pictureBox1.Refresh();
        }

        private void DrawBackground(Graphics g)
        {
            viewPort.X = 0;
            viewPort.Y = 0;
            viewPort.Width = pictureBox1.Width - 1;
            viewPort.Height = pictureBox1.Height - 1;

            int spaceForScaleIndicatorsVertical = 45;
            int spaceForScaleIndicatorsHorizontal = 25;

            if (dataSource == null)
                return;

            switch (dataSource.Settings.DrawScalePosVertical)
            {
                case DrawPosVertical.Left:
                    viewPort.X += spaceForScaleIndicatorsVertical;
                    viewPort.Width -= spaceForScaleIndicatorsVertical;
                    break;
                case DrawPosVertical.Right:
                    viewPort.Width -= spaceForScaleIndicatorsVertical;
                    break;
            }

            switch (dataSource.Settings.DrawScalePosHorizontal)
            {
                case DrawPosHorizontal.Bottom:
                    viewPort.Height -= spaceForScaleIndicatorsHorizontal;
                    break;
                case DrawPosHorizontal.Top:
                    viewPort.Y += spaceForScaleIndicatorsHorizontal;
                    viewPort.Height -= spaceForScaleIndicatorsHorizontal;
                    break;
            }


            int columns = dataSource.Settings.HorizontalDivisions;
            int rows = dataSource.Settings.VerticalDivisions;
            int pxPerColumn = viewPort.Width / columns;
            int pxPerRow = viewPort.Height / rows;
            int restWidth = viewPort.Width % columns;
            int restHeight = viewPort.Height % rows;

            spaceForScaleIndicatorsVertical += restWidth / 2;
            spaceForScaleIndicatorsHorizontal += restHeight / 2;

            viewPort.X += restWidth / 2;
            viewPort.Y -= restHeight / 2;
            viewPort.Width = columns * pxPerColumn;
            viewPort.Height = rows * pxPerRow;

            switch (dataSource.Settings.ZeroPosition)
            {
                case VerticalZeroPosition.Middle:
                    zeroPos = viewPort.Y + viewPort.Height / 2;
                    break;
                case VerticalZeroPosition.Top:
                    zeroPos = viewPort.Y;
                    break;
                case VerticalZeroPosition.Bottom:
                    zeroPos = viewPort.Y + viewPort.Height;
                    break;
            }

            g.Clear(dataSource.Settings.BackgroundColor);

            //Draw the viewport
            g.DrawRectangle(dataSource.Settings.GridPen, viewPort);

            //Draw the horizontal lines
            for (int row = 1; row < rows + 0; row++)
            {
                int y = (int)(row * pxPerRow) + viewPort.Y;
                g.DrawLine(dataSource.Settings.GridPen, viewPort.X, y, viewPort.X + viewPort.Width, y);

                if (dataSource != null)
                {
                    if (dataSource.Settings.DrawScalePosVertical != DrawPosVertical.None)
                    {
                        int scaleDrawCount = dataSource.Traces.Where(a => a.DrawOption.HasFlag(Trace.DrawOptions.ShowScale)).Count();
                        int fit = pxPerRow / dataSource.Settings.Font.Height;
                        if (fit > scaleDrawCount)
                            fit = scaleDrawCount;
                        int yy = y - (fit / 2) * dataSource.Settings.Font.Height;
                        if (fit % 2 != 0)
                            yy -= dataSource.Settings.Font.Height / 2;

                        int i = 0;

                        foreach (Trace t in dataSource.Traces)
                        {

                            if (i < dataSource.Traces.Count
                                && (i) < fit
                                && t.DrawOption.HasFlag(Trace.DrawOptions.ShowScale)
                                && t.Visible)
                            {

                                double yValue = ((dataSource.Settings.VerticalDivisions - row) * t.Scale) - t.Offset;
                                switch (dataSource.Settings.ZeroPosition)
                                {
                                    case VerticalZeroPosition.Middle:
                                        yValue -= (dataSource.Settings.VerticalDivisions / 2) * t.Scale;
                                        break;
                                    case VerticalZeroPosition.Top:
                                        yValue -= (dataSource.Settings.VerticalDivisions) * t.Scale;
                                        break;
                                }

                                Brush b = new SolidBrush(t.Pen.Color);
                                int x = dataSource.Settings.DrawScalePosVertical == DrawPosVertical.Left ? 0 : viewPort.X + viewPort.Width;
                                g.DrawString(t.ToHumanReadable(yValue), dataSource.Settings.Font, b, new Rectangle(x, yy, spaceForScaleIndicatorsVertical, dataSource.Settings.Font.Height));
                                yy += dataSource.Settings.Font.Height;
                                i++;
                            }

                            else
                                break;
                        }
                    }
                }
            }

            //Draw the vertical lines
            for (int i = 1; i < columns + 0; i++)
            {
                int x = (int)(i * pxPerColumn) + viewPort.X;
                g.DrawLine(dataSource.Settings.GridPen, x, viewPort.Y, x, viewPort.Y + viewPort.Height);

                if (dataSource != null)
                {
                    if (dataSource.Settings.DrawScalePosHorizontal != DrawPosHorizontal.None)
                    {
                        double pxPerUnits_hor = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
                        double xVal = ((x - viewPort.X) / pxPerUnits_hor) - dataSource.Settings.HorOffset;
                        string xString = dataSource.Settings.HorizontalToHumanReadable(xVal);

                        if (xString != null)
                        {
                            Brush b = new SolidBrush(dataSource.Settings.GridZeroPen.Color);
                            int y = dataSource.Settings.DrawScalePosHorizontal == DrawPosHorizontal.Top ? 0 : viewPort.Y + viewPort.Height + 2;
                            StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center };
                            Size textSize = TextRenderer.MeasureText(xString, dataSource.Settings.Font);
                            g.DrawString(xString, dataSource.Settings.Font, b, new Rectangle(x - pxPerColumn / 2, y, pxPerColumn, spaceForScaleIndicatorsHorizontal), sf);
                        }
                    }
                }
            }

            //Draw the zero line
            g.DrawLine(dataSource.Settings.GridZeroPen, viewPort.X, zeroPos, viewPort.X + viewPort.Width, zeroPos);
        }

        private void DrawData()
        {
            pictureBox2.Refresh();
        }

        private void DrawData(Graphics g)
        {
            if (DataSource == null)
            {
                g.DrawString("No datasource bound", DefaultFont, Brushes.White, new Point(this.Width / 2 - 50, this.Height / 2));
            }
            else
            {
                int errNo = 0;
                Brush errBrush = new SolidBrush(Color.Red);
                double pxPerUnits_hor = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale);
                var sortedTraces = from trace in DataSource.Traces
                                   orderby trace.Layer descending
                                   select trace;

                PointD min = PointD.Empty;
                PointD max = PointD.Empty;

                foreach (Trace t in sortedTraces.Where(t => t.Visible))
                {
                    min.KeepMinimum(t.Minimum);
                    max.KeepMaximum(t.Maximum);
                }

                double xLeft = dataSource.Settings.HorOffset;
                double xRight = ((viewPort.Width) / pxPerUnits_hor) - dataSource.Settings.HorOffset;

                //Loop through traces
                foreach (Trace trace in sortedTraces)
                {
                    double pxPerUnits_ver = viewPort.Height / (dataSource.Settings.VerticalDivisions * trace.Scale);

                    Func<PointD, Point> convert = (p) => new Point(
                        (int)((p.X + dataSource.Settings.HorOffset) * pxPerUnits_hor) + viewPort.X,
                        (int)(zeroPos - (p.Y + trace.Offset) * pxPerUnits_ver));
                    try
                    {
                        trace.Draw(g, viewPort, convert, min.X, max.X, xLeft, xRight, dataSource.Settings.Font);
                    }
                    catch (Exception ex)
                    {
                        g.DrawString(ex.Message, dataSource.Settings.Font, errBrush, new Point(0, (errNo++) * dataSource.Settings.Font.Height));
                    }
                }

                try
                {
                    foreach (Marker marker in dataSource.Markers)
                    {
                        if (marker is LinkedMarker lm)
                        {
                            if (dataSource.Traces.Contains(lm.Trace))
                            {
                                double pxPerUnits_ver = viewPort.Height / (dataSource.Settings.VerticalDivisions * lm.Trace.Scale);
                                Func<PointD, Point> convert = (p) => new Point(
                                    (int)((p.X + dataSource.Settings.HorOffset) * pxPerUnits_hor) + viewPort.X,
                                    (int)(zeroPos - (p.Y + lm.Trace.Offset) * pxPerUnits_ver));

                                marker.Draw(g, viewPort, convert, dataSource.Settings.Font);
                            }
                        }
                        else if (marker is FreeMarker fm)
                        {
                            double pxPerUnits_ver = viewPort.Height / (dataSource.Settings.VerticalDivisions * 1);
                            Func<PointD, Point> convert = (p) => new Point(
                                (int)((p.X + dataSource.Settings.HorOffset) * pxPerUnits_hor) + viewPort.X,
                                (int)(zeroPos - (p.Y + 0) * pxPerUnits_ver));

                            marker.Draw(g, viewPort, convert, dataSource.Settings.Font);
                        }

                    }
                }
                catch (Exception ex)
                {
                    g.DrawString(ex.Message, dataSource.Settings.Font, errBrush, new Point(0, (errNo++) * dataSource.Settings.Font.Height));
                }

                try
                {
                    foreach (IScopeDrawable drawable in dataSource.Drawables)
                    {
                        Func<PointD, Point> convert = (p) => new Point((int)((p.X + dataSource.Settings.HorOffset) * pxPerUnits_hor), (int)(viewPort.Height / 2 - p.Y));
                        drawable.Draw(g, convert);

                    }
                }
                catch (Exception ex)
                {
                    g.DrawString(ex.Message, dataSource.Settings.Font, errBrush, new Point(0, (errNo++) * dataSource.Settings.Font.Height));
                }
            }
        }

        private void DrawForeground()
        {
            pictureBox3.Refresh();
        }
        void DrawForeground(Graphics g)
        {
            if (DataSource != null)
            {
                double pxPerUnits_hor = viewPort.Width / (dataSource.Settings.HorizontalDivisions * dataSource.Settings.HorScale); // hPxPerSub * grid.Horizontal.SubDivs / (HorUnitsPerDivision /** grid.Horizontal.Divisions*/);

                int markerNo = 0;
                //Loop through markers
                foreach (Cursor marker in DataSource.Cursors)  // (int traceIndex = 0; traceIndex < Scope.Traces.Count; traceIndex++)
                {
                    Pen pen = marker.Pen;
                    Brush brush = new SolidBrush(pen.Color);

                    try
                    {
                        float x = (float)((marker.X + dataSource.Settings.HorOffset) * pxPerUnits_hor) + viewPort.X;
                        g.DrawLine(pen, x, viewPort.Y, x, viewPort.Y + viewPort.Height);
                        g.DrawString(marker.ID.ToString(), dataSource.Settings.Font, brush, new PointF(x, 0));

                    }

                    catch (Exception ex)
                    {
                        g.DrawString(ex.Message, dataSource.Settings.Font, brush, new Point(0, markerNo * dataSource.Settings.Font.Height));
                    }
                    markerNo++;
                }

                //Func<double, int> scaleX = (x) => (int)((x + dataSource.Settings.HorOffset) * pxPerUnits_hor);
                //Loop trought mathitems
                //@TODO
                //foreach (MathItem mathItem in DataSource.MathItems)  // (int traceIndex = 0; traceIndex < Scope.Traces.Count; traceIndex++)
                //{
                //    try
                //    {
                //        if (mathItem.Trace != null)
                //        {
                //            double pxPerUnits_ver = viewPort.Height / (Settings.VerticalDivisions * mathItem.Trace.Scale);
                //            Func<double, int> scaleY = (x) => (int)(zeroPos - (x + mathItem.Trace.Offset) * pxPerUnits_ver);
                //
                //            mathItem.Draw(g, scaleY, scaleX);
                //        }
                //
                //    }
                //    catch (Exception ex)
                //    {
                //        g.DrawString(ex.Message, dataSource.Settings.Font, Brushes.White, new Point(0, markerNo * dataSource.Settings.Font.Height));
                //    }
                //}
            }
        }

        #endregion


        private void DrawAll()
        {
            DrawBackground();
            DrawData();
            DrawForeground();
        }

        //Draw the background
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            DrawBackground(g);
        }

        private void PictureBox2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            DrawData(g);
        }

        private void PictureBox3_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            DrawForeground(g);
        }

        private void Markers_ListChanged(object sender, ListChangedEventArgs e)
        {
            DrawForeground();
        }

        private void Traces_ListChanged(object sender, ListChangedEventArgs e)
        {
            DrawData();
        }

        private void Form_ResizeEnd(object sender, EventArgs e)
        {
            DrawAll();
        }
    }
}
