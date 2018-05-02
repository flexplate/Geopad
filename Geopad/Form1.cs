using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Geopad
{
    public partial class Form1 : Form
    {
        private Point origin;
        private Point end;
        private bool drawing;
        private List<GeoJSON.Net.Geometry.Position> points;
        private List<Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>> lines;
        private List<Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>> polygons;
        private bool pointerOverPanel;
        private Point mouseLocation;

        public enum DrawMode
        {
            Point,
            Line,
            Polygon
        }

        public DrawMode DrawingMode { get; set; }

        public Form1()
        {
            InitializeComponent();

            points = new List<GeoJSON.Net.Geometry.Position>();
            lines = new List<Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>>();
            polygons = new List<Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>>();

            panel1.MouseEnter += Panel1_MouseEnter;
            panel1.MouseLeave += Panel1_MouseLeave;
            panel1.MouseDown += Panel1_MouseDown;
            panel1.MouseMove += Panel1_MouseMove;
            panel1.MouseUp += Panel1_MouseUp;
            panel1.Paint += Panel1_Paint;
        }

        private void Panel1_MouseLeave(object sender, EventArgs e)
        {
            pointerOverPanel = false;
        }

        private void Panel1_MouseEnter(object sender, EventArgs e)
        {
            pointerOverPanel = true;
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            foreach (var Pt in points) { e.Graphics.DrawEllipse(Pens.Blue, (float)Pt.Longitude, (float)Pt.Latitude, 10, 10); }
            foreach (var Ln in lines) { e.Graphics.DrawLine(Pens.Blue, (float)Ln.Item1.Longitude, (float)Ln.Item1.Latitude, (float)Ln.Item2.Longitude, (float)Ln.Item2.Latitude); }
            foreach (var Po in polygons) { e.Graphics.DrawRectangle(Pens.Blue, (int)Po.Item1.Longitude, (int)Po.Item1.Latitude, (int)(Po.Item2.Longitude - Po.Item1.Longitude), (int)(Po.Item2.Latitude - Po.Item1.Latitude)); }

            if (drawing)
            {
                switch (DrawingMode)
                {
                    case DrawMode.Point:
                        e.Graphics.DrawEllipse(Pens.Cyan, end.X, end.Y, 10, 10);
                        break;
                    case DrawMode.Line:
                        e.Graphics.DrawLine(Pens.Cyan, origin, end);
                        break;
                    case DrawMode.Polygon:
                        e.Graphics.DrawRectangle(Pens.Cyan, origin.X, origin.Y, end.X - origin.X, end.Y - origin.Y);
                        break;
                    default:
                        break;
                }
            }

            if (pointerOverPanel)
            {
                // Draw size tooltip
                var TooltipLabel = string.Format("{0}, {1}", mouseLocation.X, mouseLocation.Y);
                var GenericFont = new Font(FontFamily.GenericSansSerif, 10);
                SizeF LayoutSize = new SizeF(200.0F, 50.0F);
                SizeF TextSize = e.Graphics.MeasureString(TooltipLabel, GenericFont, LayoutSize);
                e.Graphics.DrawRectangle(Pens.Black, mouseLocation.X + 5, mouseLocation.Y + 5, TextSize.Width, TextSize.Height);
                e.Graphics.DrawString(TooltipLabel, GenericFont, Brushes.Black, new Point(mouseLocation.X + 6, mouseLocation.Y + 6));
            }
        }

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drawing = true;
                origin = e.Location;
                end = e.Location;
                switch (DrawingMode)
                {
                    case DrawMode.Line:
                        break;
                    case DrawMode.Polygon:
                        break;
                    default:
                        break;
                }
                panel1.Invalidate();
            }
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseLocation = e.Location;
            if (drawing)
            {
                end = e.Location;
            }
            panel1.Invalidate();
        }



        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            drawing = false;
            end = e.Location;
            switch (DrawingMode)
            {
                case DrawMode.Point:
                    points.Add(new GeoJSON.Net.Geometry.Position(end.Y, end.X));
                    break;
                case DrawMode.Line:
                    lines.Add(new Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>(new GeoJSON.Net.Geometry.Position(origin.Y, origin.X), new GeoJSON.Net.Geometry.Position(end.Y, end.X)));
                    break;
                case DrawMode.Polygon:
                    polygons.Add(new Tuple<GeoJSON.Net.Geometry.Position, GeoJSON.Net.Geometry.Position>(new GeoJSON.Net.Geometry.Position(origin.Y, origin.X), new GeoJSON.Net.Geometry.Position(end.Y, end.X)));                    
                    break;
                default:
                    break;
            }
            panel1.Invalidate();
        }

        private void pointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawingMode = DrawMode.Point;
        }

        private void lineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawingMode = DrawMode.Line;
        }

        private void polygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawingMode = DrawMode.Polygon;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var Features = new List<Feature>();
            foreach (var CurrentPoint in points)
            {
                var Geometry = new GeoJSON.Net.Geometry.Point(CurrentPoint);
                Features.Add(new Feature(Geometry));
            }
            foreach (var CurrentLine in lines)
            {
                var Geometry = new GeoJSON.Net.Geometry.LineString(new List<GeoJSON.Net.Geometry.Position> { CurrentLine.Item1, CurrentLine.Item2 });
                Features.Add(new Feature(Geometry));
            }
            foreach (var CurrentPoly in polygons)
            {
                var Poly = new List<List<List<double>>> {
                    new List<List<double>> {
                        new List<double> { CurrentPoly.Item1.Latitude, CurrentPoly.Item1.Longitude },
                        new List<double> { CurrentPoly.Item1.Latitude, CurrentPoly.Item2.Longitude },
                        new List<double> { CurrentPoly.Item2.Latitude, CurrentPoly.Item2.Longitude },
                        new List<double> { CurrentPoly.Item2.Latitude, CurrentPoly.Item1.Longitude },
                        new List<double> { CurrentPoly.Item1.Latitude, CurrentPoly.Item1.Longitude },
                    }
                };
                var Geometry = new GeoJSON.Net.Geometry.Polygon(Poly);
                Features.Add(new Feature(Geometry));
            }

            var FeatureCol = new FeatureCollection(Features);

            var SaveDialog = new SaveFileDialog();
            SaveDialog.Filter = "GeoJSON Files (*.json)|*.json";
            var Result = SaveDialog.ShowDialog();
            if (Result == DialogResult.OK)
            {
                Stream Stream;
                if ((Stream = SaveDialog.OpenFile()) != null)
                {
                    string Json = JsonConvert.SerializeObject(FeatureCol);
                    using (var Writer = new StreamWriter(Stream))
                    {
                        Writer.Write(Json);
                        Writer.Flush();
                    }
                    Stream.Close();
                }
            }

        }
    }
}
