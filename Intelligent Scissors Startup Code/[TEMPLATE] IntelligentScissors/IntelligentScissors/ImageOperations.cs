using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System;
using System.IO;
///Algorithms Project
///Intelligent Scissors
///

namespace IntelligentScissors
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }

    /// <summary>
    /// Holds the edge energy between 
    ///     1. a pixel and its right one (X)
    ///     2. a pixel and its bottom one (Y)
    /// </summary>
    public struct Vector2D
    {
        
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[0];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[2];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Calculate edge energy between
        ///     1. the given pixel and its right one (X)
        ///     2. the given pixel and its bottom one (Y)
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns>edge energy with the right pixel (X) and with the bottom pixel (Y)</returns>
        public static Vector2D CalculatePixelEnergies(int x, int y, RGBPixel[,] ImageMatrix)
        {
            if (ImageMatrix == null) throw new Exception("image is not set!");

            Vector2D gradient = CalculateGradientAtPixel(x, y, ImageMatrix);

            double gradientMagnitude = Math.Sqrt(gradient.X * gradient.X + gradient.Y * gradient.Y);
            double edgeAngle = Math.Atan2(gradient.Y, gradient.X);
            double rotatedEdgeAngle = edgeAngle + Math.PI / 2.0;

            Vector2D energy = new Vector2D();
            energy.X = Math.Abs(gradientMagnitude * Math.Cos(rotatedEdgeAngle));
            energy.Y = Math.Abs(gradientMagnitude * Math.Sin(rotatedEdgeAngle));

            return energy;
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[0] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[2] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }
            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;
            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }
            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }
            return Filtered;
        }
        #region Private Functions
        /// <summary>
        /// Calculate Gradient vector between the given pixel and its right and bottom ones
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns></returns>
        private static Vector2D CalculateGradientAtPixel(int x, int y, RGBPixel[,] ImageMatrix)
        {
            Vector2D gradient = new Vector2D();
            RGBPixel mainPixel = ImageMatrix[y, x];
            double pixelGrayVal = 0.21 * mainPixel.red + 0.72 * mainPixel.green + 0.07 * mainPixel.blue;
            if (y == GetHeight(ImageMatrix) - 1)
            {
                //boundary pixel.
                for (int i = 0; i < 3; i++)
                {
                    gradient.Y = 0;
                }
            }
            else
            {
                RGBPixel downPixel = ImageMatrix[y + 1, x];
                double downPixelGrayVal = 0.21 * downPixel.red + 0.72 * downPixel.green + 0.07 * downPixel.blue;
                gradient.Y = pixelGrayVal - downPixelGrayVal;
            }
            if (x == GetWidth(ImageMatrix) - 1)
            {
                //boundary pixel.
                gradient.X = 0;
            }
            else
            {
                RGBPixel rightPixel = ImageMatrix[y, x + 1];
                double rightPixelGrayVal = 0.21 * rightPixel.red + 0.72 * rightPixel.green + 0.07 * rightPixel.blue;

                gradient.X = pixelGrayVal - rightPixelGrayVal;
            }
            return gradient;
        }
        #endregion
        public static int get1D(int x , int y,int width)
        {
            return (y * width) + x;
        }
        public static Vector2D get2D(int Index, int width)
        {
            // y -> row ,  x -> column  
            Vector2D v = new Vector2D();
            v.X = (int)Index % (int)width;
            v.Y = (int)Index / width;
            return v;
        }

    }
    public class adj
    {

        int timeInSec;
        public List<List<KeyValuePair<double, int>>> graph;
        public void createGraph(RGBPixel[,] ImageMatrix, int width, int height)
        {
            if (graph != null)
                graph.Clear();
            graph = new List<List<KeyValuePair<double, int>>>();
            int PixelsNum = width * height, NumOfPixel;
            int bottom, right;
            for (int i = 0; i < PixelsNum; i++)
            {
                var list = new List<KeyValuePair<double, int>>();
                graph.Add(list);
            }
            var time = System.Diagnostics.Stopwatch.StartNew();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                  
                    NumOfPixel = ImageOperations.get1D(x,y,width);

                    Vector2D w = ImageOperations.CalculatePixelEnergies(x, y, ImageMatrix);
                    double val = 0;
                    if (y != height - 1)
                    {
                        bottom = ImageOperations.get1D(x, y+1, width);
                        if (w.Y == 0)
                            val = 1E+16;
                        else
                            val = 1/w.Y;
                        graph[NumOfPixel].Add(new KeyValuePair<double, int>(val, bottom));
                        graph[bottom].Add(new KeyValuePair<double, int>(val, NumOfPixel));
                    }
                    if (x != width - 1)
                    {
                        right = ImageOperations.get1D(x+1, y, width);
                        if (w.X == 0)
                            val = 1E+16;
                        else
                            val = 1 / w.X;
                        graph[NumOfPixel].Add(new KeyValuePair<double, int>(val, right));
                        graph[right].Add(new KeyValuePair<double, int>(val, NumOfPixel));
                    }
                }
            }
            time.Stop();
            timeInSec =(int) time.ElapsedMilliseconds/1000;
            // return graph;
        }
        public void printSampleOutputGraph(string file)
        {
            if (File.Exists(file))
            {
                File.WriteAllText(file, string.Empty);

                using (StreamWriter sw = File.CreateText(file))
                {
                    sw.WriteLine("The constructed graph");
                    sw.WriteLine("");

                    for (int i = 0; i < graph.Count; i++)
                    {
                        sw.WriteLine("The index node : " + i);
                        sw.WriteLine("Edges");
                        for (int j = 0; j < graph[i].Count; j++)
                        {
                            sw.WriteLine("edge from  " + i+" to "+ graph[i][j].Value + "  With Weights " + graph[i][j].Key);
                        }
                        sw.WriteLine("");
                        sw.WriteLine("");
                    }
                }
            }
        }
        public void printCompleteOutputGraph(string file)
        {
            if (File.Exists(file))
            {
                File.WriteAllText(file, string.Empty);
                
                using (StreamWriter sw = File.CreateText(file))
                {
                    sw.WriteLine("The constructed graph");
                    sw.WriteLine("");
                    string s;
                    for (int i = 0; i < graph.Count; i++)
                    {
                        s = i+ "| edges : ";
                        for (int j = 0; j < graph[i].Count; j++)
                        {
                            s += " (" +i+",";
                            s += graph[i][j].Value+","+ graph[i][j].Key+") ";
                        }
                       
                        sw.WriteLine(s);
                    }
                    sw.WriteLine("");
                    sw.WriteLine("the graph constructed in time :" + timeInSec.ToString());

                    
                }
            }
        }

    }
}
class PriorityQueue
{
    public List<KeyValuePair<double, int>> list;
    public int Count
    {
        get
        {
            return list.Count;
        }
    }
    public PriorityQueue()
    {
        list = new List<KeyValuePair<double, int>>();
    }
    public void Enqueue(double x, int vertex)
    {
        list.Add(new KeyValuePair<double, int>(x, vertex));

        int p = 0;

        for (int i = Count - 1; i > 0;)
        {
            int pos = (i - 1) / 2;
            if (list[pos].Key <= x)
            {
                p = i;
                break;

            }
            list[i] = list[pos];
            i = pos;
        }

        if (Count > 0)
        {
            KeyValuePair<double, int> temp = new KeyValuePair<double, int>(x, vertex);
            list[p] = temp;
        }
    }

