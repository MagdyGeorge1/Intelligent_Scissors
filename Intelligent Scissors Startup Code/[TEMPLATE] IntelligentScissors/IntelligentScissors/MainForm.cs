using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System;
using System.IO;
namespace IntelligentScissors
{
    public partial class MainForm : Form
    {
        List<int> shortestPath;
        int firstAnchor;
        bool clicked = false;
        int CountOfClicked = 0;
        int CurrentSelection;
        bool sampleTest = false;
        public MainForm()
        {
            InitializeComponent();
        }
        RGBPixel[,] ImageMatrix;
        RGBPixel[,] TempImageMatrix;
        public static List<List<KeyValuePair<double, int>>> graph;
        int width;
        int height;
        adj a = new adj();
        dijkstra d = new dijkstra();
        string file;
        string filepath;
        double pathTime=5;
        private void btnOpen_Click(object sender, EventArgs e)
        {
            string folderName = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string OpenedFilePath = openFileDialog1.FileName;
                if (OpenedFilePath.Contains("Sample"))
                    sampleTest = true;
                folderName = OpenedFilePath.Substring(0, OpenedFilePath.Length - 9);
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                TempImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                Array.Copy(ImageMatrix, TempImageMatrix, ImageMatrix.Length);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            width = ImageOperations.GetWidth(ImageMatrix);
            height = ImageOperations.GetHeight(ImageMatrix);
            file = folderName + "OurOutput.txt";
            filepath = folderName + "OurPath.txt";
            a.createGraph(ImageMatrix, width, height);
            if (sampleTest)
                a.printSampleOutputGraph(file);
            else
            {
                pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
                a.printCompleteOutputGraph(file);              
            }
            txtWidth.Text = width.ToString();
            txtHeight.Text = height.ToString();
        }
        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            //double sigma = double.Parse(txtGaussSigma.Text);
            //int maskSize = (int)nudMaskSize.Value;
            //ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            mouseLoc.Text = e.Location.ToString();
            pixelNum.Text = (ImageOperations.get1D(e.X,e.Y,width)).ToString();
            if (clicked && ImageMatrix != null)
            {
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                Array.Copy(ImageMatrix, TempImageMatrix, ImageMatrix.Length);
                System.Diagnostics.Stopwatch time =new System.Diagnostics.Stopwatch();
                if (CountOfClicked == 1)
                {
                    time.Start();
                }
                d.print_path(ImageOperations.get1D(e.X, e.Y, width), ref shortestPath);
                if (CountOfClicked == 1)
                {
                    pathTime =(double)time.ElapsedMilliseconds / 1000;
                }
                if (shortestPath == null)
                    return;
                for (int i = 0; i < shortestPath.Count; i++)
                {
                    int pix = shortestPath[i];
                    Vector2D v = ImageOperations.get2D(pix, width);
                    
                    TempImageMatrix[(int)v.Y, (int)v.X].blue = 0;
                    TempImageMatrix[(int)v.Y, (int)v.X].green = 255;
                    TempImageMatrix[(int)v.Y, (int)v.X].red = 0;
                }
                CurrentSelection = ImageOperations.get1D(e.X, e.Y, width);
                ImageOperations.DisplayImage(TempImageMatrix, pictureBox1);
            }
        }
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            int anchor = ImageOperations.get1D(e.X, e.Y, width);

            if (CountOfClicked > 0)
            {
                if (CountOfClicked == 1 && sampleTest)
                {
                    printpathSample(anchor);
                }
                else if (CountOfClicked == 1 && !sampleTest)
                {
                   printPathComplete(anchor,pathTime);
                }
                for (int i = 0; i < shortestPath.Count; i++)
                {
                    int pix = shortestPath[i];
                    Vector2D v = ImageOperations.get2D(pix, width);
                    ImageMatrix[(int)v.Y, (int)v.X].blue = 255;
                    ImageMatrix[(int)v.Y, (int)v.X].green = 0;
                    ImageMatrix[(int)v.Y, (int)v.X].red = 0;
                }
            }
            else if (CountOfClicked == 0)
            {
                firstAnchor = anchor;
            }
            d = new dijkstra();
            d.dij(anchor, ref a.graph);
            clicked = true;
            CountOfClicked++;
        }
        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void printpathSample(int anchor)
        {
            if (File.Exists(filepath))
            {
                File.WriteAllText(filepath, string.Empty);
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine("The shortest path from node : " + firstAnchor + " and Node : " + anchor);
                    sw.WriteLine("");
                    sw.WriteLine("");

                    for (int i = 0; i < shortestPath.Count; i++)
                    {
                        int node = shortestPath[i];
                        Vector2D v = ImageOperations.get2D(node, width);
                        sw.WriteLine("Node : " + node + " at postion X = " + v.X + "  , at position Y = " + v.Y);
                    }
                }

            }
        }
        private void printPathComplete(int anchor,double time)
        {
            if (File.Exists(filepath))
            {
                File.WriteAllText(filepath, string.Empty);
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    Vector2D v1 = ImageOperations.get2D(firstAnchor, width);
                    Vector2D v2 = ImageOperations.get2D(anchor, width);
                    sw.WriteLine("The shortest path from node : "+firstAnchor + " at(" +v1.X+","+v1.Y+") to Node : " + anchor + " at(" + v2.X + "," + v2.Y + ")");
                    sw.WriteLine("");
                    for (int i = 0; i < shortestPath.Count; i++)
                    {
                        int node = shortestPath[i];
                        Vector2D v = ImageOperations.get2D(node, width);
                        sw.WriteLine("({X=" +v.X+ ",Y =" +v.Y+ "},"+v.X+","+v.Y+")");
                    }
                    sw.WriteLine(time.ToString());
                }

            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            d.print_path(firstAnchor, ref shortestPath);
            for (int i = 0; i < shortestPath.Count; i++)
            {
                int pix = shortestPath[i];
                Vector2D v = ImageOperations.get2D(pix, width);
                ImageMatrix[(int)v.Y, (int)v.X].blue = 255;
                ImageMatrix[(int)v.Y, (int)v.X].green = 0;
                ImageMatrix[(int)v.Y, (int)v.X].red = 0;
            }

            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }
    }
}