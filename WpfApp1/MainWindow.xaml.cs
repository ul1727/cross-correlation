﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Windows.Interop;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using static System.Net.WebRequestMethods;
using System.Windows.Threading;
using OpenCvSharp;
//using Window = OpenCvSharp.Window;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;
using Rectangle = System.Windows.Shapes.Rectangle;
using Point = System.Windows.Point;
using System.Drawing.Imaging;
using System.Net;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    class Video : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private BitmapSource video_source;

        public BitmapSource Video_source
        {
            get { return video_source; }
            set
            {
                video_source = value;
                OnPropertyChanged("Video_source");
            }
        }

        private string video_name;
        public string Video_name
        {
            get { return video_name; }
            set
            {
                video_name = value;
                string[] photonames = new string[] { ".jpg", ".png", ".jpeg", ".tiff", ".gif", ".bmp", ".ico", ".webp", ".raw" };
                if (!string.IsNullOrEmpty(video_name) && System.IO.File.Exists(video_name) && Directory.Exists(System.IO.Path.GetDirectoryName(video_name)))
                { //1
                    if (photonames.Contains(System.IO.Path.GetExtension(value)))
                    {

                        BitmapImage X = new BitmapImage();
                        X.BeginInit();
                        X.UriSource = new Uri(value, UriKind.RelativeOrAbsolute);
                        X.EndInit();
                        Video_source = X;

                    }
                    else
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(value);
                        BitmapSource X1 = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle, System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        Video_source = X1;
                    }
                }
            }
        }

        private Dictionary<int, BitmapSource> list_of_frames;
        public Dictionary<int, BitmapSource> List_Of_Frames
        {
            get { return list_of_frames; }
            set
            {
                list_of_frames = value;
                OnPropertyChanged("List_Of_Frames");
            }
        }

        //public List<string> list_of_puctures;
        //public List<string> List_Of_Pictires
        //{
        //    get { return list_of_puctures; }
        //    set { list_of_puctures = value;
        //        OnPropertyChanged("List_Of_Pictires");
        //    }
        //}
    }



    public partial class MainWindow : System.Windows.Window
    {

        string Path = " ";
        int currentIndex = 0;
        string[] fileNames;
        double Sec = 0.05;
        private DispatcherTimer timer;
        int flag = 0;
        System.Windows.Point startPoint;
        bool selectFlag = false;
        Dictionary<int, BitmapSource> dic_image = new Dictionary<int, BitmapSource>();
        Dictionary<int, BitmapSource> dic_image2 = new Dictionary<int, BitmapSource>();
        double crop_im;

        Mat imageCv;
        Mat tempCv;
        double minVal, maxVal;
        OpenCvSharp.Point minLoc, maxLoc;
        Point TP;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new Video();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(Sec);
            //timer.Tick += Timer_Tick;
            timer.Start();
        }

        Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new System.Drawing.Rectangle(new System.Drawing.Point(0,0), bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static CroppedBitmap GetCroppedBitmap(BitmapSource src, double x, double y, double w, double h )
        {
            double factorX, factorY;

            factorX = src.PixelWidth / src.Width;
            factorY = src.PixelHeight / src.Height;
            //Console.WriteLine(factorY);
            return new CroppedBitmap(src, new Int32Rect((int)Math.Round(x * factorX), (int)Math.Round(y * factorY), (int)Math.Round(w * factorX), (int)Math.Round(h * factorY)));
        }

        public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            flag = 1;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            Path = fbd.SelectedPath;
            if (Path != "" && Path != null) fileNames = Directory.GetFiles(Path).ToArray();
            if (fileNames != null)
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    BitmapImage bitmapImage = new BitmapImage(new Uri(fileNames[i], UriKind.Absolute));
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                    // Преобразование BitmapImage в BitmapSource
                    BitmapSource bitmapSource = bitmapImage as BitmapSource;

                    if (!dic_image2.ContainsKey(i)) {
                        dic_image2.Add(i, bitmapSource);
                            }

                }

                (DataContext as Video).List_Of_Frames = dic_image2;
                (DataContext as Video).Video_source = dic_image2[0];
            }


        }
        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    if (flag == 1)
        //    {
        //        if (Path != "" && Path != null)
        //        {
        //            if (currentIndex < fileNames.Length)
        //            {
        //                (DataContext as Video).Video_name = fileNames[currentIndex];
        //                currentIndex++;
        //            }
        //            else
        //            {
        //                currentIndex = 0;
        //            }
        //        }
        //    }
        //    else if (flag == 2)
        //    {
        //        if (Path != "" && Path != null)
        //        {
        //            if (currentIndex < Frames.Count)
        //            {
        //                (DataContext as Video).Video_source = Frames[currentIndex];
        //                //Console.WriteLine($"{currentIndex}  " + Frames[currentIndex]);
        //                currentIndex++;
        //            }
        //            else
        //            {
        //                currentIndex = 0;
        //            }
        //        }

        //    }
        //}

        private void Button_Click_1(object sender, RoutedEventArgs e) //Преобразование видео в кадры. Библиотека OpenCvSharp
        {
            flag = 2;
            System.Windows.Forms.OpenFileDialog fbd = new System.Windows.Forms.OpenFileDialog(); //выбор файла. Есть в папке с проектом
            fbd.ShowDialog();
             Path = fbd.FileName;
            string videoFile=" ";
            if (Path != "" && Path != null) videoFile = Path;
            var capture = new VideoCapture(videoFile);
            //var window = new OpenCvSharp.Window("Video Frame by Frame");  //для вывода в окно
            var image = new Mat();
            //var dic_image = new Dictionary<int, Bitmap>(); // словарь с кадрами
            
            int i = 0;
            while (capture.IsOpened()) //Получение кадров
            {
                capture.Read(image);
                
                if (image.Empty()) break;
                /*
                if (i == 0)
                {
                    imageCv = image;
                    var window = new OpenCvSharp.Window("Video Frame by Frame");
                    window.ShowImage(imageCv);
                }*/
                if (i % 3 == 0)
                {
                    BitmapSource frame = Convert(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image)); // в битмап
                    if(!dic_image.ContainsKey(i)) dic_image[i] = frame;

                }//добавляем конвертированный в битмап сурс битмап в список битмапов
                i++;
                //window.ShowImage(image);  //для вывода в окно
            }


            (DataContext as Video).List_Of_Frames = dic_image;
            if (dic_image.Count!=0) (DataContext as Video).Video_source = dic_image[0];
            /*
            foreach (var frame in dic_image)
            {
                Console.WriteLine($"Key: {frame.Key}, Value: {frame.Value}");
            }
            */


            //Console.WriteLine(Frames.Count);
        }


        private void rec_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectFlag = true;
            rec.CaptureMouse();
            startPoint = e.GetPosition(rec);
            sreault.Visibility = Visibility.Hidden;
            if (selectFlag && (DataContext as Video).Video_source != null)
            {
                if ((DataContext as Video).Video_source.Width >= (DataContext as Video).Video_source.Height)
                {
                    crop_im = (DataContext as Video).Video_source.Width / image.ActualWidth;
                }
                else
                {
                    crop_im = (DataContext as Video).Video_source.Height / image.ActualHeight;
                }

            }
                
            


        }

        private void rec_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (selectFlag && (DataContext as Video).Video_source!=null)
            {
                Point newPoint = e.GetPosition((IInputElement)rec.Parent);
               
                if((newPoint.X - startPoint.X+rec.ActualWidth) < image.ActualWidth && (newPoint.Y - startPoint.Y + rec.ActualHeight) < image.ActualHeight && (newPoint.X - startPoint.X)>0 && (newPoint.Y - startPoint.Y) >0)
                {
                    Canvas.SetLeft(rec, newPoint.X - startPoint.X);
                    Canvas.SetTop(rec, newPoint.Y - startPoint.Y);
                }

                
                Point PosCanv = rec.TranslatePoint(new Point(0,0), image);
                TP = rec.TranslatePoint(new Point(0, 0), image);


                var tempCrB = GetCroppedBitmap((DataContext as Video).Video_source, PosCanv.X * crop_im, PosCanv.Y * crop_im, rec.ActualWidth * crop_im, rec.ActualHeight * crop_im);
                //CroppedBitmap croppedBitmap = new CroppedBitmap((DataContext as Video).Video_source, new Int32Rect((int)PosCanv.X*4, (int)PosCanv.Y*4, (int)rec.ActualWidth, (int)rec.ActualHeight));
                tempCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap(tempCrB));
                /*
                var window = new OpenCvSharp.Window("Video Frame by Frame");
                window.ShowImage(tempCv);
                */
                
                img.Source = tempCrB;
                
                
            }


        }

        private void rec_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            selectFlag = false;
            
            rec.ReleaseMouseCapture();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedKey = box.SelectedIndex;
            Canvas.SetLeft(rec, 0);
            Canvas.SetTop(rec, 0);

            if (dic_image.ContainsKey(selectedKey) && flag==2)
            {
                (DataContext as Video).Video_source = dic_image[selectedKey]; 
            }
            if (dic_image2.ContainsKey(selectedKey) && flag == 1)
            {
                (DataContext as Video).Video_source = dic_image2[selectedKey];
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if ((DataContext as Video).Video_source != null && img.Source !=null)
            {
                imageCv = OpenCvSharp.Extensions.BitmapConverter.ToMat(GetBitmap((DataContext as Video).Video_source));
                var result = new Mat();
                result = imageCv.MatchTemplate(tempCv, TemplateMatchModes.CCoeffNormed);
                result.MinMaxLoc(out minVal, out maxVal, out minLoc, out maxLoc);

                
                Console.WriteLine(maxVal);
                
                Console.WriteLine("\nTemp X = {0}, Y = {1}", TP.X * 1.0 / image.ActualWidth * (DataContext as Video).Video_source.Width, TP.Y * 1.0 / image.ActualHeight * (DataContext as Video).Video_source.Height);
                Console.WriteLine("Result X = {0}, Y = {1}\n", maxLoc.X * 1.0 / imageCv.Width * (DataContext as Video).Video_source.Width, maxLoc.Y * 1.0 / imageCv.Height * (DataContext as Video).Video_source.Height);
                if (maxVal > 0.55)
                {
                    Canvas.SetLeft(sreault, maxLoc.X * 1.0 / imageCv.Width * image.ActualWidth);
                    Canvas.SetTop(sreault, maxLoc.Y * 1.0 / imageCv.Height * image.ActualHeight);
                    sreault.Visibility = Visibility.Visible;
                }
            }
        }
    }
}