    public KeyValuePair<double, int> Dequeue()
    {
        KeyValuePair<double, int> min = Top();
        double top = list[Count - 1].Key;
        int ver = list[Count - 1].Value;
        list.RemoveAt(Count - 1);

        int i = 0;
        while (i * 2 < Count - 2)
        {
            int a = i * 2 + 1;
            int b = i * 2 + 2;
            int c = 0;
            if (b < Count && list[b].Key < list[a].Key)
                c = b;
            else
                c = a;

            if (list[c].Key >= top)
            {
                break;
            }
            list[i] = list[c];
            i = c;
        }
        if (Count > 0)
        {
            list[i] = new KeyValuePair<double, int>(top, ver);
        }

        return new KeyValuePair<double, int>(min.Key, min.Value);
    }

    public KeyValuePair<double, int> Top()
    {
        return new KeyValuePair<double, int>(list[0].Key, list[0].Value);
    }

    public void Clear()
    {
        list.Clear();
    }
}
class Stack
{
    private int[] items = new int[100000 + 10];
    private int count;

    public void Push(int item)
    {
        if (count == items.Length)
            throw new Exception("");

        items[count] = item;
        count++;
    }

    public int Pop()
    {
        if (count == 0)
            throw new Exception();
        count -= 1;
        return items[count];
    }

    public int Top()
    {
        if (count == 0)
            throw new Exception();

        return items[count - 1];
    }

    public bool IsEmpty()
    {
        return count == 0;
    }
}
class dijkstra
{

    public List <double> dis = new List<double>();
    public List<int> parent = new List<int>();

    public dijkstra()
    {
        for (int i = 0; i < 14100000 ; i++)
        {
            dis.Add (double.MaxValue); // b7t infinite 3nd kol wa7da m3ada awl wa7da 
            parent .Add(-1);
        }
    }
    public void dij(int node, ref List<List<KeyValuePair<double, int>>> adj)
    {
        dis[node] = 0; // 3nd awl wa7da b7t 0
        PriorityQueue pq = new PriorityQueue();
        pq.Enqueue(0, node);
        while (pq.Count != 0)
        {
            //MessageBox.Show("asdasd");
            double w = pq.Top().Key;
            int nod = pq.Top().Value;
            pq.Dequeue();
            if (w > dis[nod])
              continue;
            foreach (KeyValuePair<double, int> i in adj[nod])
            {
                double child_w = i.Key;
                int child = i.Value;
                if (w + child_w < dis[child])
                {
                    //MessageBox.Show("heo");
                    dis[child] = w + child_w;
                    pq.Enqueue(dis[child], child);
                    parent[child] = nod;

                }
            }
        }
    }
    public void print_path(int node, ref List<int> shortest_paths)
    {
        
        int current = node;
        Stack st = new Stack();

        if (parent[current] == -1)
        {
            return;
        }
        while (current != -1)
        {

            st.Push(current);
            current = parent[current];
        }
        shortest_paths = new List<int>();
        while (st.IsEmpty() == false)
        {
            shortest_paths.Add(st.Top());
            st.Pop();

        }
        st = new Stack();
        
    }
    //public List<List<int>> getShortestPath(int anchorPoint, ref List<List<KeyValuePair<double, int>>> adj)
    //{


    //    List<List<int>> shortest_paths = new List<List<int>>();

    //    for (int j = 0; j <= adj.Count; j++)
    //        shortest_paths.Add(new List<int>());

    //    dij(anchorPoint, ref adj); // the last free point for each time  <-------------------should changed 
    //    for (int i = 0; i < adj.Count; i++)
    //    {
    //        print_path(i, ref shortest_paths);
    //        // if (shortest_paths[i].Count > 0)
    //        //MessageBox.Show(i+"  "+shortest_paths[i].Count.ToString());
    //    }
    //    return shortest_paths;
    //}
}